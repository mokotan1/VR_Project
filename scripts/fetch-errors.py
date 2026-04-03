"""
CI Error Fetcher
----------------
GitHub CLI(gh)를 사용하여 모든 브랜치의 실패한 CI 워크플로우에서
오류 정보를 가져와 .cursor/errors/errors.json에 저장합니다.

사전 요구 사항:
    1. GitHub CLI 설치: winget install GitHub.cli
    2. 인증: gh auth login

Usage:
    python scripts/fetch-errors.py                    # 모든 브랜치의 최근 실패 가져오기
    python scripts/fetch-errors.py --branch kth       # 특정 브랜치만
    python scripts/fetch-errors.py --limit 5          # 최근 5개 실패만
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
import tempfile
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parent.parent
CURSOR_ERRORS_DIR = REPO_ROOT / ".cursor" / "errors"
OUTPUT_FILE = CURSOR_ERRORS_DIR / "errors.json"
WORKFLOW_NAME = "CI Error Check"

DEFAULT_LIMIT = 10


def run_gh(*args: str) -> str:
    cmd = ["gh", *args]
    try:
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            check=True,
            cwd=str(REPO_ROOT),
        )
        return result.stdout.strip()
    except FileNotFoundError:
        print("ERROR: GitHub CLI(gh)가 설치되어 있지 않습니다.")
        print("  설치: winget install GitHub.cli")
        print("  인증: gh auth login")
        sys.exit(1)
    except subprocess.CalledProcessError as exc:
        stderr = exc.stderr.strip() if exc.stderr else ""
        if "not logged" in stderr.lower() or "auth" in stderr.lower():
            print("ERROR: GitHub CLI 인증이 필요합니다.")
            print("  실행: gh auth login")
            sys.exit(1)
        raise


def get_failed_runs(*, branch: str | None, limit: int) -> list[dict[str, Any]]:
    """실패한 workflow run 목록을 조회합니다."""
    args = [
        "run", "list",
        "--workflow", WORKFLOW_NAME,
        "--status", "failure",
        "--limit", str(limit),
        "--json", "databaseId,headBranch,headSha,createdAt,conclusion,name,event",
    ]
    if branch:
        args.extend(["--branch", branch])

    raw = run_gh(*args)
    if not raw:
        return []

    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        return []


def _download_single_artifact(artifact_id: int) -> list[dict[str, Any]]:
    """단일 artifact를 다운로드하고 JSON 오류 목록을 반환합니다."""
    errors: list[dict[str, Any]] = []
    with tempfile.TemporaryDirectory() as dl_dir:
        zip_path = Path(dl_dir) / "artifact.zip"
        run_gh(
            "api",
            f"repos/{{owner}}/{{repo}}/actions/artifacts/{artifact_id}/zip",
            "--method", "GET",
            "--output", str(zip_path),
        )
        if not zip_path.exists() or zip_path.stat().st_size == 0:
            return errors
        with zipfile.ZipFile(zip_path) as zf:
            for name in zf.namelist():
                if name.endswith(".json"):
                    data = json.loads(zf.read(name))
                    errors.extend(data.get("errors", []))
    return errors


def _parse_artifact_list(raw: str) -> list[dict[str, Any]]:
    """artifact 목록 JSON lines를 파싱하여 유효한 error-report artifact만 반환합니다."""
    artifacts: list[dict[str, Any]] = []
    for line in raw.strip().splitlines():
        try:
            artifact = json.loads(line)
        except json.JSONDecodeError:
            continue
        if not artifact.get("name", "").startswith("error-report-"):
            continue
        if artifact.get("expired", True):
            continue
        artifacts.append(artifact)
    return artifacts


def download_artifact(run_id: int) -> list[dict[str, Any]]:
    """특정 run의 error-report artifact를 다운로드하고 파싱합니다."""
    artifacts_raw = run_gh(
        "api",
        f"repos/{{owner}}/{{repo}}/actions/runs/{run_id}/artifacts",
        "--jq", ".artifacts[] | {id: .id, name: .name, expired: .expired}",
    )
    if not artifacts_raw:
        return []

    all_errors: list[dict[str, Any]] = []
    for artifact in _parse_artifact_list(artifacts_raw):
        try:
            all_errors.extend(_download_single_artifact(artifact["id"]))
        except (subprocess.CalledProcessError, json.JSONDecodeError, zipfile.BadZipFile):
            continue
    return all_errors


def _parse_error_lines(log_output: str) -> list[dict[str, Any]]:
    """로그 텍스트에서 error 키워드를 포함하는 라인을 오류 객체로 변환합니다."""
    errors: list[dict[str, Any]] = []
    for raw_line in str(log_output).splitlines():
        stripped = raw_line.strip()
        if not stripped or stripped.startswith("##"):
            continue
        if "error" not in stripped.lower():
            continue
        errors.append({
            "type": "ci_log",
            "file": "",
            "line": 0,
            "column": 0,
            "message": stripped[:500],
            "severity": "error",
        })
    return errors


def fetch_run_logs_fallback(run_id: int) -> list[dict[str, Any]]:
    """Artifact가 없는 경우, workflow 로그에서 직접 오류를 파싱합니다."""
    try:
        run_gh(
            "run", "view", str(run_id),
            "--log-failed",
            "--exit-status",
        )
    except subprocess.CalledProcessError as exc:
        log_output = exc.stdout or exc.stderr or ""
        return _parse_error_lines(log_output)
    return []


def build_combined_report(
    runs: list[dict[str, Any]],
    *,
    branch_filter: str | None,
) -> dict[str, Any]:
    """여러 run의 오류를 하나의 리포트로 합칩니다."""
    branch_errors: dict[str, list[dict[str, Any]]] = {}

    for run in runs:
        run_id = run["databaseId"]
        branch_name = run["headBranch"]
        commit_sha = run["headSha"]

        errors = download_artifact(run_id)
        if not errors:
            errors = fetch_run_logs_fallback(run_id)

        for error in errors:
            error["branch"] = branch_name
            error["commit"] = commit_sha[:8]
            error["run_id"] = run_id

        if errors:
            branch_errors.setdefault(branch_name, []).extend(errors)

    all_errors = []
    for errs in branch_errors.values():
        all_errors.extend(errs)

    return {
        "schema_version": "1.0",
        "fetched_at": datetime.now(timezone.utc).isoformat(),
        "filter_branch": branch_filter,
        "branches_with_errors": list(branch_errors.keys()),
        "total_errors": len([e for e in all_errors if e.get("severity") == "error"]),
        "total_warnings": len([e for e in all_errors if e.get("severity") == "warning"]),
        "errors": all_errors,
    }


def print_summary(report: dict[str, Any]) -> None:
    """오류 요약을 터미널에 출력합니다."""
    print("\n" + "=" * 60)
    print("  CI Error Report")
    print("=" * 60)

    if not report["errors"]:
        print("\n  All branches are clean! No errors found.")
        print("=" * 60)
        return

    print(f"\n  Branches with errors: {', '.join(report['branches_with_errors'])}")
    print(f"  Total errors:   {report['total_errors']}")
    print(f"  Total warnings: {report['total_warnings']}")
    print("-" * 60)

    by_branch: dict[str, list[dict[str, Any]]] = {}
    for error in report["errors"]:
        br = error.get("branch", "unknown")
        by_branch.setdefault(br, []).append(error)

    for branch_name, errors in by_branch.items():
        print(f"\n  [{branch_name}] ({len(errors)} issues)")
        for err in errors[:15]:
            sev_icon = "X" if err["severity"] == "error" else "!"
            loc = err["file"]
            if err.get("line"):
                loc += f":{err['line']}"
            msg = err["message"][:80]
            print(f"    [{sev_icon}] {loc}")
            print(f"        {msg}")

        if len(errors) > 15:
            print(f"    ... and {len(errors) - 15} more")

    print("\n" + "=" * 60)
    print(f"  Report saved to: {OUTPUT_FILE}")
    print("  Use Cursor AI with: 'CI 오류 수정해줘'")
    print("=" * 60 + "\n")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Fetch CI errors from GitHub Actions"
    )
    parser.add_argument(
        "--branch", "-b",
        default=None,
        help="특정 브랜치만 조회 (기본: 전체)",
    )
    parser.add_argument(
        "--limit", "-l",
        type=int,
        default=DEFAULT_LIMIT,
        help=f"조회할 최근 실패 run 수 (기본: {DEFAULT_LIMIT})",
    )
    args = parser.parse_args()

    print("Fetching failed CI runs from GitHub...")
    runs = get_failed_runs(branch=args.branch, limit=args.limit)

    if not runs:
        print("No failed CI runs found. All branches are clean!")
        return 0

    print(f"Found {len(runs)} failed run(s). Downloading error details...")
    report = build_combined_report(runs, branch_filter=args.branch)

    CURSOR_ERRORS_DIR.mkdir(parents=True, exist_ok=True)
    OUTPUT_FILE.write_text(
        json.dumps(report, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )

    print_summary(report)
    return 1 if report["total_errors"] > 0 else 0


if __name__ == "__main__":
    sys.exit(main())

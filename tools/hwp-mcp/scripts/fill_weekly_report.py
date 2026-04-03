"""CLI: apply weekly report txt into HWP. Run from repo root."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[1]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from hwp_mcp.report_fill import apply_txt_to_weekly_hwp  # noqa: E402


def main() -> None:
    repo = Path(__file__).resolve().parents[3]
    p = argparse.ArgumentParser(description="Fill weekly HWP from UTF-8 txt.")
    p.add_argument(
        "--hwp",
        type=Path,
        default=repo / "보고서" / "오합지졸_캡스톤디자인 프로젝트 주간 보고서(김기석 3.25).hwp",
        help="기준 서식(빈 주간 양식). 기본은 3.25 템플릿.",
    )
    p.add_argument(
        "--txt",
        type=Path,
        default=repo / "보고서" / "오합지졸_캡스톤디자인 프로젝트 주간 보고서(김기석 3.31).txt",
    )
    p.add_argument(
        "--out",
        type=Path,
        default=repo / "보고서" / "오합지졸_캡스톤디자인 프로젝트 주간 보고서(김기석 3.31)_filled.hwp",
        help="저장할 주간보고서 .hwp 경로 (기본: …김기석 3.31)_filled.hwp).",
    )
    p.add_argument(
        "--date-old",
        default=None,
        help="치환 전 보고 일자 한 줄(텍스트 내보내기와 동일). 생략 시 문서에서 날짜 줄을 자동 탐지.",
    )
    p.add_argument(
        "--date-new",
        default="2026년  3 월 31일",
        help="넣을 보고 일자 한 줄.",
    )
    args = p.parse_args()
    msg = apply_txt_to_weekly_hwp(
        args.hwp,
        args.txt,
        args.out,
        report_date_line_old=args.date_old,
        report_date_line_new=args.date_new or None,
    )
    print(msg)


if __name__ == "__main__":
    main()

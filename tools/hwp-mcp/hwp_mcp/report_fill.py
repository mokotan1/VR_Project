"""
Weekly HWP report: export plain text via SaveAs(TEXT), then replace line-by-line
using Execute("AllReplace") — Hangul does not match multi-line FindString.
"""

from __future__ import annotations

import os
import re
import tempfile
from pathlib import Path

try:
    import win32com.client as win32  # type: ignore[import-untyped]
except ImportError:  # pragma: no cover
    win32 = None  # type: ignore[assignment]

from hwp_mcp.automation import (
    HWP_COM_PROGID,
    SECURITY_MODULE_PAIR,
    HwpAutomationError,
    configure_hwp_automation_ui,
    restore_hwp_message_box_mode,
)

_NEWLINE_RE = re.compile(r"\r\n|\r")
_RULE_LINE = re.compile(r"^─{5,}\s*$", re.MULTILINE)
# 한글 텍스트 내보내기의 보고 일자 줄 (공백 수가 버전마다 다를 수 있음)
_KR_DATE_LINE_RE = re.compile(r"\d{4}년\s+\d+\s*월\s+\d+\s*일")


def _report_date_line_after_label(snap_text: str) -> str | None:
    """Return the date line immediately under '보고 일자' in exported text."""
    lines = normalize_newlines(snap_text).split("\n")
    for i, line in enumerate(lines):
        if line.strip() == "보고 일자" and i + 1 < len(lines):
            cand = lines[i + 1].strip()
            if _KR_DATE_LINE_RE.fullmatch(cand):
                return cand
    return None


def normalize_newlines(s: str) -> str:
    return _NEWLINE_RE.sub("\n", s)


def parse_weekly_txt_sections(raw: str) -> dict[str, str]:
    text = normalize_newlines(raw)
    m = re.search(r"^1\.\s*기본\s*정보\s*$", text, re.MULTILINE)
    if not m:
        raise HwpAutomationError("Weekly txt: missing '1. 기본 정보'.")
    text = text[m.start() :]
    pat = re.compile(r"^(?P<num>[1-6])\.\s*(?P<title>[^\n]+)$", re.MULTILINE)
    matches = list(pat.finditer(text))
    if not matches:
        raise HwpAutomationError("Weekly txt: no numbered sections 1–6.")
    out: dict[str, str] = {}
    for i, mm in enumerate(matches):
        num = int(mm.group("num"))
        start = mm.start()
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        chunk = text[start:end].strip()
        out[f"s{num}"] = chunk

    if "s6" in out:
        s6 = out["s6"]
        for sep in (
            "\n────────────────────────\n부록",
            "\n부록:",
            "\n\n부록",
        ):
            if sep in s6:
                out["s6"] = s6.split(sep, 1)[0].strip()
                out["appendix"] = ("부록" + s6.split(sep, 1)[1]).strip()
                break
    return out


def _table_rows_from_section2(s2: str) -> list[tuple[str, str, str]]:
    rows: list[tuple[str, str, str]] = []
    for line in normalize_newlines(s2).split("\n"):
        line = line.strip()
        m = re.match(
            r"^\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*([^|]*?)\s*\|\s*$",
            line,
        )
        if not m:
            continue
        g1, g2, g3, _ = (m.group(i).strip() for i in range(1, 5))
        if g1 in ("구분", "---") or g1.startswith("-"):
            continue
        rows.append((g1, g2, g3))
    return rows


def _progress_pairs_from_section2(s2: str) -> list[tuple[str, str]]:
    pairs: list[tuple[str, str]] = []
    for line in normalize_newlines(s2).split("\n"):
        line = line.strip()
        m = re.match(r"^•\s*(.+?):\s*(\d+%)\s*$", line)
        if m:
            pairs.append((m.group(1).strip(), m.group(2).strip()))
    return pairs


def build_hwp_section2(sections: dict[str, str]) -> str:
    s2 = sections.get("s2", "")
    rows = _table_rows_from_section2(s2)
    prog = _progress_pairs_from_section2(s2)
    out: list[str] = ["금주 목표"]
    for g1, g2, _g3 in rows:
        out.append(f"{g1}: {g2}")
        out.append(" 김기석")
        out.append("")
    out.append("진행 현황")
    if prog:
        for label, pct in prog:
            out.append(label)
            out.append("")
            out.append(pct)
            out.append("")
    elif rows:
        out.append(rows[0][2])
        out.append("")
        out.append("100%")
        out.append("")
    return "\n".join(out).rstrip() + "\n"


def build_hwp_section3(sections: dict[str, str]) -> str:
    s3 = normalize_newlines(sections.get("s3", ""))
    s3 = re.sub(r"^3\.\s*주요\s*수행\s*내용\s*\(상세\)\s*$", "", s3, flags=re.MULTILINE)
    s3 = _RULE_LINE.sub("", s3)
    inner = s3.strip()
    return (
        "3. 주요 수행 내용 (상세)\n\n"
        "성명\n"
        "담당 업무 및 수행 내용 (기술적 상세 기술)\n"
        "제출 결과물\n"
        "김기석\n"
        f"{inner}\n"
    )


def build_hwp_section4(sections: dict[str, str]) -> str:
    s4 = normalize_newlines(sections.get("s4", ""))
    s4 = re.sub(r"^4\.\s*문제점\s*및\s*해결\s*방안\s*$", "", s4, flags=re.MULTILINE)
    s4 = _RULE_LINE.sub("", s4)
    lines = list(s4.split("\n"))
    out_lines: list[str] = ["4. 문제점 및 해결 방안", ""]
    i = 0
    while i < len(lines):
        ln = lines[i].strip()
        if ln.startswith("• 문제:") or ln.startswith("•문제:"):
            prob = re.sub(r"^•\s*문제:\s*", "", ln).strip()
            out_lines.append(f"\t&#8226; 문제점 : {prob}")
            i += 1
            if i < len(lines) and lines[i].strip().startswith("해결:"):
                sol = lines[i].strip()
                sol = re.sub(r"^해결:\s*", "", sol).strip()
                out_lines.append(f"\t&#8226; 해결 내용: {sol}")
                i += 1
            continue
        i += 1
    return "\n".join(out_lines) + "\n"


def build_hwp_section5(sections: dict[str, str]) -> str:
    s5 = normalize_newlines(sections.get("s5", ""))
    s5 = re.sub(r"^5\.\s*차주\s*계획\s*$", "", s5, flags=re.MULTILINE)
    s5 = _RULE_LINE.sub("", s5).strip()
    body = s5.replace("\n", "\n\t")
    return f"5. 차주 계획\n\n\t{body}\n"


def build_hwp_section6(sections: dict[str, str]) -> str:
    s6 = normalize_newlines(sections.get("s6", ""))
    s6 = re.sub(r"^6\.\s*참고\s*자료[^\n]*\s*$", "", s6, flags=re.MULTILINE)
    s6 = _RULE_LINE.sub("", s6).strip()
    parts = [
        "6. 증빙 자료(스크린샷 / 코드 일부 등) ",
        "",
        "",
        "",
        "",
        s6,
    ]
    if sections.get("appendix"):
        parts.extend(["", sections["appendix"]])
    return "\n".join(parts) + "\n"


def export_hwp_as_plain_utf8(hwp: object, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    hwp.SaveAs(str(path), "TEXT", "")


def read_hwp_text_export(path: Path) -> str:
    raw = path.read_bytes()
    for enc in ("utf-8-sig", "utf-8", "cp949", "euc-kr"):
        try:
            return raw.decode(enc)
        except UnicodeDecodeError:
            continue
    return raw.decode("cp949", errors="replace")


def _replace_all(hwp: object, find: str, replace: str) -> bool:
    ps = hwp.HParameterSet.HFindReplace.HSet
    hwp.HAction.GetDefault("AllReplace", ps)
    ps.SetItem("FindString", find)
    ps.SetItem("ReplaceString", replace)
    try:
        ps.SetItem("IgnoreMessage", 1)
    except Exception:
        pass
    return bool(hwp.HAction.Execute("AllReplace", ps))


def _extract_snapshot_blocks(snap: str) -> dict[str, str]:
    s = normalize_newlines(snap)
    out: dict[str, str] = {}
    out["s2"] = _between(s, "금주 목표\n", "\n\n3. 주요 수행 내용 (상세)")
    out["s3"] = _between(s, "3. 주요 수행 내용 (상세)\n", "\n\n4. 문제점 및 해결 방안")
    out["s4"] = _between(s, "4. 문제점 및 해결 방안\n", "\n\n5. 차주 계획")
    out["s5"] = _between(s, "5. 차주 계획\n", "\n\n6. 증빙 자료")
    idx = s.find("6. 증빙 자료")
    if idx < 0:
        raise HwpAutomationError("Snapshot: section 6 header not found.")
    out["s6"] = s[idx:]
    return out


def _between(haystack: str, start: str, end: str) -> str:
    i = haystack.index(start)
    j = haystack.index(end, i + len(start))
    return haystack[i:j]


def _align_line_pairs(old_block: str, new_block: str) -> list[tuple[str, str]]:
    """Pair lines; pad the shorter side with empty lines at the end."""
    o_lines = normalize_newlines(old_block).split("\n")
    n_lines = normalize_newlines(new_block).split("\n")
    if len(n_lines) < len(o_lines):
        n_lines.extend([""] * (len(o_lines) - len(n_lines)))
    elif len(o_lines) < len(n_lines):
        o_lines.extend([""] * (len(n_lines) - len(o_lines)))
    return list(zip(o_lines, n_lines))


def apply_line_diff_replaces(
    hwp: object,
    snap_text: str,
    old_block: str,
    new_block: str,
) -> tuple[str, list[str]]:
    """
    Replace line-by-line where old line is unique in snap_text. Longest old lines first.
    Returns (updated_snap_text, warnings).
    """
    warnings: list[str] = []
    working = snap_text
    pairs = _align_line_pairs(old_block, new_block)
    indexed: list[tuple[int, str, str]] = [
        (i, o, n) for i, (o, n) in enumerate(pairs) if o != n
    ]
    indexed.sort(key=lambda t: len(t[1]), reverse=True)

    for _i, old_ln, new_ln in indexed:
        if not old_ln.strip():
            if new_ln.strip():
                warnings.append(f"Skipped insert-only line (empty old): {new_ln[:60]!r}")
            continue
        if old_ln.strip() and not new_ln.strip():
            cnt = working.count(old_ln)
            if cnt == 1:
                if _replace_all(hwp, old_ln, ""):
                    working = working.replace(old_ln, "", 1)
                else:
                    warnings.append(f"Delete line failed: {old_ln[:80]!r}")
            else:
                warnings.append(
                    f"Skipped delete (non-unique ×{cnt}): {old_ln[:60]!r}"
                )
            continue
        cnt = working.count(old_ln)
        if cnt == 0:
            warnings.append(f"No match for line (already replaced?): {old_ln[:80]!r}")
            continue
        if cnt != 1:
            warnings.append(
                f"Skipped non-unique line (×{cnt}): {old_ln[:80]!r} — edit manually in Hangul."
            )
            continue
        ok = _replace_all(hwp, old_ln, new_ln)
        if not ok:
            warnings.append(f"Replace failed for: {old_ln[:80]!r}")
            continue
        working = working.replace(old_ln, new_ln, 1)

    return working, warnings


def apply_txt_to_weekly_hwp(
    hwp_path: str | os.PathLike[str],
    weekly_txt_path: str | os.PathLike[str],
    output_path: str | os.PathLike[str],
    *,
    report_date_line_old: str | None = None,
    report_date_line_new: str | None = None,
) -> str:
    if win32 is None:
        raise HwpAutomationError(
            "pywin32 is not installed. Run: pip install -r tools/hwp-mcp/requirements.txt"
        )

    hwp_p = Path(hwp_path).expanduser().resolve()
    txt_p = Path(weekly_txt_path).expanduser().resolve()
    out_p = Path(output_path).expanduser().resolve()
    if not hwp_p.is_file():
        raise HwpAutomationError(f"HWP not found: {hwp_p}")
    if not txt_p.is_file():
        raise HwpAutomationError(f"TXT not found: {txt_p}")

    sections = parse_weekly_txt_sections(txt_p.read_text(encoding="utf-8"))
    new2 = build_hwp_section2(sections)
    new3 = build_hwp_section3(sections)
    new4 = build_hwp_section4(sections)
    new5 = build_hwp_section5(sections)
    new6 = build_hwp_section6(sections)

    hwp = win32.gencache.EnsureDispatch(HWP_COM_PROGID)
    msg_prev: int | None = None
    all_warnings: list[str] = []
    try:
        hwp.RegisterModule(*SECURITY_MODULE_PAIR)
        msg_prev = configure_hwp_automation_ui(hwp)
        hwp.Open(str(hwp_p))

        with tempfile.TemporaryDirectory() as td:
            snap_path = Path(td) / "snap.txt"
            export_hwp_as_plain_utf8(hwp, snap_path)
            snap_text = read_hwp_text_export(snap_path)
        hwp.Open(str(hwp_p))

        blocks = _extract_snapshot_blocks(snap_text)

        if report_date_line_new:
            if report_date_line_old and snap_text.count(report_date_line_old) == 1:
                if not _replace_all(hwp, report_date_line_old, report_date_line_new):
                    all_warnings.append("보고 일자 replace failed.")
                else:
                    snap_text = snap_text.replace(report_date_line_old, report_date_line_new, 1)
            elif report_date_line_old:
                all_warnings.append(
                    f"보고 일자 skipped (count={snap_text.count(report_date_line_old)})."
                )
            else:
                replaced = False
                label_date = _report_date_line_after_label(snap_text)
                if label_date and label_date.strip() == report_date_line_new.strip():
                    replaced = True
                elif (
                    label_date
                    and label_date.strip() != report_date_line_new.strip()
                    and snap_text.count(label_date) == 1
                ):
                    if _replace_all(hwp, label_date, report_date_line_new):
                        snap_text = snap_text.replace(label_date, report_date_line_new, 1)
                        replaced = True
                if not replaced:
                    for m in _KR_DATE_LINE_RE.finditer(snap_text):
                        cand = m.group(0)
                        if cand.strip() == report_date_line_new.strip():
                            continue
                        if snap_text.count(cand) != 1:
                            continue
                        if _replace_all(hwp, cand, report_date_line_new):
                            snap_text = snap_text.replace(cand, report_date_line_new, 1)
                            replaced = True
                            break
                if not replaced:
                    all_warnings.append(
                        "보고 일자: '보고 일자' 다음 줄을 바꾸지 못했습니다. "
                        "--date-old 에 텍스트 내보내기와 동일한 한 줄을 넣어 주세요."
                    )

        for key, new_block in [
            ("s2", new2),
            ("s3", new3),
            ("s4", new4),
            ("s5", new5),
            ("s6", new6),
        ]:
            snap_text, warns = apply_line_diff_replaces(
                hwp, snap_text, blocks[key], new_block
            )
            for w in warns:
                all_warnings.append(f"[{key}] {w}")

        out_p.parent.mkdir(parents=True, exist_ok=True)
        hwp.SaveAs(str(out_p))
    finally:
        if msg_prev is not None:
            try:
                restore_hwp_message_box_mode(hwp, msg_prev)
            except Exception:
                pass
        try:
            hwp.Quit()
        except Exception:
            pass

    msg = f"Saved: {out_p}"
    if all_warnings:
        msg += "\nWarnings:\n" + "\n".join(f"- {w}" for w in all_warnings)
    return msg

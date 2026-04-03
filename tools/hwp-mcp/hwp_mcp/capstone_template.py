"""
Map weekly report UTF-8 sections to 누름틀 field names for
캡스톤디자인 주간보고서(양식) — PutFieldText keys must match the HWP template.
"""

from __future__ import annotations

import os
import re
from pathlib import Path
from typing import Mapping

from hwp_mcp.automation import HwpAutomationError, fill_template
from hwp_mcp.report_fill import (
    _table_rows_from_section2,
    normalize_newlines,
    parse_weekly_txt_sections,
)

def _extract_report_date(raw: str) -> str:
    m = re.search(r"^작성일:\s*(.+)$", raw, re.MULTILINE)
    if m:
        return m.group(1).strip()
    m = re.search(r"^•\s*작성일:\s*(.+)$", raw, re.MULTILINE)
    if m:
        return m.group(1).strip()
    return ""


def _extract_author_name(s1: str) -> str:
    m = re.search(r"^•\s*성명:\s*(.+)$", normalize_newlines(s1), re.MULTILINE)
    if m:
        return m.group(1).strip()
    return ""


def _four_progress_rates(s2: str) -> list[str]:
    """Values after '• label:' in the 진행률 block (4 rows)."""
    rates: list[str] = []
    in_section = False
    for line in normalize_newlines(s2).split("\n"):
        line_st = line.strip()
        if "진행률" in line_st and "예시" in line_st:
            in_section = True
            continue
        if in_section:
            m = re.match(r"^•\s*.+?:\s*(.+)$", line_st)
            if m:
                rates.append(m.group(1).strip())
            elif line_st == "" and rates:
                break
    while len(rates) < 4:
        rates.append("—")
    return rates[:4]


def _table_rows_padded(s2: str, n: int = 4) -> list[tuple[str, str, str]]:
    rows = _table_rows_from_section2(s2)
    while len(rows) < n:
        rows.append(("", "", ""))
    return rows[:n]


def _strip_section_heading(s: str, num: int, title_regex: str) -> str:
    t = normalize_newlines(s)
    t = re.sub(rf"^{num}\.\s*{title_regex}\s*$", "", t, flags=re.MULTILINE)
    t = re.compile(r"^─{5,}\s*$", re.MULTILINE).sub("", t)
    return t.strip()


def _split_s3_into_three_tasks(s3: str) -> tuple[str, str, str]:
    """Split §3 body into three blocks for the template table (3 rows)."""
    inner = _strip_section_heading(
        s3, 3, r"주요\s*수행\s*내용\s*\(상세\)"
    )
    if not inner:
        return "", "", ""
    blocks = re.split(r"\n(?=\[[^\]]+\])", inner)
    blocks = [b.strip() for b in blocks if b.strip()]
    if len(blocks) >= 3:
        return blocks[0], blocks[1], blocks[2]
    lines = [ln for ln in inner.split("\n") if ln.strip()]
    if not lines:
        return "", "", ""
    n = len(lines)
    a = (n + 2) // 3
    b = (n - a + 1) // 2
    p1 = "\n".join(lines[:a])
    p2 = "\n".join(lines[a : a + b])
    p3 = "\n".join(lines[a + b :])
    return p1, p2, p3


def _parse_section4_pairs(s4: str) -> list[tuple[str, str]]:
    lines = normalize_newlines(s4).split("\n")
    pairs: list[tuple[str, str]] = []
    i = 0
    while i < len(lines):
        ln = lines[i].strip()
        if ln.startswith("• 문제:") or ln.startswith("•문제:"):
            prob = re.sub(r"^•\s*문제:\s*", "", ln).strip()
            sol = ""
            if i + 1 < len(lines) and lines[i + 1].strip().startswith("해결:"):
                sol = re.sub(r"^해결:\s*", "", lines[i + 1].strip()).strip()
                i += 1
            pairs.append((prob, sol))
        i += 1
    return pairs


def weekly_sections_to_capstone_field_data(sections: Mapping[str, str], raw: str) -> dict[str, str]:
    """
    Build PutFieldText map for the capstone weekly template.
    `raw` is the full txt (for 작성일 before §1).
    """
    s1 = sections.get("s1", "")
    s2 = sections.get("s2", "")
    s3 = sections.get("s3", "")
    s4 = sections.get("s4", "")
    s5 = sections.get("s5", "")
    s6 = sections.get("s6", "")
    appendix = sections.get("appendix", "")

    author = _extract_author_name(s1) or _extract_author_name(raw)
    report_date = _extract_report_date(raw)

    rows = _table_rows_padded(s2, 4)
    rates = _four_progress_rates(s2)

    data: dict[str, str] = {
        "report_date": report_date,
        "member_g1": author,
        "author_name": author,
    }

    for i in range(4):
        _g1, g2, g3 = rows[i]
        data[f"goal_detail_{i + 1}"] = g2.strip() if g2 else "—"
        data[f"prog_detail_{i + 1}"] = g3.strip() if g3 else "—"
        data[f"goal_rate_{i + 1}"] = rates[i]
        data[f"prog_rate_{i + 1}"] = rates[i]

    t1, t2, t3 = _split_s3_into_three_tasks(s3)
    data["s3_task_1"] = t1
    data["s3_task_2"] = t2
    data["s3_task_3"] = t3
    data["s3_deliver_1"] = ""
    data["s3_deliver_2"] = ""
    data["s3_deliver_3"] = ""

    pairs = _parse_section4_pairs(s4)
    if pairs:
        data["s4_problem"] = "\n".join(f"• {p}" for p, _ in pairs)
        data["s4_solution"] = "\n".join(f"• {s}" for _, s in pairs if s)
        data["s4_pending"] = "없음"
    else:
        data["s4_problem"] = ""
        data["s4_solution"] = ""
        data["s4_pending"] = ""

    s5_body = _strip_section_heading(s5, 5, r"차주\s*계획")
    data["next_week"] = s5_body

    s6_clean = _strip_section_heading(s6, 6, r"참고\s*자료[^\n]*")
    ev = s6_clean
    if appendix:
        ev = ev + "\n\n" + appendix.strip()
    data["s6_evidence"] = ev.strip()

    # Legacy placeholder name in some templates — clear so COM does not target wrong field.
    data["PutFieldText"] = ""

    return data


def weekly_txt_to_capstone_field_data(raw_txt: str) -> dict[str, str]:
    sections = parse_weekly_txt_sections(raw_txt)
    return weekly_sections_to_capstone_field_data(sections, raw_txt)


def fill_capstone_weekly_template(
    template_path: str | os.PathLike[str],
    weekly_txt_path: str | os.PathLike[str],
    output_path: str | os.PathLike[str],
) -> str:
    """
    Parse weekly UTF-8 txt, map to capstone 누름틀 names, fill_template, save.
    """
    txt_p = Path(weekly_txt_path).expanduser().resolve()
    if not txt_p.is_file():
        raise HwpAutomationError(f"TXT not found: {txt_p}")
    raw = txt_p.read_text(encoding="utf-8")
    data = weekly_txt_to_capstone_field_data(raw)
    return fill_template(template_path, output_path, data)

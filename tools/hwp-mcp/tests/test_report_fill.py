"""Unit tests for weekly report txt parsing (no HWP COM)."""

from __future__ import annotations

import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[1]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from hwp_mcp.report_fill import (  # noqa: E402
    _align_line_pairs,
    _report_date_line_after_label,
    normalize_newlines,
    parse_weekly_txt_sections,
)


def test_normalize_newlines() -> None:
    assert normalize_newlines("a\r\nb\rc") == "a\nb\nc"


def test_parse_weekly_txt_sections_finds_keys() -> None:
    raw = """
────────────────────────
1. 기본 정보
────────────────────────
x

────────────────────────
2. 주간계획 및 진행상황 요약
────────────────────────
y

────────────────────────
6. 참고 자료 (링크)
────────────────────────
z
"""
    sections = parse_weekly_txt_sections(raw)
    assert "s1" in sections and "s2" in sections and "s6" in sections


def test_align_line_pairs_pad_new_shorter() -> None:
    old = "a\nb\nc"
    new = "A\nB"
    pairs = _align_line_pairs(old, new)
    assert pairs == [("a", "A"), ("b", "B"), ("c", "")]


def test_report_date_line_after_label() -> None:
    snap = "보고 일자\n2026년  3 월 25일\n"
    assert _report_date_line_after_label(snap) == "2026년  3 월 25일"


def test_align_line_pairs_pad_old_shorter() -> None:
    old = "a"
    new = "A\nB"
    pairs = _align_line_pairs(old, new)
    assert pairs == [("a", "A"), ("", "B")]

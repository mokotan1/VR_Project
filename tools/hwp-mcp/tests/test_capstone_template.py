"""Capstone weekly 누름틀 mapping (no HWP COM)."""

from __future__ import annotations

import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[1]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from hwp_mcp.capstone_template import (  # noqa: E402
    weekly_txt_to_capstone_field_data,
)
from hwp_mcp.report_fill import parse_weekly_txt_sections  # noqa: E402


def test_weekly_txt_to_capstone_field_data_sample() -> None:
    raw = """
────────────────────────
1. 기본 정보
────────────────────────
• 성명: 김기석
────────────────────────
2. 주간계획 및 진행상황 요약
────────────────────────
| 구분 | 금주 목표(계획) | 진행 내용 요약 | 비고 |
| A | goal-a | prog-a | x |
| B | goal-b | prog-b | x |
| C | goal-c | prog-c | x |
| D | goal-d | prog-d | x |
진행률(서식에 % 칸이 있을 경우 예시):
• L1: 10%
• L2: 20%
• L3: 30%
• L4: 40%
────────────────────────
3. 주요 수행 내용 (상세)
────────────────────────
[블록1]
내용1

[블록2]
내용2

[블록3]
내용3
────────────────────────
4. 문제점 및 해결 방안
────────────────────────
• 문제: p1
  해결: s1
────────────────────────
5. 차주 계획
────────────────────────
다음주
────────────────────────
6. 참고 자료 (링크)
────────────────────────
링크
"""
    raw = "작성일: 2099년 1월 1일\n" + raw
    data = weekly_txt_to_capstone_field_data(raw)
    assert data["report_date"] == "2099년 1월 1일"
    assert data["author_name"] == "김기석"
    assert data["goal_detail_1"] == "goal-a"
    assert data["prog_detail_1"] == "prog-a"
    assert data["goal_rate_1"] == "10%"
    assert data["prog_rate_1"] == "10%"
    assert "내용1" in data["s3_task_1"]
    assert data["s4_problem"] == "• p1"
    assert data["next_week"] == "다음주"
    assert "링크" in data["s6_evidence"]


def test_parse_sections_keys() -> None:
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
6. 참고 자료
────────────────────────
z
"""
    s = parse_weekly_txt_sections(raw)
    assert "s1" in s and "s2" in s and "s6" in s

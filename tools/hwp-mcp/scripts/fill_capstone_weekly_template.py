"""CLI: fill capstone weekly HWP template from UTF-8 txt. Run from repo root."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[1]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from hwp_mcp.capstone_template import fill_capstone_weekly_template  # noqa: E402


def main() -> None:
    p = argparse.ArgumentParser(
        description="Fill 캡스톤 주간보고서 누름틀 template from weekly UTF-8 txt."
    )
    p.add_argument(
        "--template",
        type=Path,
        required=True,
        help="Path to .hwp template (누름틀: report_date, goal_detail_*, …).",
    )
    p.add_argument("--txt", type=Path, required=True, help="Weekly report UTF-8 .txt")
    p.add_argument("--out", type=Path, required=True, help="Output .hwp path")
    args = p.parse_args()
    msg = fill_capstone_weekly_template(args.template, args.txt, args.out)
    print(msg)


if __name__ == "__main__":
    main()

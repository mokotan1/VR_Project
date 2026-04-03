"""
FastMCP entry — tools only; domain logic lives in automation.py.
"""

from __future__ import annotations

from typing import Any

from mcp.server.fastmcp import FastMCP

from hwp_mcp.automation import HwpAutomationError, fill_template, probe_hwp_com
from hwp_mcp.capstone_template import fill_capstone_weekly_template
from hwp_mcp.report_fill import apply_txt_to_weekly_hwp

mcp = FastMCP("hwp-automation")


@mcp.tool()
def hwp_fill_template(
    template_path: str,
    output_path: str,
    data: dict[str, Any],
) -> str:
    """
    Fill an HWP template's named fields (누름틀) and save a new .hwp file.

    - template_path: Absolute or relative path to the .hwp template
    - output_path: Target path for the new document (parent folders are created if needed)
    - data: Map of field name → text (values are converted to strings)
    """
    try:
        return fill_template(template_path, output_path, data)
    except HwpAutomationError as e:
        return f"Error: {e}"
    except Exception as e:
        return f"HWP error: {e}"


@mcp.tool()
def hwp_fill_weekly_capstone_template(
    template_path: str,
    weekly_txt_path: str,
    output_path: str,
) -> str:
    """
    Fill 캡스톤 주간보고서 HWP template (누름틀: report_date, goal_detail_*, prog_*,
    member_g1, author_name, s3_task_*, s3_deliver_*, s4_*, next_week, s6_evidence)
    from a UTF-8 weekly .txt (sections 1–6, same format as hwp_apply_weekly_txt).
    Saves a new .hwp via PutFieldText.
    """
    try:
        return fill_capstone_weekly_template(template_path, weekly_txt_path, output_path)
    except HwpAutomationError as e:
        return f"Error: {e}"
    except Exception as e:
        return f"HWP error: {e}"


@mcp.tool()
def hwp_apply_weekly_txt(
    hwp_path: str,
    weekly_txt_path: str,
    output_path: str,
    report_date_line_old: str | None = None,
    report_date_line_new: str | None = None,
) -> str:
    """
    Merge weekly report UTF-8 text (sections 1–6) into a matching .hwp and save as output_path.
    Uses Hangul AllReplace per line (multi-line find is not supported). Section 2 rows that repeat
    in the document may need manual touch-up; the tool returns warnings when applicable.
    """
    try:
        return apply_txt_to_weekly_hwp(
            hwp_path,
            weekly_txt_path,
            output_path,
            report_date_line_old=report_date_line_old,
            report_date_line_new=report_date_line_new,
        )
    except HwpAutomationError as e:
        return f"Error: {e}"
    except Exception as e:
        return f"HWP error: {e}"


@mcp.tool()
def hwp_probe() -> str:
    """
    Check whether Hangul (HWP) COM automation is available on this machine.
    Run this before batch jobs if HWP was recently installed or updated.
    """
    try:
        return probe_hwp_com()
    except HwpAutomationError as e:
        return f"Error: {e}"
    except Exception as e:
        return f"HWP probe failed: {e}"


def main() -> None:
    mcp.run(transport="stdio")


if __name__ == "__main__":
    main()

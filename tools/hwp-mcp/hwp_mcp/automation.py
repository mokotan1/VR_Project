"""
HWP COM automation — isolated from MCP transport so it can be unit-tested with mocks.
"""

from __future__ import annotations

import os
import platform
from pathlib import Path
from typing import Any, Mapping

try:
    import win32com.client as win32  # type: ignore[import-untyped]
except ImportError:  # pragma: no cover - non-Windows / no pywin32
    win32 = None  # type: ignore[assignment]

HWP_COM_PROGID = "HWPFrame.HwpObject"
SECURITY_MODULE_PAIR = ("FilePathCheckDLL", "SecurityModule")
# Hangul: suppress script/UI message boxes during automation (see SetMessageBoxMode).
HWP_MSGBOX_MODE_SUPPRESS_ALL = 0xFFFF


class HwpAutomationError(Exception):
    """Raised when HWP automation fails in a recoverable, user-facing way."""


def configure_hwp_automation_ui(hwp: object) -> int:
    """
    Hide the main window and suppress Hangul message boxes during COM automation.
    Returns the previous GetMessageBoxMode() value for restore_hwp_message_box_mode().
    """
    prev = 0
    try:
        prev = int(hwp.GetMessageBoxMode())
    except Exception:
        prev = 0
    try:
        hwp.SetMessageBoxMode(HWP_MSGBOX_MODE_SUPPRESS_ALL)
    except Exception:
        try:
            hwp.SetMessageBoxMode(1)
        except Exception:
            pass
    try:
        hwp.XHwpWindows.Item(0).Visible = False
    except Exception:
        pass
    return prev


def restore_hwp_message_box_mode(hwp: object, previous: int) -> None:
    try:
        hwp.SetMessageBoxMode(previous)
    except Exception:
        pass


def _require_windows() -> None:
    if platform.system() != "Windows":
        raise HwpAutomationError(
            "HWP COM automation is only supported on Windows with Hangul (HWP) installed."
        )


def _normalize_field_data(data: Mapping[str, Any]) -> dict[str, str]:
    out: dict[str, str] = {}
    for key, value in data.items():
        if not isinstance(key, str) or not key.strip():
            raise HwpAutomationError("Field names must be non-empty strings.")
        out[key] = "" if value is None else str(value)
    return out


def fill_template(
    template_path: str | os.PathLike[str],
    output_path: str | os.PathLike[str],
    data: Mapping[str, Any],
) -> str:
    """
    Open an HWP template, fill named fields (누름틀), save as a new file, and quit HWP.

    Returns a short success message including the absolute output path.
    """
    _require_windows()
    if win32 is None:
        raise HwpAutomationError(
            "pywin32 is not installed. Run: pip install -r tools/hwp-mcp/requirements.txt"
        )

    tpl = Path(template_path).expanduser().resolve()
    out = Path(output_path).expanduser().resolve()

    if not tpl.is_file():
        raise HwpAutomationError(f"Template not found: {tpl}")

    out.parent.mkdir(parents=True, exist_ok=True)
    fields = _normalize_field_data(data)

    hwp = win32.gencache.EnsureDispatch(HWP_COM_PROGID)
    msg_prev: int | None = None
    try:
        hwp.RegisterModule(*SECURITY_MODULE_PAIR)
        msg_prev = configure_hwp_automation_ui(hwp)

        hwp.Open(str(tpl))

        for field_name, text in fields.items():
            hwp.PutFieldText(field_name, text)

        hwp.SaveAs(str(out))
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

    return f"Saved: {out}"


def probe_hwp_com() -> str:
    """Verify that the HWP COM object can be created (no document open)."""
    _require_windows()
    if win32 is None:
        raise HwpAutomationError(
            "pywin32 is not installed. Run: pip install -r tools/hwp-mcp/requirements.txt"
        )

    hwp = win32.gencache.EnsureDispatch(HWP_COM_PROGID)
    msg_prev: int | None = None
    try:
        hwp.RegisterModule(*SECURITY_MODULE_PAIR)
        msg_prev = configure_hwp_automation_ui(hwp)
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

    return "HWP COM is available (Hangul automation can run)."

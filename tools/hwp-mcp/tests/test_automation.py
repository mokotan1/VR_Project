"""Unit tests for HWP automation helpers (COM is mocked)."""

from __future__ import annotations

import sys
from pathlib import Path
from unittest.mock import MagicMock, patch

import pytest

# Ensure package root on path when running pytest from repo root
_ROOT = Path(__file__).resolve().parents[1]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from hwp_mcp.automation import (  # noqa: E402
    HwpAutomationError,
    _normalize_field_data,
    fill_template,
)


def test_normalize_field_data_coerces_and_rejects_empty_keys() -> None:
    assert _normalize_field_data({"a": 1, "b": None}) == {"a": "1", "b": ""}
    with pytest.raises(HwpAutomationError, match="Field names"):
        _normalize_field_data({"": "x"})


@patch("hwp_mcp.automation.platform.system", return_value="Linux")
def test_fill_template_rejects_non_windows(mock_system: MagicMock) -> None:
    with pytest.raises(HwpAutomationError, match="only supported on Windows"):
        fill_template("/x/template.hwp", "/x/out.hwp", {})


@patch("hwp_mcp.automation.platform.system", return_value="Windows")
@patch("hwp_mcp.automation.win32")
def test_fill_template_happy_path(
    mock_win32: MagicMock,
    _mock_platform: MagicMock,
    tmp_path: Path,
) -> None:
    tpl = tmp_path / "t.hwp"
    tpl.write_bytes(b"\x00")
    out = tmp_path / "sub" / "o.hwp"

    mock_hwp = MagicMock()
    mock_hwp.GetMessageBoxMode.return_value = 0
    mock_win32.gencache.EnsureDispatch.return_value = mock_hwp

    msg = fill_template(tpl, out, {"Title": "Hello"})

    assert "Saved:" in msg
    assert str(out.resolve()) in msg
    mock_hwp.Open.assert_called_once()
    mock_hwp.PutFieldText.assert_called_once_with("Title", "Hello")
    mock_hwp.SaveAs.assert_called_once_with(str(out.resolve()))
    mock_hwp.Quit.assert_called()

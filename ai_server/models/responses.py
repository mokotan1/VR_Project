from __future__ import annotations

from typing import Any

from pydantic import BaseModel


class WSEvent(BaseModel):
    """WebSocket event sent to Unity. Compatible with newCapstone SSEEvent schema."""

    type: str
    content: str | None = None
    text: str | None = None
    name: str | None = None
    arguments: dict[str, Any] | None = None
    full_text: str | None = None

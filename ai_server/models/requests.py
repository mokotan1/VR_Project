from __future__ import annotations

from pydantic import BaseModel


class ChatRequest(BaseModel):
    """Incoming chat message from Unity via WebSocket."""

    prompt: str
    system: str = "당신은 FPS VR 게임의 AI 도우미입니다."
    use_tools: bool = True

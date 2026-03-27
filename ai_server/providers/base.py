from __future__ import annotations

from abc import ABC, abstractmethod
from typing import AsyncIterator

from models.responses import WSEvent


class AIProvider(ABC):
    """Abstract base for AI providers (DIP: high-level modules depend on this interface)."""

    @property
    @abstractmethod
    def name(self) -> str: ...

    @abstractmethod
    async def stream_chat(
        self,
        messages: list[dict],
        tools: list[dict] | None = None,
        temperature: float = 0.5,
        max_tokens: int = 256,
    ) -> AsyncIterator[WSEvent]:
        yield  # pragma: no cover

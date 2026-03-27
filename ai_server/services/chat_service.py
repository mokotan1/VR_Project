from __future__ import annotations

import logging
from typing import AsyncIterator

from models.requests import ChatRequest
from models.responses import WSEvent
from providers.base import AIProvider
from tools.registry import ToolRegistry

logger = logging.getLogger(__name__)


class ChatService:
    """Orchestrates LLM calls with tool injection.
    Adapted from newCapstone backend_ai/services/chat_service.py.
    """

    _TOOL_INSTRUCTION = (
        "\n\n[중요: 응답 방식] "
        "1) 반드시 캐릭터 대사를 텍스트로 먼저 말하세요. "
        "2) 텍스트와 별개로, 제공된 tool/function을 호출하여 게임 액션을 지시하세요. "
        "3) 절대로 텍스트 안에 function call 구문이나 JSON을 넣지 마세요. "
        "텍스트 응답과 tool 호출은 완전히 분리되어야 합니다."
    )

    def __init__(
        self,
        provider: AIProvider,
        registry: ToolRegistry,
        temperature: float = 0.5,
        max_tokens: int = 256,
    ) -> None:
        self._provider = provider
        self._registry = registry
        self._temperature = temperature
        self._max_tokens = max_tokens

    def _build_messages(self, request: ChatRequest) -> list[dict]:
        system_content = request.system
        if request.use_tools and len(self._registry) > 0:
            system_content += self._TOOL_INSTRUCTION
        return [
            {"role": "system", "content": system_content},
            {"role": "user", "content": request.prompt},
        ]

    def _get_tools(self, request: ChatRequest) -> list[dict] | None:
        if not request.use_tools or len(self._registry) == 0:
            return None
        return self._registry.get_all_openai_format()

    async def stream_chat(self, request: ChatRequest) -> AsyncIterator[WSEvent]:
        messages = self._build_messages(request)
        tools = self._get_tools(request)

        try:
            logger.info("Streaming via %s", self._provider.name)
            async for event in self._provider.stream_chat(
                messages=messages,
                tools=tools,
                temperature=self._temperature,
                max_tokens=self._max_tokens,
            ):
                yield event
        except Exception as exc:
            logger.error("Provider %s failed: %s", self._provider.name, exc, exc_info=True)
            yield WSEvent(type="error", content=f"AI 엔진 오류: {exc}")
            yield WSEvent(type="done", full_text="")

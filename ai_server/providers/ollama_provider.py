from __future__ import annotations

import json
import logging
from typing import AsyncIterator

import httpx

from models.responses import WSEvent
from providers.base import AIProvider

logger = logging.getLogger(__name__)


class OllamaProvider(AIProvider):
    """Streams chat completions from a local Ollama instance via its REST API."""

    def __init__(
        self,
        base_url: str = "http://localhost:11434",
        model: str = "llama3:latest",
        num_ctx: int = 2048,
    ) -> None:
        self._base_url = base_url.rstrip("/")
        self._model = model
        self._num_ctx = num_ctx

    @property
    def name(self) -> str:
        return f"ollama/{self._model}"

    async def stream_chat(
        self,
        messages: list[dict],
        tools: list[dict] | None = None,
        temperature: float = 0.5,
        max_tokens: int = 256,
    ) -> AsyncIterator[WSEvent]:
        payload: dict = {
            "model": self._model,
            "messages": messages,
            "stream": True,
            "options": {
                "temperature": temperature,
                "num_predict": max_tokens,
                "num_ctx": self._num_ctx,
            },
        }
        if tools:
            payload["tools"] = tools

        full_text_parts: list[str] = []
        tool_calls_buffer: list[dict] = []

        async with httpx.AsyncClient(timeout=httpx.Timeout(120.0)) as client:
            async with client.stream(
                "POST",
                f"{self._base_url}/api/chat",
                json=payload,
            ) as response:
                response.raise_for_status()

                async for raw_line in response.aiter_lines():
                    if not raw_line.strip():
                        continue

                    try:
                        chunk = json.loads(raw_line)
                    except json.JSONDecodeError:
                        logger.warning("Skipping malformed Ollama chunk: %s", raw_line[:120])
                        continue

                    msg = chunk.get("message", {})

                    content = msg.get("content", "")
                    if content:
                        full_text_parts.append(content)
                        yield WSEvent(type="text_delta", content=content)

                    for tc in msg.get("tool_calls", []):
                        fn = tc.get("function", {})
                        fn_name = fn.get("name", "")
                        fn_args = fn.get("arguments", {})
                        if fn_name:
                            tool_calls_buffer.append({"name": fn_name, "arguments": fn_args})
                            yield WSEvent(
                                type="function_call",
                                name=fn_name,
                                arguments=fn_args,
                            )

                    if chunk.get("done", False):
                        break

        yield WSEvent(type="done", full_text="".join(full_text_parts))

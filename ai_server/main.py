from __future__ import annotations

import asyncio
import json
import logging

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import uvicorn

from config import get_settings
from models.requests import ChatRequest
from models.responses import WSEvent
from providers.ollama_provider import OllamaProvider
from services.chat_service import ChatService
from tools.game_tools import GAME_TOOLS
from tools.registry import ToolRegistry

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Bootstrap (lightweight only — no heavy ML imports at startup)
# ---------------------------------------------------------------------------
settings = get_settings()

app = FastAPI(title="VR FPS AI Server")

registry = ToolRegistry()
registry.register_many(GAME_TOOLS)

ollama = OllamaProvider(
    base_url=settings.ollama_base_url,
    model=settings.ollama_model,
    num_ctx=settings.ollama_num_ctx,
)

chat_service = ChatService(
    provider=ollama,
    registry=registry,
    temperature=settings.ollama_temperature,
    max_tokens=settings.ollama_max_tokens,
)

_whisper_service = None


def _get_whisper():
    """Lazy-load whisper to avoid slow startup from heavy ML imports."""
    global _whisper_service
    if _whisper_service is None:
        from services.whisper_service import WhisperService
        _whisper_service = WhisperService(
            model_size=settings.whisper_model_size,
            device=settings.whisper_device,
            compute_type=settings.whisper_compute_type,
            language=settings.whisper_language,
        )
    return _whisper_service


# ---------------------------------------------------------------------------
# Health
# ---------------------------------------------------------------------------
@app.get("/")
def health_check():
    return {"status": "online", "model": settings.ollama_model}


# ---------------------------------------------------------------------------
# WebSocket
# ---------------------------------------------------------------------------
@app.websocket("/ws")
async def websocket_endpoint(ws: WebSocket):
    await ws.accept()
    logger.info("WebSocket client connected")

    audio_buffer = bytearray()
    is_recording = False

    try:
        while True:
            raw = await ws.receive()

            if "bytes" in raw and raw["bytes"]:
                if is_recording:
                    audio_buffer.extend(raw["bytes"])
                continue

            if "text" in raw and raw["text"]:
                try:
                    msg = json.loads(raw["text"])
                except json.JSONDecodeError:
                    await _send_event(ws, WSEvent(type="error", content="Invalid JSON"))
                    continue

                msg_type = msg.get("type", "")

                if msg_type == "audio_start":
                    audio_buffer.clear()
                    is_recording = True

                elif msg_type == "audio_end":
                    is_recording = False
                    await _handle_audio(ws, bytes(audio_buffer), msg)
                    audio_buffer.clear()

                elif msg_type == "chat":
                    await _handle_chat(ws, msg)

                else:
                    await _send_event(ws, WSEvent(type="error", content=f"Unknown type: {msg_type}"))

    except WebSocketDisconnect:
        logger.info("WebSocket client disconnected")
    except Exception as exc:
        logger.error("WebSocket error: %s", exc, exc_info=True)
        try:
            await _send_event(ws, WSEvent(type="error", content=str(exc)))
        except Exception:
            pass


async def _handle_audio(ws: WebSocket, pcm_bytes: bytes, msg: dict) -> None:
    if not pcm_bytes:
        await _send_event(ws, WSEvent(type="error", content="Empty audio buffer"))
        return

    loop = asyncio.get_running_loop()

    from services.vad_service import audio_has_energy
    has_energy = await loop.run_in_executor(None, audio_has_energy, pcm_bytes)
    if not has_energy:
        await _send_event(ws, WSEvent(type="transcription", text=""))
        return

    whisper = _get_whisper()
    text = await loop.run_in_executor(None, whisper.transcribe, pcm_bytes)
    await _send_event(ws, WSEvent(type="transcription", text=text))

    if text.strip() and msg.get("auto_chat", True):
        chat_msg = {
            "type": "chat",
            "prompt": text,
            "system": msg.get("system", "당신은 FPS VR 게임의 AI 도우미입니다."),
            "use_tools": msg.get("use_tools", True),
        }
        await _handle_chat(ws, chat_msg)


async def _handle_chat(ws: WebSocket, msg: dict) -> None:
    request = ChatRequest(
        prompt=msg.get("prompt", ""),
        system=msg.get("system", "당신은 FPS VR 게임의 AI 도우미입니다."),
        use_tools=msg.get("use_tools", True),
    )

    if not request.prompt.strip():
        await _send_event(ws, WSEvent(type="error", content="Empty prompt"))
        return

    async for event in chat_service.stream_chat(request):
        await _send_event(ws, event)


async def _send_event(ws: WebSocket, event: WSEvent) -> None:
    await ws.send_text(event.model_dump_json(exclude_none=True))


# ---------------------------------------------------------------------------
# Entry
# ---------------------------------------------------------------------------
if __name__ == "__main__":
    uvicorn.run(
        app,
        host=settings.ws_host,
        port=settings.ws_port,
        log_level="info",
    )

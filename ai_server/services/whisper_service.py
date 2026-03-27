from __future__ import annotations

import io
import logging
import tempfile

import numpy as np
from faster_whisper import WhisperModel

logger = logging.getLogger(__name__)


class WhisperService:
    """faster-whisper wrapper for speech-to-text."""

    def __init__(
        self,
        model_size: str = "small",
        device: str = "cuda",
        compute_type: str = "float16",
        language: str = "ko",
    ) -> None:
        self._language = language
        logger.info("Loading Whisper model '%s' on %s (%s)...", model_size, device, compute_type)
        self._model = WhisperModel(
            model_size,
            device=device,
            compute_type=compute_type,
        )
        logger.info("Whisper model loaded.")

    def transcribe(self, pcm_bytes: bytes, sample_rate: int = 16000) -> str:
        if len(pcm_bytes) < 2:
            return ""

        audio = np.frombuffer(pcm_bytes, dtype=np.int16).astype(np.float32) / 32767.0

        min_samples = int(sample_rate * 0.5)
        if len(audio) < min_samples:
            return ""

        segments, info = self._model.transcribe(
            audio,
            language=self._language,
            beam_size=3,
            vad_filter=True,
            vad_parameters=dict(
                min_silence_duration_ms=300,
                speech_pad_ms=100,
            ),
        )

        text_parts: list[str] = []
        for segment in segments:
            text_parts.append(segment.text.strip())

        result = " ".join(text_parts).strip()
        logger.info("Transcription (%s, %.1fs): %s", self._language, info.duration, result)
        return result

"""VAD is handled by faster-whisper's built-in vad_filter (onnxruntime-based).
This module is kept as a thin wrapper for any pre-filtering logic
that may be needed before sending audio to Whisper.
"""
from __future__ import annotations

import logging

import numpy as np

logger = logging.getLogger(__name__)

SILENCE_THRESHOLD_RMS = 200


def audio_has_energy(pcm_bytes: bytes, threshold: float = SILENCE_THRESHOLD_RMS) -> bool:
    """Quick energy check to discard completely silent buffers without invoking Whisper."""
    if len(pcm_bytes) < 2:
        return False

    samples = np.frombuffer(pcm_bytes, dtype=np.int16).astype(np.float64)
    rms = np.sqrt(np.mean(samples ** 2))
    return rms > threshold

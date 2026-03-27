using System;
using UnityEngine;

namespace VRProject.Infrastructure.XR
{
    public sealed class MicrophoneCaptureAdapter : MonoBehaviour
    {
        private const int SampleRate = 16000;
        private const int RecordLengthSec = 60;

        private AudioClip _clip;
        private string _deviceName;
        private int _lastSamplePos;
        private bool _isRecording;

        public bool IsRecording => _isRecording;

        public event Action<byte[]> OnAudioChunkReady;

        public void StartCapture(string deviceName = null)
        {
            if (_isRecording) return;

            _deviceName = deviceName;
            _clip = Microphone.Start(_deviceName, true, RecordLengthSec, SampleRate);
            _lastSamplePos = 0;
            _isRecording = true;
        }

        public void StopCapture()
        {
            if (!_isRecording) return;

            _isRecording = false;
            Microphone.End(_deviceName);
            _clip = null;
        }

        /// <summary>
        /// Returns all recorded PCM data since StartCapture was called,
        /// then stops the microphone.
        /// </summary>
        public byte[] StopAndGetAllAudio()
        {
            if (!_isRecording || _clip == null) return Array.Empty<byte>();

            int currentPos = Microphone.GetPosition(_deviceName);
            _isRecording = false;
            Microphone.End(_deviceName);

            if (currentPos <= 0)
            {
                _clip = null;
                return Array.Empty<byte>();
            }

            var samples = new float[currentPos];
            _clip.GetData(samples, 0);
            _clip = null;

            return FloatToPcm16(samples);
        }

        private void Update()
        {
            if (!_isRecording || _clip == null) return;

            int currentPos = Microphone.GetPosition(_deviceName);
            if (currentPos == _lastSamplePos) return;

            int sampleCount;
            float[] samples;

            if (currentPos > _lastSamplePos)
            {
                sampleCount = currentPos - _lastSamplePos;
                samples = new float[sampleCount];
                _clip.GetData(samples, _lastSamplePos);
            }
            else
            {
                int totalSamples = _clip.samples;
                sampleCount = (totalSamples - _lastSamplePos) + currentPos;
                samples = new float[sampleCount];

                var tailLen = totalSamples - _lastSamplePos;
                var tail = new float[tailLen];
                _clip.GetData(tail, _lastSamplePos);
                Array.Copy(tail, 0, samples, 0, tailLen);

                if (currentPos > 0)
                {
                    var head = new float[currentPos];
                    _clip.GetData(head, 0);
                    Array.Copy(head, 0, samples, tailLen, currentPos);
                }
            }

            _lastSamplePos = currentPos;

            byte[] pcm = FloatToPcm16(samples);
            OnAudioChunkReady?.Invoke(pcm);
        }

        private static byte[] FloatToPcm16(float[] samples)
        {
            var pcm = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                float clamped = Mathf.Clamp(samples[i], -1f, 1f);
                short val = (short)(clamped * 32767f);
                pcm[i * 2] = (byte)(val & 0xFF);
                pcm[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
            }
            return pcm;
        }
    }
}

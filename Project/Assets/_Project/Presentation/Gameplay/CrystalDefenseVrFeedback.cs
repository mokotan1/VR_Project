using UnityEngine;
using UnityEngine.XR;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// 크리스탈 피해 / 웨이브 시작·클리어 / 승패 등 게임 루프 핵심 이벤트에
    /// 양손 컨트롤러 햅틱 펄스를 전달하는 중앙 허브.
    /// XR 디바이스가 없을 때는 무음으로 작동 (방어적).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrystalDefenseVrFeedback : MonoBehaviour
    {
        const float MinHapticDuration = 0.01f;

        [SerializeField] CrystalCoreHealth _crystal;
        [SerializeField] CrystalDefenseWaveDirector _waveDirector;

        [Header("Amplitudes (0..1)")]
        [SerializeField, Range(0f, 1f)] float _crystalDamageAmplitude = 0.35f;
        [SerializeField, Range(0f, 1f)] float _waveStartAmplitude = 0.18f;
        [SerializeField, Range(0f, 1f)] float _waveClearAmplitude = 0.25f;
        [SerializeField, Range(0f, 1f)] float _lostAmplitude = 0.75f;
        [SerializeField, Range(0f, 1f)] float _wonAmplitude = 0.45f;

        [Header("Durations (seconds)")]
        [SerializeField] float _durationSeconds = 0.08f;
        [SerializeField] float _lostDurationSeconds = 0.25f;
        [SerializeField] float _wonDurationSeconds = 0.18f;

        void OnEnable()
        {
            if (_crystal != null)
                _crystal.Damaged += OnCrystalDamaged;
            if (_waveDirector != null)
            {
                _waveDirector.WaveStarted += OnWaveStarted;
                _waveDirector.WaveCleared += OnWaveCleared;
                _waveDirector.Lost += OnLost;
                _waveDirector.Won += OnWon;
            }
        }

        void OnDisable()
        {
            if (_crystal != null)
                _crystal.Damaged -= OnCrystalDamaged;
            if (_waveDirector != null)
            {
                _waveDirector.WaveStarted -= OnWaveStarted;
                _waveDirector.WaveCleared -= OnWaveCleared;
                _waveDirector.Lost -= OnLost;
                _waveDirector.Won -= OnWon;
            }
        }

        void OnCrystalDamaged(float _, float __, Vector3 ___) => PulseBoth(_crystalDamageAmplitude, _durationSeconds);
        void OnWaveStarted(int _) => PulseBoth(_waveStartAmplitude, _durationSeconds);
        void OnWaveCleared(int _) => PulseBoth(_waveClearAmplitude, _durationSeconds);
        void OnLost() => PulseBoth(_lostAmplitude, _lostDurationSeconds);
        void OnWon() => PulseBoth(_wonAmplitude, _wonDurationSeconds);

        static void PulseBoth(float amplitude, float duration)
        {
            Pulse(XRNode.LeftHand, amplitude, duration);
            Pulse(XRNode.RightHand, amplitude, duration);
        }

        static void Pulse(XRNode node, float amplitude, float duration)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
                return;
            device.SendHapticImpulse(0, Mathf.Clamp01(amplitude), Mathf.Max(MinHapticDuration, duration));
        }
    }
}

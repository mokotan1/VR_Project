using UnityEngine;
using VRProject.Application.Startup;
using VRProject.Presentation.Startup;

namespace VRProject.Presentation.Combat
{
    [DisallowMultipleComponent]
    public sealed class WeaponHapticFeedback : MonoBehaviour
    {
        [SerializeField] WeaponHitDetector _detector;
        [SerializeField] WeaponAttackProfile _profile;
        [SerializeField] ParryWindow _parryWindow;

        public void BindSetup(WeaponHitDetector detector, WeaponAttackProfile profile, ParryWindow parryWindow = null)
        {
            if (detector != null)
                _detector = detector;
            if (profile != null)
                _profile = profile;
            if (parryWindow != null)
                _parryWindow = parryWindow;
        }

        void Awake()
        {
            if (_detector == null)
                _detector = GetComponent<WeaponHitDetector>();
        }

        void OnEnable()
        {
            if (_detector != null)
                _detector.HitConfirmed += OnHitConfirmed;
            if (_parryWindow != null)
            {
                _parryWindow.Blocked += OnBlocked;
                _parryWindow.Parried += OnParried;
            }
        }

        void OnDisable()
        {
            if (_detector != null)
                _detector.HitConfirmed -= OnHitConfirmed;
            if (_parryWindow != null)
            {
                _parryWindow.Blocked -= OnBlocked;
                _parryWindow.Parried -= OnParried;
            }
        }

        void OnHitConfirmed(WeaponHitContext context)
        {
            if (!ShouldUseVrHaptics())
                return;

            var amplitude = (_profile != null ? _profile.HitHapticAmplitude : 0.5f)
                            * (context.Zone != null ? context.Zone.FeedbackMultiplier : 1f);
            var duration = _profile != null ? _profile.HitHapticDurationSeconds : 0.08f;
            VrHapticChannel.PulseBoth(amplitude, duration);
        }

        void OnBlocked()
        {
            if (!ShouldUseVrHaptics())
                return;

            var amplitude = _profile != null ? _profile.BlockHapticAmplitude : 0.35f;
            VrHapticChannel.PulseBoth(amplitude, 0.06f);
        }

        void OnParried()
        {
            if (!ShouldUseVrHaptics())
                return;

            VrHapticChannel.PulseBoth(0.75f, 0.12f);
        }

        static bool ShouldUseVrHaptics()
        {
            var availability = new PlayModeAvailability(mobileAvailable: true, vrAvailable: UnityEngine.XR.XRSettings.isDeviceActive);
            return PlayModeSession.GetSelectedModeOrFallback(availability) == PlayModeKind.Vr;
        }
    }
}

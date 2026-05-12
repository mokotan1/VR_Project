using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WeaponFireScreenImpulse : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] Transform _cameraTransform;
        [SerializeField] Volume _volume;
        [SerializeField] VolumeProfile _volumeProfile;

        [Header("Timing")]
        [SerializeField, Range(0.02f, 0.5f)] float _durationSeconds = 0.16f;

        [Header("Camera Kick")]
        [SerializeField, Range(0f, 3f)] float _cameraKickStrength = 1f;
        [SerializeField, Range(0f, 1f)] float _xrComfortMultiplier = 0.25f;
        [SerializeField] Vector3 _kickLocalPosition = new Vector3(0f, -0.012f, -0.035f);
        [SerializeField] Vector3 _kickLocalEuler = new Vector3(-1.1f, 0.25f, 0f);

        [Header("Post Processing Pulse")]
        [SerializeField, Range(-1f, 1f)] float _lensDistortionPulse = -0.22f;
        [SerializeField, Range(0f, 1f)] float _chromaticAberrationPulse = 0.45f;
        [SerializeField, Range(0f, 1f)] float _vignettePulse = 0.16f;

        bool _active;
        float _elapsed;
        bool _hasBasePose;
        Vector3 _baseLocalPosition;
        Quaternion _baseLocalRotation;

        LensDistortion _lensDistortion;
        ChromaticAberration _chromaticAberration;
        Vignette _vignette;

        bool _hasLensOriginal;
        bool _hasChromaticOriginal;
        bool _hasVignetteOriginal;
        float _lensOriginal;
        float _chromaticOriginal;
        float _vignetteOriginal;

        public void Trigger()
        {
            EnsureReferences();
            CaptureBasePose();
            CapturePostOriginals();
            _elapsed = 0f;
            _active = true;
        }

        void Awake()
        {
            EnsureReferences();
            CaptureBasePose();
            CapturePostOriginals();
        }

        void OnEnable()
        {
            EnsureReferences();
            CaptureBasePose();
            CapturePostOriginals();
        }

        void OnDisable()
        {
            RestoreCamera();
            RestorePostProcessing();
            _active = false;
        }

        void LateUpdate()
        {
            if (!_active)
                return;

            _elapsed += Time.unscaledDeltaTime;
            var weight = WeaponFireScreenImpulseProfile.PulseWeight(_elapsed, _durationSeconds);
            if (weight <= 0f)
            {
                RestoreCamera();
                RestorePostProcessing();
                _active = false;
                return;
            }

            ApplyCameraKick(weight);
            ApplyPostProcessing(weight);
        }

        void EnsureReferences()
        {
            if (_cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>() ?? Camera.main;
                if (cam != null)
                    _cameraTransform = cam.transform;
            }

            if (_volume == null)
                _volume = GetComponentInChildren<Volume>();

            if (_volume == null)
            {
                _volume = gameObject.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 100f;
            }

            if (_volumeProfile == null && _volume != null)
                _volumeProfile = _volume.profile;

            if (_volumeProfile == null && _volume != null)
            {
                _volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                _volumeProfile.name = "RuntimeWeaponFireScreenImpulseProfile";
                _volume.profile = _volumeProfile;
            }

            if (_volumeProfile == null)
                return;

            if (!_volumeProfile.TryGet(out _lensDistortion))
                _lensDistortion = _volumeProfile.Add<LensDistortion>(true);
            if (!_volumeProfile.TryGet(out _chromaticAberration))
                _chromaticAberration = _volumeProfile.Add<ChromaticAberration>(true);
            if (!_volumeProfile.TryGet(out _vignette))
                _vignette = _volumeProfile.Add<Vignette>(true);

            _lensDistortion.active = true;
            _chromaticAberration.active = true;
            _vignette.active = true;
        }

        void CaptureBasePose()
        {
            if (_cameraTransform == null || _hasBasePose)
                return;

            _baseLocalPosition = _cameraTransform.localPosition;
            _baseLocalRotation = _cameraTransform.localRotation;
            _hasBasePose = true;
        }

        void CapturePostOriginals()
        {
            if (_lensDistortion != null && !_hasLensOriginal)
            {
                _lensOriginal = _lensDistortion.intensity.value;
                _hasLensOriginal = true;
            }

            if (_chromaticAberration != null && !_hasChromaticOriginal)
            {
                _chromaticOriginal = _chromaticAberration.intensity.value;
                _hasChromaticOriginal = true;
            }

            if (_vignette != null && !_hasVignetteOriginal)
            {
                _vignetteOriginal = _vignette.intensity.value;
                _hasVignetteOriginal = true;
            }
        }

        void ApplyCameraKick(float weight)
        {
            if (_cameraTransform == null || !_hasBasePose)
                return;

            var xrActive = XRSettings.isDeviceActive;
            var strength = WeaponFireScreenImpulseProfile.EffectiveKickStrength(
                _cameraKickStrength,
                _xrComfortMultiplier,
                xrActive);
            var scaled = strength * weight;
            _cameraTransform.localPosition = _baseLocalPosition + _kickLocalPosition * scaled;
            _cameraTransform.localRotation = _baseLocalRotation * Quaternion.Euler(_kickLocalEuler * scaled);
        }

        void ApplyPostProcessing(float weight)
        {
            if (_lensDistortion != null && _hasLensOriginal)
                _lensDistortion.intensity.value = _lensOriginal + _lensDistortionPulse * weight;

            if (_chromaticAberration != null && _hasChromaticOriginal)
                _chromaticAberration.intensity.value = Mathf.Clamp01(_chromaticOriginal + _chromaticAberrationPulse * weight);

            if (_vignette != null && _hasVignetteOriginal)
                _vignette.intensity.value = Mathf.Clamp01(_vignetteOriginal + _vignettePulse * weight);
        }

        void RestoreCamera()
        {
            if (_cameraTransform == null || !_hasBasePose)
                return;

            _cameraTransform.localPosition = _baseLocalPosition;
            _cameraTransform.localRotation = _baseLocalRotation;
        }

        void RestorePostProcessing()
        {
            if (_lensDistortion != null && _hasLensOriginal)
                _lensDistortion.intensity.value = _lensOriginal;

            if (_chromaticAberration != null && _hasChromaticOriginal)
                _chromaticAberration.intensity.value = _chromaticOriginal;

            if (_vignette != null && _hasVignetteOriginal)
                _vignette.intensity.value = _vignetteOriginal;
        }
    }
}

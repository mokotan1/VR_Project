using System;
using System.Collections;
using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Flashes an orange material tint on all child renderers when the enemy is hit.
    /// Works with <see cref="OsFpsInspiredDamageable"/> damage events and
    /// <see cref="SuperhotEnemy"/> delayed kill feedback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHitColorTint : MonoBehaviour
    {
        [SerializeField] Color _hitTint = EnemyHitColorTintApplier.DefaultHitTint;
        [SerializeField] float _flashSeconds = 0.12f;
        [SerializeField] float _killFlashSeconds = 0.1f;

        Renderer[] _renderers = Array.Empty<Renderer>();
        MaterialPropertyBlock _block;
        Coroutine _flashRoutine;
        OsFpsInspiredDamageable _damageable;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            _damageable = GetComponent<OsFpsInspiredDamageable>();
        }

        void OnEnable()
        {
            if (_damageable != null)
                _damageable.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (_damageable != null)
                _damageable.Damaged -= OnDamaged;
            StopFlashRoutine();
            ClearTint();
        }

        void OnDamaged(float amount, Vector3 hitPoint)
        {
            if (amount <= 0f)
                return;

            PlayHitFlash(null);
        }

        public void PlayHitFlash(Action onComplete)
        {
            if (!isActiveAndEnabled)
            {
                onComplete?.Invoke();
                return;
            }

            StopFlashRoutine();
            _flashRoutine = StartCoroutine(FlashRoutine(onComplete));
        }

        public void PlayKillFlash(Action onComplete)
        {
            PlayHitFlash(onComplete);
        }

        public float KillFlashDuration => _killFlashSeconds;

        public void ApplyTintImmediate()
        {
            RefreshRenderers();
            ApplyTint();
        }

        IEnumerator FlashRoutine(Action onComplete)
        {
            ApplyTint();

            var duration = onComplete != null ? _killFlashSeconds : _flashSeconds;
            if (duration > 0f)
                yield return new WaitForSeconds(duration);

            if (onComplete == null)
                ClearTint();

            onComplete?.Invoke();
            _flashRoutine = null;
        }

        void StopFlashRoutine()
        {
            if (_flashRoutine == null)
                return;

            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }

        void ApplyTint()
        {
            RefreshRenderers();
            for (var i = 0; i < _renderers.Length; i++)
                EnemyHitColorTintApplier.ApplyTint(_renderers[i], _hitTint, _block);
        }

        void RefreshRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        void ClearTint()
        {
            for (var i = 0; i < _renderers.Length; i++)
                EnemyHitColorTintApplier.ClearTint(_renderers[i]);
        }
    }
}

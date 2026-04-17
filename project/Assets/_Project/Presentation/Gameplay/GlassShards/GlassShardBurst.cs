using UnityEngine;
using UnityEngine.Events;

namespace GlassShards
{
    /// <summary>
    /// Plays the attached <see cref="ParticleSystem"/> mesh burst (glass shard look uses
    /// <c>GlassShard_URP</c> Shader Graph + GlassShard material). Tune burst count on the
    /// <see cref="ParticleSystem"/> (Emission bursts) or duplicate the prefab for variants.
    /// VR: lower max particles / burst count if frame time spikes; reduce distortion on the material if used.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class GlassShardBurst : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem _particleSystem;

        [SerializeField]
        private UnityEvent _onPlayed;

        private void Awake()
        {
            if (_particleSystem == null)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }
        }

        private void OnValidate()
        {
            if (_particleSystem == null)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }
        }

        /// <summary>Emit one burst from the configured particle system.</summary>
        public void PlayBurst()
        {
            if (_particleSystem == null)
            {
                return;
            }

            _particleSystem.Clear(withChildren: true);
            _particleSystem.Play(withChildren: true);
            _onPlayed?.Invoke();
        }
    }
}

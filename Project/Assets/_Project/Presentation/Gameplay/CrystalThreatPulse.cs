using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CrystalThreatPulse : MonoBehaviour
    {
        [SerializeField] CrystalCoreHealth _crystal;
        [SerializeField] float _nearThreatDistance = 3f;
        [SerializeField] float _farThreatDistance = 18f;
        [SerializeField] float _baseRadius = 1.45f;
        [SerializeField] float _pulseRadius = 0.35f;
        [SerializeField] Color _safeColor = new Color(1f, 0.85f, 0.1f, 0.3f);
        [SerializeField] Color _dangerColor = new Color(1f, 0.08f, 0.05f, 1f);

        LineRenderer _ring;

        void Awake()
        {
            if (_crystal == null)
                _crystal = FindAnyObjectByType<CrystalCoreHealth>();
            EnsureRing();
        }

        void LateUpdate()
        {
            if (_crystal == null || _ring == null)
                return;

            var threat = ComputeHighestThreat();
            _ring.enabled = threat > 0.01f;
            if (!_ring.enabled)
                return;

            var pulse = Mathf.Sin(Time.time * Mathf.Lerp(3f, 8f, threat)) * 0.5f + 0.5f;
            var radius = _baseRadius + _pulseRadius * threat * pulse;
            var color = Color.Lerp(_safeColor, _dangerColor, threat);

            _ring.startColor = color;
            _ring.endColor = color;
            DrawRing(radius);
        }

        float ComputeHighestThreat()
        {
            var enemies = FindObjectsByType<CrystalDefenseEnemyObjective>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            var highest = 0f;
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                    continue;

                var distance = Vector3.Distance(_crystal.transform.position, enemy.transform.position);
                highest = Mathf.Max(highest, CrystalDefenseAwarenessMath.Threat01(distance, _nearThreatDistance, _farThreatDistance));
            }

            return highest;
        }

        void EnsureRing()
        {
            if (_ring != null)
                return;

            var go = new GameObject("Crystal Threat Ring");
            go.transform.SetParent(transform, false);

            _ring = go.AddComponent<LineRenderer>();
            _ring.useWorldSpace = true;
            _ring.loop = true;
            _ring.positionCount = 64;
            _ring.widthMultiplier = 0.06f;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;
            _ring.enabled = false;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
                _ring.sharedMaterial = new Material(shader);
        }

        void DrawRing(float radius)
        {
            var center = _crystal.transform.position + Vector3.up * 0.06f;
            for (var i = 0; i < _ring.positionCount; i++)
            {
                var t = i / (float)_ring.positionCount * Mathf.PI * 2f;
                var p = center + new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius);
                _ring.SetPosition(i, p);
            }
        }
    }
}

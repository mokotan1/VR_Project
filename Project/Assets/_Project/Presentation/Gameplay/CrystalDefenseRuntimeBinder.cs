using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// 씬 내의 EnemyObjective 컴포넌트가 Crystal 참조 없이 생성된 경우
    /// 런타임에 자동 결합해주는 안전망 (씬 메뉴/외부 스폰 누락 대비).
    /// 풀링/저빈도 Update로 동작하여 비용을 최소화한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrystalDefenseRuntimeBinder : MonoBehaviour
    {
        const float RebindIntervalSeconds = 0.5f;

        [SerializeField] CrystalCoreHealth _crystal;
        [SerializeField] CrystalDefenseWaveDirector _director;

        float _nextRebindTime;

        void Awake()
        {
            if (_crystal == null)
                _crystal = FindAnyObjectByType<CrystalCoreHealth>();
            if (_director == null)
                _director = FindAnyObjectByType<CrystalDefenseWaveDirector>();

            BindExistingEnemies();
        }

        void Update()
        {
            if (Time.time < _nextRebindTime)
                return;

            _nextRebindTime = Time.time + RebindIntervalSeconds;
            BindExistingEnemies();
        }

        void BindExistingEnemies()
        {
            if (_crystal == null)
                return;

            var objectives = FindObjectsByType<CrystalDefenseEnemyObjective>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (var objective in objectives)
            {
                if (objective != null && objective.Crystal == null)
                    objective.Crystal = _crystal;
            }
        }
    }
}

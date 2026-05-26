using System;
using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// 직렬화 가능한 웨이브 데이터. WaveDirector가 인스펙터에서 배열로 노출한다.
    /// Normalized()로 유효성 검증을 거친 복사본을 사용하여 런타임 음수/0 값을 방지한다.
    /// </summary>
    [Serializable]
    public struct CrystalDefenseWaveDefinition
    {
        const float MinSpawnInterval = 0.05f;

        public GameObject EnemyPrefab;
        public int EnemyCount;
        public float SpawnIntervalSeconds;
        public int MaxAlive;
        public float StartDelaySeconds;
        public bool IsBossWave;

        public CrystalDefenseWaveDefinition Normalized()
        {
            var enemyCount = Mathf.Max(1, EnemyCount);
            return new CrystalDefenseWaveDefinition
            {
                EnemyPrefab = EnemyPrefab,
                EnemyCount = enemyCount,
                SpawnIntervalSeconds = Mathf.Max(MinSpawnInterval, SpawnIntervalSeconds),
                MaxAlive = Mathf.Clamp(MaxAlive <= 0 ? enemyCount : MaxAlive, 1, enemyCount),
                StartDelaySeconds = Mathf.Max(0f, StartDelaySeconds),
                IsBossWave = IsBossWave
            };
        }
    }
}

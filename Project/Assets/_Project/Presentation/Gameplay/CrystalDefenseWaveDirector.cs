using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// 크리스탈 디펜스 게임 루프 오케스트레이션.
    /// 웨이브 정의에 따라 적을 시간차 스폰하고, MaxAlive 한도를 강제하고,
    /// 웨이브 클리어 / 승리 / 패배 이벤트를 외부에 알린다.
    /// 적 추적은 GameObject null 검사 기반 (적의 Destroy() 호출 시 자동 정리).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrystalDefenseWaveDirector : MonoBehaviour
    {
        [SerializeField] CrystalCoreHealth _crystal;
        [SerializeField] Transform[] _spawnPoints;
        [SerializeField] CrystalDefenseWaveDefinition[] _waves;
        [SerializeField] bool _startOnEnable = true;

        readonly List<GameObject> _alive = new List<GameObject>();
        Coroutine _run;
        int _currentWaveIndex = -1;
        bool _lost;
        bool _won;

        public int CurrentWaveIndex => _currentWaveIndex;
        public int WaveCount => _waves != null ? _waves.Length : 0;
        public int AliveCount => _alive.Count;
        public bool IsRunning => _run != null;
        public bool IsLost => _lost;
        public bool IsWon => _won;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;
        public event Action Won;
        public event Action Lost;

        void OnEnable()
        {
            if (_crystal != null)
                _crystal.Destroyed += OnCrystalDestroyed;

            if (_startOnEnable)
                StartWaves();
        }

        void OnDisable()
        {
            if (_crystal != null)
                _crystal.Destroyed -= OnCrystalDestroyed;
            if (_run != null)
                StopCoroutine(_run);
            _run = null;
        }

        public void StartWaves()
        {
            if (_run != null)
                StopCoroutine(_run);

            _lost = false;
            _won = false;
            _currentWaveIndex = -1;
            _alive.Clear();
            _run = StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            if (_waves == null || _waves.Length == 0)
            {
                MarkWon();
                yield break;
            }

            for (var i = 0; i < _waves.Length; i++)
            {
                if (_lost)
                    yield break;

                _currentWaveIndex = i;
                var wave = _waves[i].Normalized();
                WaveStarted?.Invoke(i);

                if (wave.StartDelaySeconds > 0f)
                    yield return new WaitForSeconds(wave.StartDelaySeconds);

                var spawned = 0;
                while (spawned < wave.EnemyCount && !_lost)
                {
                    PruneAlive();
                    if (_alive.Count >= wave.MaxAlive)
                    {
                        yield return null;
                        continue;
                    }

                    SpawnEnemy(wave, spawned);
                    spawned++;
                    yield return new WaitForSeconds(wave.SpawnIntervalSeconds);
                }

                while (!_lost)
                {
                    PruneAlive();
                    if (IsWaveClear(spawned, wave.EnemyCount, _alive.Count))
                        break;
                    yield return null;
                }

                WaveCleared?.Invoke(i);
            }

            if (!_lost)
                MarkWon();
        }

        const float BossScaleMultiplier = 1.25f;

        void SpawnEnemy(CrystalDefenseWaveDefinition wave, int spawnIndex)
        {
            if (wave.EnemyPrefab == null || _spawnPoints == null || _spawnPoints.Length == 0)
                return;

            var spawn = _spawnPoints[spawnIndex % _spawnPoints.Length];
            if (spawn == null)
                return;

            var enemy = Instantiate(wave.EnemyPrefab, spawn.position, spawn.rotation);
            _alive.Add(enemy);

            var objective = enemy.GetComponent<CrystalDefenseEnemyObjective>();
            if (objective != null)
                objective.Crystal = _crystal;

            if (wave.IsBossWave)
                ApplyBossModifiers(enemy);
        }

        static void ApplyBossModifiers(GameObject enemy)
        {
            enemy.name = "Boss_" + enemy.name;
            enemy.transform.localScale *= BossScaleMultiplier;

            if (enemy.GetComponent<OsFpsInspiredDamageable>() == null)
                enemy.AddComponent<OsFpsInspiredDamageable>();
        }

        void PruneAlive()
        {
            for (var i = _alive.Count - 1; i >= 0; i--)
            {
                if (_alive[i] == null)
                    _alive.RemoveAt(i);
            }
        }

        void OnCrystalDestroyed(Vector3 _)
        {
            if (_lost)
                return;

            _lost = true;
            Lost?.Invoke();
        }

        void MarkWon()
        {
            if (_won)
                return;

            _won = true;
            _run = null;
            Won?.Invoke();
        }

        /// <summary>
        /// 순수 함수: 웨이브 클리어 조건 = 전부 스폰됐고 생존자 없음.
        /// edit-mode 테스트 가능하도록 분리.
        /// </summary>
        public static bool IsWaveClear(int spawned, int totalToSpawn, int aliveCount)
        {
            return spawned >= totalToSpawn && aliveCount <= 0;
        }
    }
}

using NUnit.Framework;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class CrystalDefenseWaveDirectorTests
    {
        [Test]
        public void WaveDefinition_Normalized_ClampsCountsAndTiming()
        {
            var wave = new CrystalDefenseWaveDefinition
            {
                EnemyCount = -5,
                SpawnIntervalSeconds = -1f,
                MaxAlive = 0,
                StartDelaySeconds = -3f
            };

            var normalized = wave.Normalized();

            Assert.AreEqual(1, normalized.EnemyCount);
            Assert.AreEqual(0.05f, normalized.SpawnIntervalSeconds, 0.001f);
            Assert.AreEqual(1, normalized.MaxAlive);
            Assert.AreEqual(0f, normalized.StartDelaySeconds, 0.001f);
        }

        [Test]
        public void WaveDefinition_Normalized_MaxAliveDoesNotExceedEnemyCount()
        {
            var wave = new CrystalDefenseWaveDefinition
            {
                EnemyCount = 3,
                SpawnIntervalSeconds = 0.25f,
                MaxAlive = 10,
                StartDelaySeconds = 1f
            };

            var normalized = wave.Normalized();

            Assert.AreEqual(3, normalized.EnemyCount);
            Assert.AreEqual(3, normalized.MaxAlive);
            Assert.AreEqual(0.25f, normalized.SpawnIntervalSeconds, 0.001f);
            Assert.AreEqual(1f, normalized.StartDelaySeconds, 0.001f);
        }

        [Test]
        public void WaveDefinition_Normalized_PreservesBossFlag()
        {
            var wave = new CrystalDefenseWaveDefinition
            {
                EnemyCount = 1,
                SpawnIntervalSeconds = 0.5f,
                MaxAlive = 1,
                StartDelaySeconds = 0f,
                IsBossWave = true
            };

            var normalized = wave.Normalized();

            Assert.IsTrue(normalized.IsBossWave);
        }

        [Test]
        public void IsWaveClear_WhenAllSpawnedAndNoAlive_ReturnsTrue()
        {
            Assert.IsFalse(CrystalDefenseWaveDirector.IsWaveClear(2, 3, 0));
            Assert.IsFalse(CrystalDefenseWaveDirector.IsWaveClear(3, 3, 1));
            Assert.IsTrue(CrystalDefenseWaveDirector.IsWaveClear(3, 3, 0));
        }
    }
}

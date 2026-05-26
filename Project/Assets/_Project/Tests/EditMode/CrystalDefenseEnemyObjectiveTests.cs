using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class CrystalDefenseEnemyObjectiveTests
    {
        [Test]
        public void ChooseTarget_PlayerWinsWhenThreateningAndVisible()
        {
            var result = CrystalDefenseEnemyObjective.ChooseTarget(
                hasPlayer: true,
                playerVisible: true,
                playerDistance: 8f,
                playerThreat01: 0.8f,
                hasCrystal: true,
                crystalDistance: 3f,
                crystalDestroyed: false);

            Assert.AreEqual(CrystalDefenseTargetKind.Player, result);
        }

        [Test]
        public void ChooseTarget_CrystalWinsWhenPlayerUnavailable()
        {
            var result = CrystalDefenseEnemyObjective.ChooseTarget(
                hasPlayer: false,
                playerVisible: false,
                playerDistance: 0f,
                playerThreat01: 0f,
                hasCrystal: true,
                crystalDistance: 4f,
                crystalDestroyed: false);

            Assert.AreEqual(CrystalDefenseTargetKind.Crystal, result);
        }

        [Test]
        public void ChooseTarget_NoneWhenNoValidTarget()
        {
            var result = CrystalDefenseEnemyObjective.ChooseTarget(
                hasPlayer: false,
                playerVisible: false,
                playerDistance: 0f,
                playerThreat01: 0f,
                hasCrystal: true,
                crystalDistance: 4f,
                crystalDestroyed: true);

            Assert.AreEqual(CrystalDefenseTargetKind.None, result);
        }

        [Test]
        public void ChooseTarget_CrystalWhenPlayerOutOfPriorityRangeAndLowThreat()
        {
            var result = CrystalDefenseEnemyObjective.ChooseTarget(
                hasPlayer: true,
                playerVisible: true,
                playerDistance: 30f,
                playerThreat01: 0.1f,
                hasCrystal: true,
                crystalDistance: 5f,
                crystalDestroyed: false);

            Assert.AreEqual(CrystalDefenseTargetKind.Crystal, result);
        }

        [Test]
        public void ChooseTarget_PlayerFallbackWhenCrystalDestroyed()
        {
            var result = CrystalDefenseEnemyObjective.ChooseTarget(
                hasPlayer: true,
                playerVisible: false,
                playerDistance: 30f,
                playerThreat01: 0.1f,
                hasCrystal: true,
                crystalDistance: 5f,
                crystalDestroyed: true);

            Assert.AreEqual(CrystalDefenseTargetKind.Player, result);
        }
    }
}

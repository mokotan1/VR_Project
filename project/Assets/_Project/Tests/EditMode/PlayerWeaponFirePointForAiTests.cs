using NUnit.Framework;
using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode
{
    public sealed class PlayerWeaponFirePointForAiTests
    {
        [Test]
        public void Publish_SetsActiveMuzzle()
        {
            var go = new GameObject("Owner");
            var muzzleGo = new GameObject("Muzzle");
            muzzleGo.transform.SetParent(go.transform, false);

            PlayerWeaponFirePointForAi.Publish(go, muzzleGo.transform);

            Assert.AreSame(muzzleGo.transform, PlayerWeaponFirePointForAi.ActiveMuzzle);
            PlayerWeaponFirePointForAi.ClearIfOwner(go);
            Assert.IsNull(PlayerWeaponFirePointForAi.ActiveMuzzle);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ClearIfOwner_RemovesOnlyMatchingOwner()
        {
            var ownerA = new GameObject("A");
            var muzzleA = new GameObject("MuzzleA");
            muzzleA.transform.SetParent(ownerA.transform, false);
            PlayerWeaponFirePointForAi.Publish(ownerA, muzzleA.transform);

            var ownerB = new GameObject("B");
            PlayerWeaponFirePointForAi.ClearIfOwner(ownerB);
            Assert.AreSame(muzzleA.transform, PlayerWeaponFirePointForAi.ActiveMuzzle);

            PlayerWeaponFirePointForAi.ClearIfOwner(ownerA);
            Assert.IsNull(PlayerWeaponFirePointForAi.ActiveMuzzle);

            Object.DestroyImmediate(muzzleA);
            Object.DestroyImmediate(ownerA);
            Object.DestroyImmediate(ownerB);
        }
    }
}

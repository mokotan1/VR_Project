using NUnit.Framework;
using UnityEngine;
using VRProject.Application.Combat;
using VRProject.Presentation.Combat;
using VRProject.Presentation.Gameplay;

namespace VRProject.Tests.EditMode.Combat
{
    public sealed class DamageReceiverTests
    {
        [Test]
        public void TryReceiveHit_KillsSuperhotEnemy()
        {
            var enemyGo = new GameObject("Enemy");
            try
            {
                enemyGo.AddComponent<DamageReceiver>();
                var enemy = enemyGo.AddComponent<SuperhotEnemy>();

                var zoneGo = new GameObject("HeadZone");
                zoneGo.transform.SetParent(enemyGo.transform, false);
                var zone = zoneGo.AddComponent<HitZone>();

                var receiver = enemyGo.GetComponent<DamageReceiver>();
                var context = new WeaponHitContext(zone, Vector3.up, Vector3.forward, WeaponAttackKind.Slash, 0.8f, 1);

                Assert.IsTrue(receiver.TryReceiveHit(context));
                Assert.IsTrue(enemyGo == null);
            }
            finally
            {
                if (enemyGo != null)
                    Object.DestroyImmediate(enemyGo);
            }
        }
    }
}

using UnityEngine;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.Gameplay
{
    public static class UnityChanPrototypeEnemyDemolishSetup
    {
        public static bool Ensure(GameObject enemyRoot)
        {
            if (enemyRoot == null)
                return false;

            if (enemyRoot.GetComponent<OsFpsInspiredDamageable>() == null)
                enemyRoot.AddComponent<OsFpsInspiredDamageable>();

            if (enemyRoot.GetComponent<EnemyPoseDemolishOnDeath>() == null)
                enemyRoot.AddComponent<EnemyPoseDemolishOnDeath>();

            if (enemyRoot.GetComponent<EnemyHitColorTint>() == null)
                enemyRoot.AddComponent<EnemyHitColorTint>();

            return true;
        }
    }
}

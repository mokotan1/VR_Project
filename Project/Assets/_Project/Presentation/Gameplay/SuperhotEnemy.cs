using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SuperhotEnemy : MonoBehaviour
    {
        SuperhotCombatZone _zone;

        void Awake()
        {
            _zone = GetComponentInParent<SuperhotCombatZone>();
        }

        void OnDestroy()
        {
            _zone?.NotifyEnemyDestroyed(this);
        }

        public void Kill()
        {
            Destroy(gameObject);
        }
    }
}

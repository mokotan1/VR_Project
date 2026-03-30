using System.Collections.Generic;
using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Tracks enemies in this node; reveals the exit interactable when all are gone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotCombatZone : MonoBehaviour
    {
        [SerializeField] GameObject _exitInteractableRoot;

        readonly HashSet<SuperhotEnemy> _alive = new();

        void OnEnable()
        {
            RefreshEnemies();
            if (_exitInteractableRoot != null)
                _exitInteractableRoot.SetActive(false);
        }

        void Start()
        {
            RefreshEnemies();
        }

        public void RegisterEnemy(SuperhotEnemy enemy)
        {
            if (enemy == null)
                return;
            _alive.Add(enemy);
            UpdatePortalVisibility();
        }

        public void NotifyEnemyDestroyed(SuperhotEnemy enemy)
        {
            if (enemy != null)
                _alive.Remove(enemy);
            UpdatePortalVisibility();
        }

        void RefreshEnemies()
        {
            _alive.Clear();
            foreach (var e in GetComponentsInChildren<SuperhotEnemy>(true))
                _alive.Add(e);
            UpdatePortalVisibility();
        }

        void UpdatePortalVisibility()
        {
            if (_exitInteractableRoot == null)
                return;
            _exitInteractableRoot.SetActive(_alive.Count == 0);
        }
    }
}

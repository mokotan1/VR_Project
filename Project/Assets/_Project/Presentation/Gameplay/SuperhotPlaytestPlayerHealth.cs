using System;
using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Simple hit points for playtest scenes; projectiles call <see cref="ApplyHit"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotPlaytestPlayerHealth : MonoBehaviour
    {
        [SerializeField] int _startingHits = 3;

        int _hitsTaken;

        public int RemainingHits => Mathf.Max(0, _startingHits - _hitsTaken);

        public bool IsAlive => _hitsTaken < _startingHits;

        public event System.Action PlayerDefeated;

        public void ApplyHit()
        {
            if (!IsAlive)
                return;

            _hitsTaken++;
            if (!IsAlive)
                PlayerDefeated?.Invoke();
        }

        public void ResetHits()
        {
            _hitsTaken = 0;
        }
    }
}

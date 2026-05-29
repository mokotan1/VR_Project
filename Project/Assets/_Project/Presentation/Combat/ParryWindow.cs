using System;
using UnityEngine;

namespace VRProject.Presentation.Combat
{
    [DisallowMultipleComponent]
    public sealed class ParryWindow : MonoBehaviour
    {
        [SerializeField] ShieldBlocker _shieldBlocker;

        float _lastBlockTime = float.NegativeInfinity;

        public event Action Blocked;
        public event Action Parried;

        void Awake()
        {
            if (_shieldBlocker == null)
                _shieldBlocker = GetComponent<ShieldBlocker>();
        }

        public bool RegisterBlock(float timeSeconds)
        {
            _lastBlockTime = timeSeconds;
            Blocked?.Invoke();
            return true;
        }

        public bool TryConsumeParry(float timeSeconds, float parryWindowSeconds, float qualifyingScore, float minParryScore)
        {
            if (timeSeconds - _lastBlockTime > parryWindowSeconds)
                return false;

            if (qualifyingScore < minParryScore)
                return false;

            Parried?.Invoke();
            return true;
        }
    }
}

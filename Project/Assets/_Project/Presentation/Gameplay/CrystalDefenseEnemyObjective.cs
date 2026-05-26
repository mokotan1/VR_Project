using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    public enum CrystalDefenseTargetKind
    {
        None,
        Player,
        Crystal
    }

    /// <summary>
    /// 적의 표적 결정 로직. ChooseTarget()은 의존성 없는 정적 함수로 분리하여
    /// edit-mode 테스트 가능하도록 했다. MonoBehaviour 인스턴스는 인스펙터 참조를
    /// 통해 Crystal/Player를 보관하고 RefreshTarget(LOS) 결과를 캐싱한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrystalDefenseEnemyObjective : MonoBehaviour
    {
        const float DefaultPlayerPriorityDistance = 7f;
        const float ThreatThreshold = 0.65f;

        [SerializeField] CrystalCoreHealth _crystal;
        [SerializeField] Transform _player;
        [SerializeField] float _playerPriorityDistance = DefaultPlayerPriorityDistance;
        [SerializeField, Range(0f, 1f)] float _playerThreat01 = 0.5f;

        public CrystalCoreHealth Crystal
        {
            get => _crystal;
            set => _crystal = value;
        }

        public Transform Player
        {
            get => _player;
            set => _player = value;
        }

        public CrystalDefenseTargetKind CurrentTargetKind { get; private set; }

        public Transform CurrentTargetTransform
        {
            get
            {
                if (CurrentTargetKind == CrystalDefenseTargetKind.Player)
                    return _player;
                if (CurrentTargetKind == CrystalDefenseTargetKind.Crystal && _crystal != null)
                    return _crystal.transform;
                return null;
            }
        }

        public CrystalDefenseTargetKind RefreshTarget(bool playerVisible)
        {
            var hasPlayer = _player != null;
            var hasCrystal = _crystal != null;
            var playerDistance = hasPlayer ? Vector3.Distance(transform.position, _player.position) : 0f;
            var crystalDistance = hasCrystal ? Vector3.Distance(transform.position, _crystal.transform.position) : 0f;

            CurrentTargetKind = ChooseTarget(
                hasPlayer,
                playerVisible,
                playerDistance,
                _playerThreat01,
                hasCrystal,
                crystalDistance,
                hasCrystal && _crystal.IsDestroyed,
                _playerPriorityDistance);

            return CurrentTargetKind;
        }

        /// <summary>
        /// 외부 데이터로부터 표적 종류를 결정. 단위 테스트 가능하도록 의존성 없음.
        /// 우선순위:
        /// 1. 가시 + 가까이 있거나 큰 위협이면 → Player
        /// 2. 크리스탈이 살아있으면 → Crystal
        /// 3. Player만 있으면 → Player
        /// 4. 아무것도 없으면 → None
        /// </summary>
        public static CrystalDefenseTargetKind ChooseTarget(
            bool hasPlayer,
            bool playerVisible,
            float playerDistance,
            float playerThreat01,
            bool hasCrystal,
            float crystalDistance,
            bool crystalDestroyed,
            float playerPriorityDistance = DefaultPlayerPriorityDistance)
        {
            var priorityRange = Mathf.Max(0f, playerPriorityDistance);

            if (hasPlayer && playerVisible &&
                (playerDistance <= priorityRange || playerThreat01 >= ThreatThreshold))
                return CrystalDefenseTargetKind.Player;

            if (hasCrystal && !crystalDestroyed)
                return CrystalDefenseTargetKind.Crystal;

            if (hasPlayer)
                return CrystalDefenseTargetKind.Player;

            return CrystalDefenseTargetKind.None;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _playerPriorityDistance = Mathf.Max(0f, _playerPriorityDistance);
            _playerThreat01 = Mathf.Clamp01(_playerThreat01);
        }
#endif
    }
}

using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Activates combat nodes one at a time; advance after grabbing the exit object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotNodeFlow : MonoBehaviour
    {
        [SerializeField] SuperhotCombatZone[] _zonesInOrder;

        int _currentIndex;

        void Awake()
        {
            if (_zonesInOrder == null || _zonesInOrder.Length == 0)
                return;

            for (var i = 0; i < _zonesInOrder.Length; i++)
            {
                if (_zonesInOrder[i] != null)
                    _zonesInOrder[i].gameObject.SetActive(i == 0);
            }

            _currentIndex = 0;
        }

        public void AdvanceFromZone(SuperhotCombatZone finished)
        {
            if (_zonesInOrder == null || _zonesInOrder.Length == 0)
                return;

            var idx = System.Array.IndexOf(_zonesInOrder, finished);
            if (idx < 0 || idx != _currentIndex)
                return;

            if (_zonesInOrder[_currentIndex] != null)
                _zonesInOrder[_currentIndex].gameObject.SetActive(false);

            _currentIndex++;
            if (_currentIndex < _zonesInOrder.Length && _zonesInOrder[_currentIndex] != null)
                _zonesInOrder[_currentIndex].gameObject.SetActive(true);
        }
    }
}

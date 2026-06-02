using UnityEngine;

namespace VRProject.Presentation.Combat
{
    [DisallowMultipleComponent]
    public sealed class HitZone : MonoBehaviour
    {
        [SerializeField] HitZoneKind _kind = HitZoneKind.Torso;
        [SerializeField] float _feedbackMultiplier = 1f;

        public HitZoneKind Kind => _kind;
        public float FeedbackMultiplier => _feedbackMultiplier;
        public int ZoneId => gameObject.GetHashCode();

        public static HitZone Resolve(Collider collider)
        {
            if (collider == null)
                return null;
            return collider.GetComponent<HitZone>() ?? collider.GetComponentInParent<HitZone>();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _feedbackMultiplier = Mathf.Max(0.1f, _feedbackMultiplier);
        }
#endif
    }
}

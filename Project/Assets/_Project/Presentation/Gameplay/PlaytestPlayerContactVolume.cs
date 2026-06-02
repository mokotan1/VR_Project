using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Ensures UnityChan-sized locomotion capsule and trigger hurtbox on playtest player roots.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaytestPlayerContactVolume : MonoBehaviour
    {
        void Awake() => Ensure(gameObject);

        public static void Ensure(GameObject playerRoot)
        {
            if (playerRoot == null)
                return;

            ApplyCharacterController(playerRoot);
            EnsureKinematicRigidbody(playerRoot);
            EnsureHurtbox(playerRoot);
        }

        static void ApplyCharacterController(GameObject playerRoot)
        {
            var cc = playerRoot.GetComponent<CharacterController>();
            if (cc == null)
                cc = playerRoot.AddComponent<CharacterController>();

            cc.height = PlaytestPlayerBodyProfile.CharacterControllerHeight;
            cc.radius = PlaytestPlayerBodyProfile.CharacterControllerRadius;
            cc.center = PlaytestPlayerBodyProfile.CharacterControllerCenter;
        }

        static void EnsureKinematicRigidbody(GameObject playerRoot)
        {
            var rb = playerRoot.GetComponent<Rigidbody>();
            if (rb == null)
                rb = playerRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        static void EnsureHurtbox(GameObject playerRoot)
        {
            var hurtboxTransform = playerRoot.transform.Find(PlaytestPlayerBodyProfile.HurtboxChildName);
            GameObject hurtboxGo;
            if (hurtboxTransform == null)
            {
                hurtboxGo = new GameObject(PlaytestPlayerBodyProfile.HurtboxChildName);
                hurtboxGo.transform.SetParent(playerRoot.transform, false);
            }
            else
            {
                hurtboxGo = hurtboxTransform.gameObject;
            }

            var capsule = hurtboxGo.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = hurtboxGo.AddComponent<CapsuleCollider>();

            capsule.isTrigger = true;
            capsule.height = PlaytestPlayerBodyProfile.HurtboxHeight;
            capsule.radius = PlaytestPlayerBodyProfile.HurtboxRadius;
            capsule.center = PlaytestPlayerBodyProfile.HurtboxCenter;
        }
    }
}

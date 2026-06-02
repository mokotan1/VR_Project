using UnityEngine;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// Body capsule dimensions shared by UnityChan_Player and XR playtest rigs.
    /// </summary>
    public static class PlaytestPlayerBodyProfile
    {
        public const string HurtboxChildName = "PlayerHurtbox";

        public const float CharacterControllerHeight = 1.35f;
        public const float CharacterControllerRadius = 0.22f;
        public static readonly Vector3 CharacterControllerCenter = new Vector3(0f, 0.68f, 0f);

        public const float HurtboxHeight = 1.5f;
        public const float HurtboxRadius = 0.5f;
        public static readonly Vector3 HurtboxCenter = new Vector3(0f, 0.75f, 0f);
    }
}

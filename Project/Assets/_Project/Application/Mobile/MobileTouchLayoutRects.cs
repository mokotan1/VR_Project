namespace VRProject.Application.Mobile
{
    /// <summary>
    /// Normalized screen rects (0–1, origin bottom-left) for landscape tablet controls.
    /// </summary>
    public readonly struct MobileTouchLayoutRects
    {
        public MobileTouchLayoutRects(
            float moveJoystickMinX,
            float moveJoystickMinY,
            float moveJoystickMaxX,
            float moveJoystickMaxY,
            float lookMinX,
            float lookMinY,
            float lookMaxX,
            float lookMaxY,
            float meleeMinX,
            float meleeMinY,
            float meleeMaxX,
            float meleeMaxY,
            float fireMinX,
            float fireMinY,
            float fireMaxX,
            float fireMaxY,
            float reloadMinX,
            float reloadMinY,
            float reloadMaxX,
            float reloadMaxY,
            float throwMinX,
            float throwMinY,
            float throwMaxX,
            float throwMaxY,
            float pauseMinX,
            float pauseMinY,
            float pauseMaxX,
            float pauseMaxY)
        {
            MoveJoystickMinX = moveJoystickMinX;
            MoveJoystickMinY = moveJoystickMinY;
            MoveJoystickMaxX = moveJoystickMaxX;
            MoveJoystickMaxY = moveJoystickMaxY;
            LookMinX = lookMinX;
            LookMinY = lookMinY;
            LookMaxX = lookMaxX;
            LookMaxY = lookMaxY;
            MeleeMinX = meleeMinX;
            MeleeMinY = meleeMinY;
            MeleeMaxX = meleeMaxX;
            MeleeMaxY = meleeMaxY;
            FireMinX = fireMinX;
            FireMinY = fireMinY;
            FireMaxX = fireMaxX;
            FireMaxY = fireMaxY;
            ReloadMinX = reloadMinX;
            ReloadMinY = reloadMinY;
            ReloadMaxX = reloadMaxX;
            ReloadMaxY = reloadMaxY;
            ThrowMinX = throwMinX;
            ThrowMinY = throwMinY;
            ThrowMaxX = throwMaxX;
            ThrowMaxY = throwMaxY;
            PauseMinX = pauseMinX;
            PauseMinY = pauseMinY;
            PauseMaxX = pauseMaxX;
            PauseMaxY = pauseMaxY;
        }

        public float MoveJoystickMinX { get; }
        public float MoveJoystickMinY { get; }
        public float MoveJoystickMaxX { get; }
        public float MoveJoystickMaxY { get; }
        public float LookMinX { get; }
        public float LookMinY { get; }
        public float LookMaxX { get; }
        public float LookMaxY { get; }
        public float MeleeMinX { get; }
        public float MeleeMinY { get; }
        public float MeleeMaxX { get; }
        public float MeleeMaxY { get; }
        public float FireMinX { get; }
        public float FireMinY { get; }
        public float FireMaxX { get; }
        public float FireMaxY { get; }
        public float ReloadMinX { get; }
        public float ReloadMinY { get; }
        public float ReloadMaxX { get; }
        public float ReloadMaxY { get; }
        public float ThrowMinX { get; }
        public float ThrowMinY { get; }
        public float ThrowMaxX { get; }
        public float ThrowMaxY { get; }
        public float PauseMinX { get; }
        public float PauseMinY { get; }
        public float PauseMaxX { get; }
        public float PauseMaxY { get; }

        public static MobileTouchLayoutRects LandscapeTabletDefault => new MobileTouchLayoutRects(
            moveJoystickMinX: 0.02f,
            moveJoystickMinY: 0.06f,
            moveJoystickMaxX: 0.28f,
            moveJoystickMaxY: 0.42f,
            lookMinX: 0.38f,
            lookMinY: 0.22f,
            lookMaxX: 0.98f,
            lookMaxY: 0.92f,
            meleeMinX: 0.38f,
            meleeMinY: 0.42f,
            meleeMaxX: 0.78f,
            meleeMaxY: 0.72f,
            fireMinX: 0.78f,
            fireMinY: 0.06f,
            fireMaxX: 0.98f,
            fireMaxY: 0.22f,
            reloadMinX: 0.58f,
            reloadMinY: 0.06f,
            reloadMaxX: 0.76f,
            reloadMaxY: 0.22f,
            throwMinX: 0.38f,
            throwMinY: 0.06f,
            throwMaxX: 0.56f,
            throwMaxY: 0.22f,
            pauseMinX: 0.02f,
            pauseMinY: 0.88f,
            pauseMaxX: 0.12f,
            pauseMaxY: 0.98f);
    }
}

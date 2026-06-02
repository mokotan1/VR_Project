namespace VRProject.Presentation.Combat
{
    public readonly struct VrTriggerPressDetector
    {
        readonly bool _wasPressed;

        public VrTriggerPressDetector(bool wasPressed)
        {
            _wasPressed = wasPressed;
        }

        public bool Tick(bool isPressed, out VrTriggerPressDetector next)
        {
            next = new VrTriggerPressDetector(isPressed);
            return isPressed && !_wasPressed;
        }
    }
}

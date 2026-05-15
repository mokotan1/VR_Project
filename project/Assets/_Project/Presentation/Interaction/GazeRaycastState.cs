namespace VRProject.Presentation.Interaction
{
    public readonly struct GazeRaycastTransition
    {
        public GazeRaycastTransition(GazeInteractable exit, GazeInteractable enter)
        {
            Exit = exit;
            Enter = enter;
        }

        public GazeInteractable Exit { get; }
        public GazeInteractable Enter { get; }
    }

    public static class GazeRaycastState
    {
        public static GazeRaycastTransition EvaluateTransition(
            GazeInteractable current,
            GazeInteractable next)
        {
            if (current == next)
                return new GazeRaycastTransition(null, null);

            return new GazeRaycastTransition(current, next);
        }
    }
}

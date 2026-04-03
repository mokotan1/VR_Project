namespace VRProject.Presentation.PrototypeFps
{
    public enum MantleBand
    {
        None,
        StepOrBelow,
        JumpBand,
        MantleBand,
        TooHigh
    }

    public static class MantleHeightBands
    {
        public static MantleBand Classify(float deltaY, float stepMax, float jumpMax, float mantleMax)
        {
            if (deltaY <= 0f)
                return MantleBand.None;
            if (deltaY <= stepMax)
                return MantleBand.StepOrBelow;
            if (deltaY <= jumpMax)
                return MantleBand.JumpBand;
            if (deltaY <= mantleMax)
                return MantleBand.MantleBand;
            return MantleBand.TooHigh;
        }
    }
}

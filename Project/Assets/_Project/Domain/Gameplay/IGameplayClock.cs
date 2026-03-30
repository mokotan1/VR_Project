namespace VRProject.Domain.Gameplay
{
    /// <summary>
    /// Game-world simulation clock. Consumers use <see cref="SimulationDeltaTime"/> instead of <c>Time.deltaTime</c>.
    /// </summary>
    public interface IGameplayClock
    {
        float SimulationDeltaTime { get; }

        float LastTimeFactor { get; }

        void BeginFrame(float unscaledDeltaTime, float timeFactor);
    }
}

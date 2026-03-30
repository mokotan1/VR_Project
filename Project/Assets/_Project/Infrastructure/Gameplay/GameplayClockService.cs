using VRProject.Domain.Gameplay;

namespace VRProject.Infrastructure.Gameplay
{
    public sealed class GameplayClockService : IGameplayClock
    {
        public float SimulationDeltaTime { get; private set; }

        public float LastTimeFactor { get; private set; }

        public void BeginFrame(float unscaledDeltaTime, float timeFactor)
        {
            LastTimeFactor = timeFactor;
            SimulationDeltaTime = unscaledDeltaTime * timeFactor;
        }
    }
}

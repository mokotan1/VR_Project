using VRProject.Domain.Gameplay;

namespace VRProject.Infrastructure.Gameplay
{
    public sealed class GameplayClockService : IGameplayClock
    {
        public float SimulationDeltaTime { get; private set; }

        /// <summary>드라이버가 한 번도 <see cref="BeginFrame"/>을 호출하기 전에는 1로 두어, 시계만 등록된 씬에서 적이 멈추지 않게 합니다.</summary>
        public float LastTimeFactor { get; private set; } = 1f;

        public void BeginFrame(float unscaledDeltaTime, float timeFactor)
        {
            LastTimeFactor = timeFactor;
            SimulationDeltaTime = unscaledDeltaTime * timeFactor;
        }
    }
}

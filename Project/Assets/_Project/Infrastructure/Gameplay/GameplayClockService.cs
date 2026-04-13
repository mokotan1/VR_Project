using VRProject.Domain.Gameplay;

namespace VRProject.Infrastructure.Gameplay
{
    public sealed class GameplayClockService : IGameplayClock
    {
        /// <summary>
        /// 첫 <see cref="BeginFrame"/> 전에 다른 스크립트가 읽을 때 0이 되어 이동이 멈추는 것을 줄이기 위해
        /// 약 60fps 한 프레임 분량으로 둡니다.
        /// </summary>
        public float SimulationDeltaTime { get; private set; } = 1f / 60f;

        /// <summary>드라이버가 한 번도 <see cref="BeginFrame"/>을 호출하기 전에는 1로 두어, 시계만 등록된 씬에서 적이 멈추지 않게 합니다.</summary>
        public float LastTimeFactor { get; private set; } = 1f;

        public void BeginFrame(float unscaledDeltaTime, float timeFactor)
        {
            LastTimeFactor = timeFactor;
            SimulationDeltaTime = unscaledDeltaTime * timeFactor;
        }
    }
}

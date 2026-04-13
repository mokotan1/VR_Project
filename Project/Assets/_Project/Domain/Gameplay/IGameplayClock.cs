namespace VRProject.Domain.Gameplay
{
    /// <summary>
    /// Game-world simulation clock. Consumers use <see cref="SimulationDeltaTime"/> instead of <c>Time.deltaTime</c>.
    /// 일부 UI·무기 쿨다운 등은 의도적으로 <c>Time.unscaledTime</c>을 쓸 수 있습니다.
    /// </summary>
    public interface IGameplayClock
    {
        float SimulationDeltaTime { get; }

        float LastTimeFactor { get; }

        void BeginFrame(float unscaledDeltaTime, float timeFactor);
    }
}

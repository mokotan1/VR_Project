using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRProject.Domain.Gameplay;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Gameplay
{
    /// <summary>
    /// On-screen hints, HP, time factor, and flat defeat + R restart for playtest scenes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuperhotPlaytestHud : MonoBehaviour
    {
        [SerializeField] Text _hintsText;
        [SerializeField] Text _statusText;
        [SerializeField] Text _defeatText;

        [Tooltip("켜면 HP 옆에 simDt·Unity dt·timeScale을 붙여 시간 정합을 바로 확인합니다.")]
        [SerializeField] bool _showTimeSyncDiagnostics;

        SuperhotPlaytestPlayerHealth _health;
        bool _subscribed;
        bool _defeated;

        void Start()
        {
            if (_defeatText != null)
                _defeatText.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (_health == null || !_health.isActiveAndEnabled)
            {
                if (_subscribed && _health != null)
                    _health.PlayerDefeated -= OnPlayerDefeated;

                _health = FindFirstObjectByType<SuperhotPlaytestPlayerHealth>();
                _subscribed = false;

                if (_health != null)
                {
                    _health.PlayerDefeated += OnPlayerDefeated;
                    _subscribed = true;
                }
            }

            var clock = TryGetClock();
            if (_statusText != null)
            {
                var hp = _health != null ? $"HP {Mathf.Max(0, _health.RemainingHits)}" : "—";
                var tf = clock != null ? clock.LastTimeFactor : 1f;
                var line = $"{hp}   |   time x{tf:0.00}";
                if (_showTimeSyncDiagnostics && clock != null)
                {
                    line +=
                        $"   |   simDt {clock.SimulationDeltaTime:0.0000}" +
                        $"   Unity.dt {Time.deltaTime:0.0000}" +
                        $"   TS {Time.timeScale:0.00}";
                }

                _statusText.text = line;
            }

            if (_defeated && Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        static IGameplayClock TryGetClock()
        {
            var loc = ServiceLocator.Instance;
            return loc.IsRegistered<IGameplayClock>() ? loc.Resolve<IGameplayClock>() : null;
        }

        void OnPlayerDefeated()
        {
            _defeated = true;
            if (_defeatText != null)
            {
                _defeatText.gameObject.SetActive(true);
                _defeatText.text = "Defeated — press R to restart.";
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnDestroy()
        {
            if (_health != null)
                _health.PlayerDefeated -= OnPlayerDefeated;
        }
    }
}

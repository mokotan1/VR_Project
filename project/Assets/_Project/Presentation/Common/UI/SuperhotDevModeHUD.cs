using System;
using UnityEngine;
using VRProject.Presentation.Gameplay;

namespace VRProject.Presentation.Common.UI
{
    /// <summary>
    /// 시간 배율 시스템의 실시간 값을 좌상단에 표시하는 개발자 모드 HUD.
    /// F1로 토글. <see cref="SuperhotGameplayDriver"/>를 Inspector에서 연결하거나 씬에서 자동 탐색.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class SuperhotDevModeHUD : MonoBehaviour
    {
        [SerializeField] SuperhotGameplayDriver _driver;
        [SerializeField] KeyCode _toggleKey = KeyCode.F1;
        [SerializeField] bool _visibleOnStart = true;
        [Tooltip("적 AI 목록을 다시 스캔하는 간격(초).")]
        [SerializeField] float _enemyBrainRescanInterval = 0.35f;

        bool _visible;
        float _nextEnemyRescanUnscaled;
        SuperhotEnemyBrain[] _enemyBrains = Array.Empty<SuperhotEnemyBrain>();
        Vector2 _enemyScroll;

        const float BOX_X   = 10f;
        const float BOX_Y   = 10f;
        const float BOX_W   = 210f;
        const float ENEMY_PANEL_X = BOX_X + BOX_W + 12f;
        const float ENEMY_PANEL_W = 380f;
        const float ENEMY_PANEL_H = 360f;
        const float ROW_H   = 22f;
        const float LABEL_W = 80f;
        const float BAR_W   = 90f;
        const float BAR_H   = 13f;
        const float VAL_W   = 46f;
        const int   ROWS    = 11;

        void Awake()
        {
            if (_driver == null)
                _driver = FindFirstObjectByType<SuperhotGameplayDriver>();
        }

        void OnEnable()  => _visible = _visibleOnStart;
        void OnDisable() => _visible = false;

        void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
                _visible = !_visible;
        }

        void OnGUI()
        {
            if (!_visible)
                return;

            var boxH = ROW_H * ROWS + 24f;
            GUI.Box(new Rect(BOX_X, BOX_Y, BOX_W, boxH), GUIContent.none);

            GUILayout.BeginArea(new Rect(BOX_X + 6f, BOX_Y + 6f, BOX_W - 12f, boxH - 8f));

            GUI.color = Color.cyan;
            GUILayout.Label($"■ DEV MODE  ({_toggleKey} 토글)");
            GUI.color = Color.white;

            if (_driver == null)
            {
                GUI.color = Color.red;
                GUILayout.Label("SuperhotGameplayDriver 없음");
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Space(2f);

                // 실시간 시간 배율
                DrawSeparator("── 시간 배율 ──────────────");
                DrawBar("TimeScale", Time.timeScale,        Color.green);
                DrawBar("Smoothed ", _driver.DbgSmoothed,   Color.yellow);
                DrawBar("Target   ", _driver.DbgTarget,     Color.white);

                // 입력 강도
                DrawSeparator("── 입력 ────────────────────");
                DrawBar("Move01   ", _driver.DbgMove01,     Color.cyan);
                DrawBar("Look01   ", _driver.DbgLook01,     new Color(1f, 0.6f, 0.2f));

                // 가중치 설정값
                DrawSeparator("── 가중치 ──────────────────");
                GUILayout.Label($"  Move {_driver.DbgMoveWeight:F2}  Look {_driver.DbgLookWeight:F2}  MaxΔ/s {_driver.DbgMaxDeltaPerSecond:F1}");
                GUILayout.Label($"  Head {_driver.DbgHeadWeight:F2}  Hand {_driver.DbgHandWeight:F2}");
            }

            GUILayout.EndArea();

            DrawEnemyMovementPanel();
        }

        void DrawEnemyMovementPanel()
        {
            MaybeRescanEnemyBrains();

            GUI.Box(new Rect(ENEMY_PANEL_X, BOX_Y, ENEMY_PANEL_W, ENEMY_PANEL_H), GUIContent.none);
            GUILayout.BeginArea(new Rect(ENEMY_PANEL_X + 6f, BOX_Y + 6f, ENEMY_PANEL_W - 12f, ENEMY_PANEL_H - 10f));

            GUI.color = new Color(1f, 0.75f, 0.35f);
            GUILayout.Label($"■ 적 이동 / AI ({_toggleKey} 동일)");
            GUI.color = Color.white;

            GUILayout.Space(2f);
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUILayout.Label("── 횡이동(strafe) 기준 ──");
            GUI.color = Color.white;
            if (PlayerWeaponFirePointForAi.ActiveMuzzle != null)
                GUILayout.Label("  총구(FirePoint) → 적이 총 좌우축 기준으로 움직임");
            else
                GUILayout.Label("  플레이어 몸통 right (총 미장착·비활성 시)");

            GUILayout.Space(4f);
            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUILayout.Label("── SuperhotEnemyBrain 목록 ──");
            GUI.color = Color.white;

            if (_enemyBrains.Length == 0)
            {
                GUI.color = new Color(1f, 0.45f, 0.45f);
                GUILayout.Label("  (없음 — 씬에 SuperhotEnemyBrain 컴포넌트 없음)");
                GUI.color = Color.white;
                GUILayout.EndArea();
                return;
            }

            _enemyScroll = GUILayout.BeginScrollView(_enemyScroll, GUILayout.Height(ENEMY_PANEL_H - 118f));
            foreach (var brain in _enemyBrains)
            {
                if (brain == null)
                    continue;

                var off = !brain.isActiveAndEnabled;
                var v = brain.DebugDesiredVelocity;
                var d = brain.DebugNavDestination;
                GUILayout.Label(
                    $"[{brain.name}]{(off ? " (비활성)" : "")}  {brain.DebugStateName}\n" +
                    $"  path pend={brain.DebugPathPending} stop={brain.DebugAgentStopped} has={brain.DebugHasPath} rem={brain.DebugRemainingDistance:F2}\n" +
                    $"  desiredVel xz=({v.x:F2},{v.z:F2})  dest xz=({d.x:F1},{d.z:F1})");
                GUILayout.Space(4f);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void MaybeRescanEnemyBrains()
        {
            if (Time.unscaledTime < _nextEnemyRescanUnscaled)
                return;
            _nextEnemyRescanUnscaled = Time.unscaledTime + Mathf.Max(0.05f, _enemyBrainRescanInterval);
            _enemyBrains = FindObjectsByType<SuperhotEnemyBrain>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            Array.Sort(_enemyBrains, (a, b) => string.CompareOrdinal(a.name, b.name));
        }

        static void DrawSeparator(string text)
        {
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label(text);
            GUI.color = Color.white;
        }

        static void DrawBar(string label, float value01, Color barColor)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(ROW_H));
            GUILayout.Label(label, GUILayout.Width(LABEL_W));

            var r = GUILayoutUtility.GetRect(BAR_W, BAR_H, GUILayout.Width(BAR_W), GUILayout.Height(BAR_H));
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = barColor;
            var fill = new Rect(r.x, r.y, r.width * Mathf.Clamp01(value01), r.height);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.Label(value01.ToString("F3"), GUILayout.Width(VAL_W));
            GUILayout.EndHorizontal();
        }
    }
}

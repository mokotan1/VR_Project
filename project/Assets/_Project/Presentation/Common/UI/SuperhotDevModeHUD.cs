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

        bool _visible;

        const float BOX_X   = 10f;
        const float BOX_Y   = 10f;
        const float BOX_W   = 210f;
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
                GUILayout.EndArea();
                return;
            }

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

            GUILayout.EndArea();
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

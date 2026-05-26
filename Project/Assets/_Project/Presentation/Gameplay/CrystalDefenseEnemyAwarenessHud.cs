using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRProject.Presentation.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CrystalDefenseEnemyAwarenessHud : MonoBehaviour
    {
        const int MaxIndicators = 12;

        [SerializeField] Camera _camera;
        [SerializeField] RectTransform _indicatorRoot;
        [SerializeField] float _edgePadding = 64f;
        [SerializeField] float _nearThreatDistance = 3f;
        [SerializeField] float _farThreatDistance = 18f;
        [SerializeField] Color _farColor = new Color(1f, 0.85f, 0.15f, 0.85f);
        [SerializeField] Color _nearColor = new Color(1f, 0.12f, 0.08f, 1f);

        readonly List<Image> _indicators = new List<Image>(MaxIndicators);
        Sprite _arrowSprite;

        void Awake()
        {
            if (_camera == null)
                _camera = Camera.main;
            EnsureHud();
        }

        void LateUpdate()
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null || _indicatorRoot == null)
                return;

            var enemies = FindObjectsByType<CrystalDefenseEnemyObjective>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            var used = 0;
            var canvasSize = _indicatorRoot.rect.size;

            foreach (var enemy in enemies)
            {
                if (enemy == null || used >= MaxIndicators)
                    continue;

                var viewport = _camera.WorldToViewportPoint(enemy.transform.position + Vector3.up);
                if (CrystalDefenseAwarenessMath.IsVisibleInViewport(viewport))
                    continue;

                if (!CrystalDefenseAwarenessMath.TryGetOffscreenIndicator(
                        viewport,
                        canvasSize,
                        _edgePadding,
                        out var anchoredPosition,
                        out var angleDegrees))
                    continue;

                var image = GetIndicator(used++);
                var rt = (RectTransform)image.transform;
                rt.anchoredPosition = anchoredPosition;
                rt.localRotation = Quaternion.Euler(0f, 0f, angleDegrees);

                var distance = Vector3.Distance(_camera.transform.position, enemy.transform.position);
                var threat = CrystalDefenseAwarenessMath.Threat01(distance, _nearThreatDistance, _farThreatDistance);
                image.color = Color.Lerp(_farColor, _nearColor, threat);
                image.enabled = true;
            }

            for (var i = used; i < _indicators.Count; i++)
                _indicators[i].enabled = false;
        }

        void EnsureHud()
        {
            if (_indicatorRoot != null)
                return;

            var canvasGo = new GameObject("Enemy Awareness HUD", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasGo.AddComponent<GraphicRaycaster>();

            _indicatorRoot = canvasGo.GetComponent<RectTransform>();
            _indicatorRoot.anchorMin = Vector2.zero;
            _indicatorRoot.anchorMax = Vector2.one;
            _indicatorRoot.offsetMin = Vector2.zero;
            _indicatorRoot.offsetMax = Vector2.zero;
        }

        Image GetIndicator(int index)
        {
            while (_indicators.Count <= index)
                _indicators.Add(CreateIndicator());
            return _indicators[index];
        }

        Image CreateIndicator()
        {
            var go = new GameObject("EnemyOffscreenArrow", typeof(RectTransform));
            go.transform.SetParent(_indicatorRoot, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(34f, 42f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var image = go.AddComponent<Image>();
            image.sprite = GetArrowSprite();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        Sprite GetArrowSprite()
        {
            if (_arrowSprite != null)
                return _arrowSprite;

            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.name = "RuntimeEnemyArrow";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var clear = new Color(1f, 1f, 1f, 0f);
            var white = Color.white;
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var nx = Mathf.Abs((x + 0.5f) / texture.width - 0.5f) * 2f;
                    var ny = (y + 0.5f) / texture.height;
                    texture.SetPixel(x, y, nx <= ny ? white : clear);
                }
            }

            texture.Apply();
            _arrowSprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
            return _arrowSprite;
        }
    }
}

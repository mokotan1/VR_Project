using UnityEngine;
using UnityEngine.UI;
using VRProject.Presentation.OsFpsInspired;

namespace VRProject.Presentation.PrototypeFps
{
    /// <summary>
    /// PUBG-style hip-fire crosshair: center dot + four bars that spread with move, air, and recent shots; tightens when aiming.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleRoyaleStyleCrosshair : MonoBehaviour
    {
        const float MinLineOffset = 7f;
        const float SpreadLerpSpeed = 14f;

        [SerializeField] PrototypeThirdPersonPlayer _player;
        [SerializeField] OsFpsInspiredWeapon _weapon;
        [SerializeField] RectTransform _dot;
        [SerializeField] RectTransform _lineTop;
        [SerializeField] RectTransform _lineBottom;
        [SerializeField] RectTransform _lineLeft;
        [SerializeField] RectTransform _lineRight;
        [SerializeField] Image _dotImage;
        [SerializeField] Image _lineTopImage;
        [SerializeField] Image _lineBottomImage;
        [SerializeField] Image _lineLeftImage;
        [SerializeField] Image _lineRightImage;

        [SerializeField] float _lineThickness = 2f;
        [SerializeField] float _lineLength = 9f;
        [SerializeField] float _dotSize = 3.5f;
        [SerializeField] Color _color = new Color(1f, 1f, 1f, 0.92f);
        [SerializeField] Color _colorAim = new Color(1f, 1f, 1f, 0.78f);

        static Sprite _whiteSprite;
        float _smoothedSpread;

        void OnEnable()
        {
            EnsureWhiteSprite();
            ApplySpriteToImages();
            ApplyColors();
        }

        void Update()
        {
            if (_player == null)
                return;

            var moveAxes = _player.LocomotionAxes;
            var moveMag = new Vector2(moveAxes.x, moveAxes.y).magnitude;
            var sinceFire = _weapon != null ? Time.unscaledTime - _weapon.LastFireUnscaledTime : 999f;
            var rawSpread = BattleRoyaleCrosshairSpread.ComputeRawSpread(
                moveMag,
                _player.IsGrounded,
                _player.IsAiming,
                sinceFire);

            _smoothedSpread = Mathf.Lerp(_smoothedSpread, rawSpread, Time.deltaTime * SpreadLerpSpeed);

            var offset = MinLineOffset + _smoothedSpread;
            var col = _player.IsAiming ? _colorAim : _color;

            if (_dot != null)
                _dot.sizeDelta = new Vector2(_dotSize, _dotSize);
            if (_lineTop != null)
            {
                _lineTop.sizeDelta = new Vector2(_lineThickness, _lineLength);
                _lineTop.anchoredPosition = new Vector2(0f, offset);
            }

            if (_lineBottom != null)
            {
                _lineBottom.sizeDelta = new Vector2(_lineThickness, _lineLength);
                _lineBottom.anchoredPosition = new Vector2(0f, -offset);
            }

            if (_lineLeft != null)
            {
                _lineLeft.sizeDelta = new Vector2(_lineLength, _lineThickness);
                _lineLeft.anchoredPosition = new Vector2(-offset, 0f);
            }

            if (_lineRight != null)
            {
                _lineRight.sizeDelta = new Vector2(_lineLength, _lineThickness);
                _lineRight.anchoredPosition = new Vector2(offset, 0f);
            }

            if (_dotImage != null)
                _dotImage.color = col;
            SetLineColor(_lineTopImage, col);
            SetLineColor(_lineBottomImage, col);
            SetLineColor(_lineLeftImage, col);
            SetLineColor(_lineRightImage, col);
        }

        static void SetLineColor(Image img, Color c)
        {
            if (img != null)
                img.color = c;
        }

        void ApplyColors()
        {
            if (_dotImage != null)
                _dotImage.color = _color;
            SetLineColor(_lineTopImage, _color);
            SetLineColor(_lineBottomImage, _color);
            SetLineColor(_lineLeftImage, _color);
            SetLineColor(_lineRightImage, _color);
        }

        void ApplySpriteToImages()
        {
            if (_whiteSprite == null)
                return;
            if (_dotImage != null)
            {
                _dotImage.sprite = _whiteSprite;
                _dotImage.type = Image.Type.Simple;
                _dotImage.raycastTarget = false;
            }

            foreach (var img in new[] { _lineTopImage, _lineBottomImage, _lineLeftImage, _lineRightImage })
            {
                if (img == null)
                    continue;
                img.sprite = _whiteSprite;
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
            }
        }

        static void EnsureWhiteSprite()
        {
            if (_whiteSprite != null)
                return;
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
    }
}

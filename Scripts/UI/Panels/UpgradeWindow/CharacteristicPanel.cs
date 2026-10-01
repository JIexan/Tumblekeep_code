using Assets.SimpleLocalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.UpgradeWindow {
    public class CharacteristicPanel : MonoBehaviour {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text value;

        [Header("Preview")]
        [SerializeField] Color previewColor = new Color(0.4f, 0.9f, 0.55f);
        [SerializeField, Min(1f)] float previewScale = 1.06f;
        [SerializeField, Min(0f)] float previewDuration = 0.24f;

        private Characteristic characteristic;
        private Color normalColor;
        private Vector3 normalScale;
        private Vector2 normalValueSize;
        private Vector2 normalValuePosition;
        private Vector2 normalTitleSize;
        private Tween previewTween;
        private bool initialized;

        public CharacteristicType Type => characteristic.type;

        public void Init(Characteristic characteristic) {
            if (!initialized) {
                normalColor = value.color;
                normalScale = value.transform.localScale;
                normalValueSize = value.rectTransform.sizeDelta;
                normalValuePosition = value.rectTransform.anchoredPosition;
                normalTitleSize = title.rectTransform.sizeDelta;
                initialized = true;
            }
            this.characteristic = characteristic;
            title.text = LocalizationManager.Localize(characteristic.Settings.type.ToString() + "Name");
            ClearPreview();
		}

        public void ShowPreview(CharacteristicUpgradeData upgrade) {
            ClearPreview();
            float current = characteristic.Get();
            float next = characteristic.GetUpgradePreview(upgrade.additionalValue, upgrade.persentValue);
            float difference = next - current;
            value.text = $"{next:0.##} ({difference:+0.##;-0.##;0})";
            value.color = previewColor;

            // Give the extra delta room, keeping a gap before the localized stat name.
            float rowWidth = ((RectTransform)transform).rect.width;
            float maxWidth = Mathf.Max(normalValueSize.x, rowWidth * 0.55f);
            float width = Mathf.Clamp(value.GetPreferredValues(value.text).x + 12f, normalValueSize.x, maxWidth);
            float extraWidth = width - normalValueSize.x;
            value.rectTransform.sizeDelta = normalValueSize + Vector2.right * extraWidth;
            value.rectTransform.anchoredPosition = normalValuePosition - Vector2.right * extraWidth * (1f - value.rectTransform.pivot.x);
            title.rectTransform.sizeDelta = normalTitleSize - Vector2.right * (extraWidth + 8f);

            value.transform.localScale = normalScale * previewScale;
            previewTween = value.transform.DOScale(normalScale, previewDuration)
                .SetEase(Ease.OutCubic).SetUpdate(true);
        }

        public void ClearPreview() {
            previewTween?.Kill();
            previewTween = null;
            if (!initialized)
                return;

            value.text = characteristic.Get().ToString("0.##");
            value.color = normalColor;
            value.transform.localScale = normalScale;
            value.rectTransform.sizeDelta = normalValueSize;
            value.rectTransform.anchoredPosition = normalValuePosition;
            title.rectTransform.sizeDelta = normalTitleSize;
        }

        private void OnDisable() {
            ClearPreview();
        }
    }
}

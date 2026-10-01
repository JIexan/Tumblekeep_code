using System.Collections.Generic;
using UnityEngine;

namespace UI.UpgradeWindow {
    public class StatsPanel : MonoBehaviour {

        [SerializeField] CharacteristicPanel characteristicPanelPrefab;
        [SerializeField] Transform panelsRoot;

        private List<CharacteristicPanel> panels = new List<CharacteristicPanel>();
        private CharacteristicPanel previewPanel;

        public void Init(IUpgradable target) {
            ClearPreview();
            var characteristics = target.Characteristics;

            if(characteristics.Count == 0) {
                gameObject.SetActive(false);
                return;
            } else {
                gameObject.SetActive(true);
            }

            int i = 0;
            foreach (var characteristic in characteristics) {
                if(i < panels.Count) {
                    panels[i].Init(characteristic.Value);
                    panels[i].gameObject.SetActive(true);
                } else {
                    var newPanel = Instantiate(characteristicPanelPrefab, panelsRoot);
                    newPanel.Init(characteristic.Value);
                    panels.Add(newPanel);
                }
                i++;
            }

            while(i < panels.Count) {
                panels[i].gameObject.SetActive(false);
                i++;
            }
        }

        public void ShowPreview(CharacteristicUpgradeData upgrade) {
            ClearPreview();
            if (upgrade == null || !isActiveAndEnabled)
                return;

            foreach (var panel in panels) {
                if (panel.gameObject.activeSelf && panel.Type == upgrade.characteristicType) {
                    previewPanel = panel;
                    panel.ShowPreview(upgrade);
                    return;
                }
            }
        }

        public void ClearPreview() {
            if (previewPanel != null)
                previewPanel.ClearPreview();
            previewPanel = null;
        }

        private void OnDisable() {
            ClearPreview();
        }
    }
}

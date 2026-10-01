using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Панель отсчёта активных спеллов с длительностью. Держит по строке (SpellTimerEntry) на каждый
/// играющий эффект — Money и SpeedUp могут идти одновременно
/// </summary>
public class SpellTimerPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [Tooltip("Шаблон строки (неактивный child); строки инстансятся рядом с ним")]
    [SerializeField] private SpellTimerEntry entryTemplate;

    private readonly List<SpellTimerEntry> entries = new List<SpellTimerEntry>();

    public void Show(ItemType spellType, TimedSpellEffect effect) {
        if (effect == null || entryTemplate == null)
            return;

        // Повторный каст того же спелла — переиспользуем строку
        var entry = entries.Find(e => e.SpellType == spellType);
        if (entry == null) {
            entry = Instantiate(entryTemplate, entryTemplate.transform.parent);
            entry.gameObject.SetActive(true);
            entries.Add(entry);
        }
        entry.Init(spellType, effect);
        panel.SetActive(true);
    }

    public void Hide() {
        foreach (var entry in entries)
            Destroy(entry.gameObject);
        entries.Clear();
        panel.SetActive(false);
    }

    private void Update() {
        for (int i = entries.Count - 1; i >= 0; i--) {
            var entry = entries[i];
            if (entry.Effect == null || !entry.Effect.IsPlaying) {
                Destroy(entry.gameObject);
                entries.RemoveAt(i);
                continue;
            }
            entry.Refresh();
        }

        if (entries.Count == 0 && panel.activeSelf)
            panel.SetActive(false);
    }
}

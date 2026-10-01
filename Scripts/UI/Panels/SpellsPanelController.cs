using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Динамический список свитков в HUD: по SpellPanel на каждый спелл, который есть у игрока,
/// сверху вниз в порядке получения (SaveDataProfile.spellOrder). Слотов с фиксированным типом нет
/// </summary>
public class SpellsPanelController : MonoBehaviour
{
	[Tooltip("Неактивный инстанс Spell.prefab внутри панели — шаблон для строк")]
	[SerializeField] private SpellPanel template;

	private readonly Dictionary<ItemType, SpellPanel> panels = new Dictionary<ItemType, SpellPanel>();

	private void Start() {
		if (template != null)
			template.gameObject.SetActive(false);
		Rebuild();
		InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
	}

	private void OnDestroy() {
		InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
	}

	private void Rebuild() {
		foreach (var type in SavesManager.Profile.GetOwnedSpellsInOrder())
			GetOrCreatePanel(type);
	}

	private void OnItemCountChanged(object data) {
		if (data == null)
			return;
		var type = (ItemType)data;
		if (!SaveDataProfile.IsSpell(type))
			return;

		int count = SavesManager.Profile.GetItem(type);
		if (count > 0) {
			GetOrCreatePanel(type);
		} else if (panels.TryGetValue(type, out var panel)) {
			// Свитки кончились — строка уходит, при новом получении встанет в конец
			panels.Remove(type);
			Destroy(panel.gameObject);
		}
	}

	/// <summary>
	/// Строка свитка в HUD; заводится, если свиток есть, а строки ещё нет. Публична для
	/// туториала (SpellObtainedCondition привязывает к строке реплику)
	/// </summary>
	public SpellPanel GetOrCreatePanel(ItemType type) {
		if (panels.TryGetValue(type, out var panel))
			return panel;
		if (template == null || SavesManager.Profile.GetItem(type) <= 0)
			return null;

		panel = Instantiate(template, template.transform.parent);
		panel.name = "Spell_" + type;
		panel.gameObject.SetActive(true);
		panel.transform.SetAsLastSibling();
		panel.Init(type);
		panels[type] = panel;
		return panel;
	}
}

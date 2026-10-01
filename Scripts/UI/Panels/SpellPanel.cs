using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private enum State {
        Hidden,
        Inactive,
        Active
	}

    // Тип задаёт SpellsPanelController через Init — строки создаются по имеющимся свиткам
    private ItemType type;
    [SerializeField] GameObject addButton;
    [SerializeField] GameObject draggableSpell;
    [SerializeField] Image[] activeImages;
    [SerializeField] TooltipTrigger tooltip;

    private ItemWithCount view;

    private GameObject dragItem = null;

    private State state = State.Hidden;

    public void Init(ItemType spellType) {
        type = spellType;
        view = GetComponent<ItemWithCount>();
        int count = SavesManager.Profile.GetItem(type);
        if (view != null)
            view.Init(type, count);
        UpdateState(count);
        if (tooltip != null)
            tooltip.SetContent<StatsTooltipContent>(ConfigureTooltip);
        InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
        InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
    }

    // Название, описание и параметры — те же, что на странице свитка в книге
    private void ConfigureTooltip(StatsTooltipContent content) {
        string id = type.ToString();
        content.Set(LocalizationManager.Localize("Book_" + id + "_Name"), LocalizationManager.Localize("Book_" + id + "_Desc"));
        if (!BookSpellPresenter.IsImplemented(type))
            return;
        var spellData = GameConfigs.Instance.ItemSettings.GetSpellData(type);
        foreach (var (labelKey, value) in BookSpellPresenter.GetRows(type, spellData))
            content.AddRow(LocalizationManager.Localize(labelKey), value);
    }

	private void OnDestroy() {
        InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
    }

    public void UpdateState(int itemCount) {
        state = itemCount == 0 ? State.Inactive : State.Active;
        foreach(var i in activeImages) {
            i.color = itemCount == 0 ? new Color(1f,1f,1f,0f) : Color.white;
		}
        addButton.SetActive(itemCount == 0);
	}

	private void OnItemCountChanged(object data) {
        if(data != null && (ItemType)data == type) {
            int count = SavesManager.Profile.GetItem(type);
            if (view != null)
                view.SetCount(count);
            UpdateState(count);
        }
	}

    public void OnClick() {
        if(state == State.Inactive)
            SavesManager.Profile.AddItem(type, 1);
	}

	public void OnBeginDrag(PointerEventData eventData) {
        if (state != State.Active)
            return;

        // Курсор ещё над слотом, но подсказка закрывала бы поле — прячем до следующего наведения
        if (tooltip != null)
            tooltip.Hide();

        dragItem = Instantiate(draggableSpell, GameController.Instance.uiController.spellDragPanel);
        dragItem.GetComponentInChildren<Image>().sprite = ItemIconsManager.Instance.GetIcon(type);
        dragItem.transform.position = transform.position;
        GameController.Instance.uiController.SetDragPanelActive(true);
        SpellsManager.Instance.InitRadius(eventData.position, type);
        InGameEventManager.SendEvent(InGameEvents.OnSpellDragStarted, type);
    }

	public void OnDrag(PointerEventData eventData) {
        if (state != State.Active)
            return;

        if (dragItem != null) {

            SpellsManager.Instance.UpdateRadius(eventData.position);
            dragItem.transform.position = eventData.position;
        }
	}

	public void OnEndDrag(PointerEventData eventData) {
        if (state != State.Active)
            return;

        if (dragItem != null) {
            Destroy(dragItem);
            dragItem = null;
            SpellsManager.Instance.ApplySpell(eventData.position, type);
            SpellsManager.Instance.HideRadius();
            // «Жадный гримуар»: с шансом спелл не списывается (заклинание уже применено)
            if (Gameplay.Relics.RelicsManager.RollSpellSave())
                UpdateState(SavesManager.Profile.GetItem(type));
            else if (SavesManager.Profile.SpendItem(type, 1))
                UpdateState(SavesManager.Profile.GetItem(type));
        }
        GameController.Instance.uiController.SetDragPanelActive(false);
    }
}

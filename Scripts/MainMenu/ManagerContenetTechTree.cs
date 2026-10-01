using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static SaveDataTechnologyTree;

public class ManagerContenetTechTree : Singleton<ManagerContenetTechTree>
{
    [SerializeField] private GameObject BranchArcher, BranchMortar, BranchElectric, BranchMagic, BranchCastle; // Ветки прокачки
    [SerializeField] private TechnologyTree SettingsTechTree;
    [SerializeField] private Transform LeftPanel;
    [SerializeField] private GameObject PrefabTechTree;
    [SerializeField] private Text Value;
    [SerializeField] private Text Description;
    [SerializeField] private Text PriceBuy;
    [SerializeField] private Text PriceNotBuy;
    [SerializeField] private Text YouHaveDiamond;
    [SerializeField] private GameObject ButtonUpgrade, ButtonNOTUpgrade;    
    [SerializeField] private Image ImagePrivew;
        
    private int temporary = 20;
    private bool flag;

    public ItemTreeTech SavePressedButton;
    
    void Start()
    {
        // Собираем ветку лучников
        СreateBranch(SettingsTechTree.itemsTechnologyTree.Where(x => x.type == typeItemTech.archer).OrderBy(x => x.id).ToList(), BranchArcher);
        // Собираем ветку мортиры
        СreateBranch(SettingsTechTree.itemsTechnologyTree.Where(x => x.type == typeItemTech.mortar).OrderBy(x => x.id).ToList(), BranchMortar);
        // Собираем ветку електрической башни
        СreateBranch(SettingsTechTree.itemsTechnologyTree.Where(x => x.type == typeItemTech.electric).OrderBy(x => x.id).ToList(), BranchElectric);
        // Собираем ветку магов
        СreateBranch(SettingsTechTree.itemsTechnologyTree.Where(x => x.type == typeItemTech.magic).OrderBy(x => x.id).ToList(), BranchMagic);
        // Собираем втеку замка
        СreateBranch(SettingsTechTree.itemsTechnologyTree.Where(x => x.type == typeItemTech.castle).OrderBy(x => x.id).ToList(), BranchCastle);
    }

    // Метод сбора ветки умений
    private void СreateBranch(List<ItemTechnologyTree> listSettings, GameObject branch)
    {
        flag = false;

        var listObject = branch.GetComponentsInChildren<ItemTreeTech>(); // Объекты в ветке
      
        for (int i = listSettings.Count()-1; i >= 0; i--)
        {
            listObject[i].Img.sprite = listSettings[i].imageTechTree;

            if (listSettings[i].hierarchy == hierarchyTechTree.item)
            {
                listObject[i].Description = listSettings[i].description;
                listObject[i].cost = listSettings[i].cost;
                listObject[i].Id = listSettings[i].id;
                listObject[i].hierarchy = listSettings[i].hierarchy;
                listObject[i].type = listSettings[i].type;
                listObject[i].openItem = listSettings[i].openItem;
                listObject[i].ValueItem = listSettings[i].value.ToString();

                SetStatusItem(listObject[i]);
            } 
        }
    }

    // Устанавливаем внешний вид ячейки
    private void SetStatusItem(ItemTreeTech item)
    {
        if (CheckIsBuy(item.Id)) // Купленный элемент                            
            item.SetBuy();

        else if (item.cost < temporary && !flag)
        {
            item.AvailablePurchase(); // Доступен к покупке
            flag = true;
        }
        else if (!flag)
        {
            item.NotEnoughMoney(); // Не хватает алмазов
            flag = true;
        }
        else
        {
            item.Unavailable(); // Не доступен
            item.loced = true;
        }
    }

    // Выбрали элемент, обновили info панель
    public void UpdateInfoRightPanel()
    {
        if (SavePressedButton == null) return;

        // Какую кнопку показываем, купить можно\купить нельзя
        if (CheckIsBuy(SavePressedButton.Id) || SavePressedButton.loced)
        {
            ButtonUpgrade.SetActive(false);
            ButtonNOTUpgrade.SetActive(false);
        }
        else if (SavePressedButton.cost < temporary)
        {
            ButtonNOTUpgrade.SetActive(false);
            ButtonUpgrade.SetActive(true);
        }
        else
        {
            ButtonUpgrade.SetActive(false);
            ButtonNOTUpgrade.SetActive(true);
        }

        Description.gameObject.SetActive(true);
        Description.text = SavePressedButton.Description;
        PriceBuy.text = SavePressedButton.cost.ToString();
        PriceNotBuy.text = SavePressedButton.cost.ToString();
        Value.text = SavePressedButton.ValueItem;
        ImagePrivew.sprite = SavePressedButton.Img.sprite;
    }

    // Проверяем куплен ли элемент
    private bool CheckIsBuy(idTechTree id)
    {
        return SavesManager.Profile.technologyTree.technologyTreeValues.FirstOrDefault(x => x.Key == id).Value == 1;
    }

    // Сбросить настройки окна
    public void ResetWindow()
    {
        if (SavePressedButton != null)
            SavePressedButton.RestSelector();

        ButtonUpgrade.SetActive(false);
        ButtonNOTUpgrade.SetActive(false);
    }

    // Изучаем технологию
    public void UpgradeTech()
    {
        SavesManager.Profile.technologyTree.technologyTreeValues.FirstOrDefault(x => x.Key == SavePressedButton.Id).Value.Set(1);
        SavesManager.Instance.SaveProfile();

        SavePressedButton.SetBuy();
        ButtonUpgrade.SetActive(false);

        if (SavePressedButton.NextItem != null)
        {
            flag = false;
            SavePressedButton.NextItem.loced = false;
            SetStatusItem(SavePressedButton.NextItem);
        }
    }
}

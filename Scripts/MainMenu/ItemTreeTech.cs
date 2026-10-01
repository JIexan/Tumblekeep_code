using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static SaveDataTechnologyTree;

public class ItemTreeTech : MonoBehaviour
{
    [SerializeField] private GameObject Selector; // селектор
    [SerializeField] private GameObject Green, Yellow, Gray, Red; // элементы
    [SerializeField] private GameObject Price; // ценник
    [SerializeField] private GameObject Done; // зеленая галочка    
    [SerializeField] private Text Cost; // цена

    private int mCoast;
    public int cost
    {
        get { return mCoast; }
        set
        {            
            mCoast = value;
            Cost.text = mCoast.ToString();
        }
    }

    public ItemTreeTech NextItem; // Последующий элемент
    public Button Button;
    public Image Img;
    public Sprite DoneImg;
    public string Description;   
    public idTechTree Id;
    public hierarchyTechTree hierarchy;
    public typeItemTech type;
    public bool openItem;
    public bool loced = false;
    public string ValueItem;

    private ManagerContenetTechTree _managerContenetTechTree;


    void Start()
    {
        _managerContenetTechTree = ManagerContenetTechTree.Instance;
    }

    public void ClickButton()
    {
        if (_managerContenetTechTree.SavePressedButton != null)
            _managerContenetTechTree.SavePressedButton.RestSelector();

        Selector.SetActive(true);       

        _managerContenetTechTree.SavePressedButton = this;
        _managerContenetTechTree.UpdateInfoRightPanel();
    }

    // Купленный элемент
    public void SetBuy()
    {
        DisableAllColorPanel();
        Green.SetActive(true);
        Price.SetActive(false);
        Done.SetActive(true);
    }

    // Доуступен к покупке
    public void AvailablePurchase()
    {
        DisableAllColorPanel();
        Yellow.SetActive(true);
        Price.SetActive(true);
    }

    // Доступен но не хватает алмазов
    public void NotEnoughMoney()
    {
        DisableAllColorPanel();
        Price.SetActive(true);
        Red.SetActive(true);
    }

    // Не доступен
    public void Unavailable()
    {
        DisableAllColorPanel();
        Price.SetActive(true);
        Gray.SetActive(true);
    }

    // Скрыть все панели
    private void DisableAllColorPanel()
    {
        Green.SetActive(false);
        Yellow.SetActive(false);
        Gray.SetActive(false);
        Red.SetActive(false);
        Price.SetActive(false);
        Done.SetActive(false);
    }

    // Сбрасываем селектор
    public void RestSelector()
    {
        Selector.SetActive(false);
    }
}

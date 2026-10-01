using Assets.SimpleLocalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Префаб ежедневной награды
/// </summary>
public class ItemDeilyReward : MonoBehaviour
{
    public Image imageReward;
    public Image imageRewardComplete;
    public Text textDay;
    public Sprite imageComplete;
    public Button buttonGetReward;

    private int _day;
    public int Day
    {
        get { return _day; }
        set
        {
            _day = value;
            textDay.text = LocalizationManager.Localize("Menu_DailyReward_Day") + _day.ToString();
        }
    }

    // Получаем награду
    public void GetdailyReward()
    {
        DailyComplite();
        DailyRewardPanel.Instance.TakeReward();
    }

    // Полученый
    public void DailyComplite()
    {
        imageRewardComplete.gameObject.SetActive(true);
        imageRewardComplete.sprite = imageComplete;
        imageRewardComplete.GetComponent<Image>().SetNativeSize();
        buttonGetReward.gameObject.SetActive(false);
    }

    // Доступный
    public void DailyAvailable()
    {
        buttonGetReward.gameObject.SetActive(true);
    }
}

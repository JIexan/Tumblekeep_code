using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;

/// <summary>
/// Панель ежедневных наград
/// </summary>
public class DailyRewardPanel : Singleton<DailyRewardPanel>
{
    #region Property

    public Transform TransformContentLeft;
    public GameObject ItemDailyReward;
    public DailyReward Settings;
    public Text Timer;
    public GameObject Notice;

    private DateTime _nextReward;
    private List<ItemDeilyReward> _listDeilyReward = new List<ItemDeilyReward>();

    #endregion

    protected override void Awake()    
    {
        base.Awake();

        Settings.itemsDailyReward.ForEach(addItemUI);

        if (DifferenceDate() >= 1)
        {
            Notice.SetActive(true);
			Notice.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.5f).SetEase(Ease.InCirc).SetLoops(-1,LoopType.Yoyo);

			//LeanTween.scale(Notice, new Vector3(1.2f, 1.2f, 1.2f), 0.5f).setEaseInCirc().setLoopPingPong();
        }
    }

    // Заполняем элементами панели
    private void addItemUI(itemDailyReward reward)
    {
        GameObject instance;

        instance = Instantiate(ItemDailyReward, TransformContentLeft);        
        instance.GetComponent<ItemDeilyReward>().imageReward.sprite = reward.imageReward;
        instance.GetComponent<ItemDeilyReward>().Day = reward.day;        

        _listDeilyReward.Add(instance.GetComponent<ItemDeilyReward>());
    }

    // Показываем таймер сколько осталось до следующей награды
    public void OnTimerShow()
    {
        // FormattedText.text = LocalizationManager.Localize("Settings.PlayTime", TimeSpan.FromHours(10.5f).TotalHours);
        // LocalizationManager.LocalizationChanged += () => FormattedText.text = LocalizationManager.Localize("Settings.PlayTime", TimeSpan.FromHours(10.5f).TotalHours);

        DateTime dt;
        if (DateTime.TryParseExact(SavesManager.Profile.dailyReward.lastTimeGetReward, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            _nextReward = dt.AddDays(1);
            Timer.gameObject.SetActive(true);
        }
    }

    // Обновляем значения при открытии окна
    public void RefreshWindowStats()
    {
        int dayComplite = SavesManager.Profile.dailyReward.rewardDayComplite;
        int dayDifference = DifferenceDate();

        if (dayComplite >= _listDeilyReward.Max(x => x.Day))
        {
            if (dayDifference < 1)
                OnTimerShow();
            else
            {
                // Доступная награда
                int minDay = _listDeilyReward.Min(x => x.Day);
                _listDeilyReward.FirstOrDefault(x => x.Day == minDay).DailyAvailable();                
            }
        }
        else
        {
            // Галочка на всех полученых ранее
            _listDeilyReward.FindAll(x => x.Day <= dayComplite).ForEach(x => x.DailyComplite());

            if (dayDifference < 1)
                OnTimerShow();
            else
                // Доступная награда
                _listDeilyReward.FirstOrDefault(x => x.Day == dayComplite + 1).DailyAvailable();
        }
    }

    // Сколько прошло дней с последнего взятого реварда
    private int DifferenceDate()
    {
        DateTime dt;
        if (DateTime.TryParseExact(SavesManager.Profile.dailyReward.lastTimeGetReward, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            DateTime current = CurrentTimeAPI.Instance.CheckGlobalTime();
            DateTime lastReward = dt;

            TimeSpan span = current - lastReward;
            return span.Days;
        }

        ClearValue();
        return 1;
    }

    // Получение награды
    public void TakeReward()
    {
        if (SavesManager.Profile.dailyReward.rewardDayComplite >= _listDeilyReward.Max(x => x.Day))
            SavesManager.Profile.dailyReward.rewardDayComplite = _listDeilyReward.Min(x => x.Day);
        else
            SavesManager.Profile.dailyReward.rewardDayComplite++;

        SavesManager.Profile.dailyReward.lastTimeGetReward = CurrentTimeAPI.Instance.CheckGlobalTime().ToString();
        SavesManager.Instance.SaveProfile();

        OnTimerShow();
        Notice.SetActive(false);
    }

    void Update()
    {
        if (Timer.IsActive())
        {
            TimeSpan span = _nextReward - DateTime.Now;
            Timer.text = "Next Reward " + span.Hours.ToString() + ":" + span.Minutes.ToString() + ":" + span.Seconds.ToString();
        }
    }

    public void ClearValue()
    {
        SavesManager.Profile.dailyReward.rewardDayComplite = 0;
        SavesManager.Profile.dailyReward.lastTimeGetReward = CurrentTimeAPI.Instance.CheckGlobalTime().AddDays(-2).ToString();
        SavesManager.Instance.SaveProfile();
    }
}

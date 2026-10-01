[System.Serializable]
public class SaveDataDailyReward : SaveDataBase
{
    [MinValue(0.0f)] public ObscuredInt rewardDayComplite = 0; // За сколько дней награда получена.
    public string lastTimeGetReward = "";  // Дата последней полученной награды.
}

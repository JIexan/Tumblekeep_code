using System.Collections.Generic;

[System.Serializable]
public class SaveDataBookViewed : SaveDataBase
{
    public List<UnitSave> unitSave;
    public List<TowerSave> towerSave; // легаси, не используется — оставлено для совместимости сейва
    // Просмотренные записи не-юнитов: "{BookTab}:{id}" (башни/спеллы/реликвии видны всегда, нужен только бейдж «новое»)
    public List<string> viewedEntries;

    public override void CheckValues()
    {
        base.CheckValues();
        if (unitSave == null)
            unitSave = new List<UnitSave>();

        if (towerSave == null)
            towerSave = new List<TowerSave>();

        if (viewedEntries == null)
            viewedEntries = new List<string>();
    }
}


public class UnitSave
{
    public UnitType unitType; // Тип юнита.
    public BookStatus bookStatus;  // Статус просмотрен ли юнит.
}

public class TowerSave
{
    public string towerId; // id башни.
    public BookStatus bookStatus;  // Статус просмотрена ли башня.
}

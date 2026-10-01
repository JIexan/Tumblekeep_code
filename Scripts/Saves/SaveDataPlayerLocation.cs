using System;
using System.Collections.Generic;
using Gameplay.RestorationSystem;

// Граница сейва для регионов: снаружи код оперирует RegionId (enum), в JSON регион
// хранится строкой (имя значения enum) — конвертация только здесь, через ToSaveId().
[System.Serializable]
public class SaveDataPlayerLocation : SaveDataBase
{
    public List<RegionSave> regions;

    public override void CheckValues()
    {
        base.CheckValues();
        if (regions == null)
            regions = new();

        // Saves created before the step graph deserialize completedStepIds as null.
        foreach (var region in regions)
            region.completedStepIds ??= new();
    }

    public RegionSave GetRegionSave(RegionId regionId)
    {
        string saveId = regionId.ToSaveId();
        foreach (var region in regions)
        {
            if (region.id == saveId)
                return region;
        }
        return null;
	}

    public RegionSave GetOrCreateRegionSave(RegionId regionId)
    {
        var region = GetRegionSave(regionId);
        if (region == null)
        {
            region = new RegionSave() { id = regionId.ToSaveId() };
            regions.Add(region);
        }
        return region;
    }

    public void CompleteStep(RegionId regionId, string stepGuid)
    {
        var region = GetOrCreateRegionSave(regionId);
        region.completedStepIds ??= new();
        if (!string.IsNullOrEmpty(stepGuid) && !region.completedStepIds.Contains(stepGuid))
            region.completedStepIds.Add(stepGuid);
    }

    public bool IsStepCompleted(RegionId regionId, string stepGuid)
    {
        var region = GetRegionSave(regionId);
        return region != null && region.completedStepIds != null
            && region.completedStepIds.Contains(stepGuid);
    }

    public bool IsUnlocked(RegionId regionId)
    {
        var region = GetRegionSave(regionId);
        return region != null && region.unlocked;
    }

    public void SetUnlocked(RegionId regionId)
    {
        GetOrCreateRegionSave(regionId).unlocked = true;
    }

    public bool IsCompleted(RegionId regionId)
    {
        var region = GetRegionSave(regionId);
        return region != null && region.completed;
    }

    public void SetCompleted(RegionId regionId)
    {
        GetOrCreateRegionSave(regionId).completed = true;
    }

    // Общее число шагов региона известно только контенту хаба — он записывает его сюда
    // при инициализации, чтобы процент готовности читался из любой сцены.
    public void SetTotalSteps(RegionId regionId, int totalSteps)
    {
        GetOrCreateRegionSave(regionId).totalSteps = totalSteps;
    }
}

[System.Serializable]
public class RegionSave
{
    public string id;
    public int completedSteps; // legacy linear counter; kept only so old saves deserialize, no longer read
    public List<string> completedStepIds;
    public int totalSteps; // снимок общего числа шагов, пишется при инициализации региона в хабе
    public bool unlocked;
    public bool completed;
}

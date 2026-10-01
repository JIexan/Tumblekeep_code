using Gameplay.TasksSystem;
using Gameplay.TasksSystem.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Level settings")]
public class LevelSettings : ScriptableObject
{
    public List<Level> Levels;
	public Vector3 startPosition;
	// Надпись над закрытым островом на карте и её подъём над верхней точкой острова
	public LockedIslandLabel lockedLabelPrefab;
	public float lockedLabelOffset = 3f;
}

[Serializable]
public class Level
{
    public string Id;
    public Vector3 Position;
    public string LoadScene;
    public typeLevel TypeLevel;
    public GameObject previewPrefab;
    public string infoLvl;
    public LevelSelector lvlPrefab;
    public LevelDifficulty difficulty;

    private List<AbstractTask> levelTasks;

    public List<AbstractTask> GetLevelTasks(bool force = false)
    {
        if (levelTasks == null || force)
        {
            levelTasks = LevelTasksGenerator.Generate(this);
        }
        return levelTasks;
	}

    public void ClearTasks()
    {
        levelTasks = null;
	}
}

[Serializable]
public enum typeLevel
{
    summer,
    nite,
    winter,
    autumn,
}

public enum LevelDifficulty
{
    easy,
    medium,
    hard
}
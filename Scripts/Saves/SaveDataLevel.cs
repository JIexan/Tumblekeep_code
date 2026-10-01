using UnityEngine;

[System.Serializable]
public class SaveDataLevel : SaveDataBase
{
	public const int STATE_CLOSED = 0;
	public const int STATE_OPENNED = 1;
	public const int STATE_PLAYED = 2;
	public const int STATE_COMPLETED = 3;


    [Range(0,3)] public ObscuredInt state = 0;
	[Range(0,3)] public ObscuredInt starsAmount = 0;

    public override void CheckValues()
    {
        base.CheckValues();

		state = STATE_OPENNED; // Временная заглушка пока не сохраняем пройденый уровень
    }
}

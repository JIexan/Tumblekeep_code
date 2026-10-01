using UnityEngine;

[System.Serializable]
public class SaveSettings : SaveDataBase
{
    public string language = "";

#if UNITY_ANDROID || UNITY_IPHONE
	[Range(0.0f, 1.0f)] public float musicVolume = 1.0f;
	[Range(0.0f, 1.0f)] public float soundVolume = 1.0f;
#else
    [Range(0.0f, 1.0f)] public float musicVolume = 0.5f;
	[Range(0.0f, 1.0f)] public float soundVolume = 0.7f;
#endif

    public override void CheckValues()
    {
        base.CheckValues();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class DailyReward : ScriptableObject
{
    public List<itemDailyReward> itemsDailyReward;    
}

[Serializable]
public class itemDailyReward
{
    public typeRewadr _typeRewadr;
    public Sprite imageReward;
    public int count;
    public int day;
}

[Serializable]
public enum typeRewadr
{
    coins,
    abilities,   
}

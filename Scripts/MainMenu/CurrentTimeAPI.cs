using System;
using System.Threading;
using UnityEngine;

public class CurrentTimeAPI : Singleton<CurrentTimeAPI>
{
    private void Start()
    {
        //var dateTime = CheckGlobalTime();

        //Debug.Log("Global UTC time: " + dateTime);
    }

    public DateTime CheckGlobalTime()
    {
        var www = new WWW("https://google.com");
        while (!www.isDone && www.error == null)
            Thread.Sleep(1);

        var str = www.responseHeaders["Date"];
        DateTime dateTime;

        if (!DateTime.TryParse(str, out dateTime))
            return DateTime.MinValue;

        return dateTime.ToUniversalTime();
    }
}

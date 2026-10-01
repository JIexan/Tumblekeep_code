using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DynamicSettings
{
    [Tooltip("f: The natural frequency of the system, in Hz. If the system isn't dampened, this is the frequency " +
         "it will oscillate at.")]
    public float F;
    [Tooltip("ζ: The damping coefficient. Affects how quickly the system settles on the target value. 0 = 'undamped', " +
             "between 0 and 1, 'underdamped'. 1 = 'critical damping' (e.g.: Unity's SmoothDamp())")]
    public float Z;
    [Tooltip("r: The initial responsiveness of the system. With a r of 0, the system will take some time to move towards the target. " +
             "A value of 1 will cause it to react immediately. A value greater than 1 will cause overshoot, " +
             "while a negative value will cause an undershoot before moving towards the target. " +
             "The r of a typical mechanical system is 2.")]
    public float R;
}

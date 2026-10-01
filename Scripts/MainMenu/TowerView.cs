using UnityEngine;

public class TowerView : MonoBehaviour
{
    public Transform Tower;
    public bool stopRorate;

    void Update()
    {
        if (!stopRorate)
            transform.RotateAround(Tower.position, Vector3.up, 15f * Time.deltaTime); 
    }
}

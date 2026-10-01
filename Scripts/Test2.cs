using UnityEngine;

public class Test2 : MonoBehaviour
{
    public Transform island_1;
    public Transform island_2;    
    public GameObject box;

    float distanceFromFirstToSecond;
    private Vector3 pos1, pos2;

    void Start()
    {
        pos1 = island_1.localPosition;
        pos2 = island_2.localPosition;
        distanceFromFirstToSecond = 3;
        //direction = (pos2 - pos1).normalized* 0.2f + pos1;
        AddBox();
    }

    private void AddBox()
    {
        for (int i = 0; i < 20; i++)
        {
            var objectItem = Instantiate(box);
            objectItem.transform.SetParent(transform, false);

            objectItem.transform.localPosition = pos1 + distanceFromFirstToSecond / (pos1 - pos2).magnitude * (pos2 - pos1); //direction * (i+ 1f); 
            objectItem.transform.forward = pos2 - pos1;
            
            distanceFromFirstToSecond += 4;

            float dist = (pos2 - objectItem.transform.localPosition).sqrMagnitude;

            if (dist <=20) return;           
        }
    }

}
    



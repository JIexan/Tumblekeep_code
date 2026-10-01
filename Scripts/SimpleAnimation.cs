using UnityEngine;

public class SimpleAnimation : MonoBehaviour
{
    public bool rotate = false;
    public bool rotateAround = false;
    
    public float speed;
    public Vector3 vector;
    public GameObject target;
    
    void Update()
    {
        if (rotate)
            transform.Rotate(vector, speed);
        if (rotateAround)
            transform.RotateAround(target.transform.position, Vector3.up, 30 * Time.deltaTime);
    }
}

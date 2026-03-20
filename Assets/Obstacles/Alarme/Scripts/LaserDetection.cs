using UnityEngine;

public class LaserDetection : MonoBehaviour
{
    [SerializeField] Collider laser;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RaycastHit hit;
        Vector3 _up = transform.TransformDirection(Vector3.up);
        if (Physics.Raycast(transform.position, _up, 10))
        {
            print("There is something in front of the object!");

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

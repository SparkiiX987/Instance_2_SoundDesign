using UnityEngine;

public class LaserSetup : Interactible
{
    [SerializeField] BoxCollider laser;
    [SerializeField] LayerMask detectedLayer;
    void Start()
    {
        RaycastHit hit;
        Vector3 _up = transform.TransformDirection(Vector3.up);
        if (Physics.Raycast(transform.position, _up, out hit, 10,detectedLayer))
        {
            print("There is something in front of the object!");
            print(hit.distance);
            laser.center = new Vector3(laser.center.x, hit.distance*2, laser.center.z);
            laser.size = new Vector3 (laser.size.x,hit.distance*4,laser.size.z);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        EventBus.Publish(new AlarmeSetActive
        {
            playerPosition = other.transform.position,
        });
        laser.enabled = false;
    }
}

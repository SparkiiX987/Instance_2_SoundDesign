using DG.Tweening;
using UnityEngine;

public class LaserSetup : MonoBehaviour
{
    [SerializeField] Collider laser;
    void Start()
    {
        RaycastHit hit;
        Vector3 _up = transform.TransformDirection(Vector3.up);
        if (Physics.Raycast(transform.position, _up, out hit, 10))
        {
            print("There is something in front of the object!");
            print(hit.distance);
            laser.transform.DOScaleY(hit.distance * 4, 0);
            laser.transform.DOLocalMoveY(hit.distance * 2, 0);
        }
    }
}

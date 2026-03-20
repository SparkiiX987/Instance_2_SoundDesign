using UnityEngine;

public class ignoreParentChangements : MonoBehaviour
{
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;

    void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.localScale;
    }

    void LateUpdate()
    {
        Vector3 deltaPosition = transform.position - lastPosition;
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);
        Vector3 deltaScale = new Vector3(
            transform.localScale.x / lastScale.x,
            transform.localScale.y / lastScale.y,
            transform.localScale.z / lastScale.z
        );

        foreach (Transform child in transform)
        {
            child.position -= deltaPosition;

            child.rotation = Quaternion.Inverse(deltaRotation) * child.rotation;

            child.localScale = new Vector3(
                child.localScale.x / deltaScale.x,
                child.localScale.y / deltaScale.y,
                child.localScale.z / deltaScale.z
            );
        }

        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.localScale;
    }
}

using UnityEngine;

public class PulseController : MonoBehaviour
{
    public Material stifledMat;
    public float pulseDuration = 0.5f;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(Pulse());
        }
    }

    System.Collections.IEnumerator Pulse()
    {
        stifledMat.SetFloat("_Pulse", 1);
        yield return new WaitForSeconds(pulseDuration);
        stifledMat.SetFloat("_Pulse", 0);
    }
}
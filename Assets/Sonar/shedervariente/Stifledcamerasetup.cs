using UnityEngine;

/// <summary>
/// A mettre sur le meme GameObject que la Camera principale.
/// Pousse les matrices inverses correctes au shader chaque frame.
/// </summary>
[RequireComponent(typeof(Camera))]
public class StifledCameraSetup : MonoBehaviour
{
    private Camera _cam;

    // IDs des proprietes shader
    private static readonly int ID_InvVP        = Shader.PropertyToID("_StifledInvVP");
    private static readonly int ID_CamPos       = Shader.PropertyToID("_StifledCamPos");
    private static readonly int ID_CamNear      = Shader.PropertyToID("_StifledCamNear");
    private static readonly int ID_CamFar       = Shader.PropertyToID("_StifledCamFar");

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        // Matrice View * Projection inversee — permet de reconstruire
        // la position monde depuis l'UV et la depth
        Matrix4x4 vp    = _cam.projectionMatrix * _cam.worldToCameraMatrix;
        Matrix4x4 invVP = vp.inverse;

        Shader.SetGlobalMatrix(ID_InvVP,   invVP);
        Shader.SetGlobalVector(ID_CamPos,  _cam.transform.position);
        Shader.SetGlobalFloat(ID_CamNear,  _cam.nearClipPlane);
        Shader.SetGlobalFloat(ID_CamFar,   _cam.farClipPlane);
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Player.Scripts;

namespace UI.Scripts
{
    /// <summary>
    /// Toggle menu pour activer/désactiver le FullScreenPassRendererFeature
    /// </summary>
    public class S_RendererMenuPrefs : MonoBehaviour
    {
        [Header("Renderer Toggle")]
        [SerializeField] private Toggle rendererToggle;
        [SerializeField] private UniversalRendererData pcRendererData; // ton PC_Renderer
        [SerializeField] private string featureName = "FullScreenPass"; // nom exact du RendererFeature

        private void Start()
        {
            if (rendererToggle != null)
            {
                rendererToggle.isOn = S_RendererPrefs.IsFullScreenPassEnabled;
                rendererToggle.onValueChanged.AddListener(OnRendererToggleChanged);
            }

            ApplyRendererState(S_RendererPrefs.IsFullScreenPassEnabled);
        }

        private void OnDestroy()
        {
            if (rendererToggle != null)
                rendererToggle.onValueChanged.RemoveListener(OnRendererToggleChanged);
        }

        private void OnRendererToggleChanged(bool value)
        {
            S_RendererPrefs.IsFullScreenPassEnabled = value;
            ApplyRendererState(value);
        }

        private void ApplyRendererState(bool enabled)
        {
            S_RendererUtils.SetRendererFeatureEnabled(pcRendererData, featureName, enabled);
        }
    }
}
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Player.Scripts
{
    /// <summary>
    /// Utilitaires pour manipuler les Renderer Features URP
    /// </summary>
    public static class S_RendererUtils
    {
        /// <summary>
        /// Cherche un Renderer Feature par nom et applique un bool à sa propriété custom IsEnabled.
        /// Fonctionne si ton Renderer Feature a un bool exposé public IsEnabled.
        /// </summary>
        public static void SetRendererFeatureEnabled(UniversalRendererData rendererData, string featureName, bool enabled)
        {
            if (rendererData == null)
            {
                Debug.LogWarning("[S_RendererUtils] rendererData est null !");
                return;
            }

            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature.name == featureName)
                {
                    // Vérifie si le feature a une propriété publique "IsEnabled"
                    var field = feature.GetType().GetField("IsEnabled");
                    if (field != null)
                    {
                        field.SetValue(feature, enabled);
                    }
                    else
                    {
                        Debug.LogWarning($"[S_RendererUtils] Le feature '{featureName}' n'a pas de bool IsEnabled public !");
                    }

                    break;
                }
            }
        }
    }
}
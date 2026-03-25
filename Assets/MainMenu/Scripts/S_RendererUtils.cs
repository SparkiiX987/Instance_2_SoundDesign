using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Player.Scripts
{
   
    public static class S_RendererUtils
    {
       
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
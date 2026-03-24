using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StifledRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material edgeMaterial;
        public Color edgeColor = Color.white;
        [Range(0.5f, 5f)] public float edgeThickness = 1.5f;
        [Range(0f, 1f)] public float depthThreshold = 0.01f;
        [Range(0f, 1f)] public float normalThreshold = 0.4f;
    }

    public Settings settings = new Settings();
    private StifledEdgePass _edgePass;

    public override void Create()
    {
        _edgePass = new StifledEdgePass(settings)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.edgeMaterial == null) return;
        _edgePass.Setup(renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(_edgePass);
    }
}
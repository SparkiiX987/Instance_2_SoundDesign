using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StifledEdgePass : ScriptableRenderPass
{
    private readonly StifledRenderFeature.Settings _settings;
    private RTHandle _source;
    private RTHandle _tempRT;

    public StifledEdgePass(StifledRenderFeature.Settings settings)
    {
        _settings = settings;
        // Demande le rendu des normales
        ConfigureInput(ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth);
    }

    public void Setup(RTHandle source) => _source = source;

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, name: "_StifledTemp");
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cmd = CommandBufferPool.Get("Stifled Edge");

        _settings.edgeMaterial.SetColor("_EdgeColor", _settings.edgeColor);
        _settings.edgeMaterial.SetFloat("_EdgeThickness", _settings.edgeThickness);
        _settings.edgeMaterial.SetFloat("_DepthThreshold", _settings.depthThreshold);
        _settings.edgeMaterial.SetFloat("_NormalThreshold", _settings.normalThreshold);

        Blitter.BlitCameraTexture(cmd, _source, _tempRT, _settings.edgeMaterial, 0);
        Blitter.BlitCameraTexture(cmd, _tempRT, _source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        _tempRT?.Release();
    }
}
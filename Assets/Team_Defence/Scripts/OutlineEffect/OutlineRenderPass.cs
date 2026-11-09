using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlineRenderPass : ScriptableRenderPass
{
    private const string ProfilerTag = "Render Outline";

    private Material m_OutlineMaterial;
    private Color m_OutlineColor;
    private float m_OutlineWidth;
    private int m_DownSampleScale;
    private int m_BlurIterations;
    private float m_BlurSpread;

    public Renderer[] OutlineObjects;

    private class PassData
    {
        public Material outlineMaterial;
        public Color outlineColor;
        public float outlineWidth;
        public int blurIterations;
        public float blurSpread;
        public List<Renderer> outlineObjects;
        public TextureHandle outlineColorTex;
        public TextureHandle blurTex;
        public TextureHandle blurTempTex;
        public TextureHandle cameraColorTex;
        public TextureHandle finalTex;
    }

    public OutlineRenderPass(Material outlineMaterial)
    {
        m_OutlineMaterial = outlineMaterial;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void Setup(Color outlineColor, float outlineWidth, int downSampleScale, int blurIterations, float blurSpread)
    {
        m_OutlineColor = outlineColor;
        m_OutlineWidth = outlineWidth;
        m_DownSampleScale = downSampleScale;
        m_BlurIterations = blurIterations;
        m_BlurSpread = blurSpread;
    }

    // ---------------------------------------------------------------------
    // Compatibility Mode (non Render Graph) path
    // ---------------------------------------------------------------------
    private static readonly int OutlineColorRTId = Shader.PropertyToID("_OutlineColorRT_Compat");
    private static readonly int BlurRTId = Shader.PropertyToID("_BlurRT_Compat");
    private static readonly int BlurTempRTId = Shader.PropertyToID("_BlurTempRT_Compat");
    private static readonly int FinalRTId = Shader.PropertyToID("_OutlineFinalRT_Compat");

    private RenderTextureDescriptor m_CompatDescriptor;

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        m_CompatDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        m_CompatDescriptor.depthBufferBits = 0;
        m_CompatDescriptor.msaaSamples = 1;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (OutlineObjects == null || OutlineObjects.Length == 0 || m_OutlineMaterial == null)
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);

        m_OutlineMaterial.SetColor("_OutlineColor", m_OutlineColor);
        m_OutlineMaterial.SetFloat("_OutlineWidth", m_OutlineWidth);

        int width = m_CompatDescriptor.width;
        int height = m_CompatDescriptor.height;
        int blurWidth = Mathf.Max(1, width >> m_DownSampleScale);
        int blurHeight = Mathf.Max(1, height >> m_DownSampleScale);

        cmd.GetTemporaryRT(OutlineColorRTId, width, height, 0, FilterMode.Bilinear, m_CompatDescriptor.colorFormat);
        cmd.GetTemporaryRT(BlurRTId, blurWidth, blurHeight, 0, FilterMode.Bilinear, m_CompatDescriptor.colorFormat);
        cmd.GetTemporaryRT(BlurTempRTId, blurWidth, blurHeight, 0, FilterMode.Bilinear, m_CompatDescriptor.colorFormat);
        cmd.GetTemporaryRT(FinalRTId, width, height, 0, FilterMode.Bilinear, m_CompatDescriptor.colorFormat);

        cmd.SetRenderTarget(OutlineColorRTId);
        cmd.ClearRenderTarget(true, true, Color.clear);
        for (int i = 0; i < OutlineObjects.Length; ++i)
        {
            if (OutlineObjects[i] != null)
            {
                cmd.DrawRenderer(OutlineObjects[i], m_OutlineMaterial, 0, 0);
            }
        }

        cmd.Blit(OutlineColorRTId, BlurRTId);

        for (int i = 0; i < m_BlurIterations; ++i)
        {
            m_OutlineMaterial.SetFloat("_BlurSize", 1.0f + i * m_BlurSpread);
            cmd.Blit(BlurRTId, BlurTempRTId, m_OutlineMaterial, 1);
            cmd.Blit(BlurTempRTId, BlurRTId, m_OutlineMaterial, 2);
        }

        cmd.SetGlobalTexture("_OutlineColorTex", OutlineColorRTId);
        cmd.SetGlobalTexture("_BlurTex", BlurRTId);

        RenderTargetIdentifier cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        cmd.Blit(cameraColorTarget, FinalRTId, m_OutlineMaterial, 3);
        cmd.Blit(FinalRTId, cameraColorTarget);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        cmd.ReleaseTemporaryRT(OutlineColorRTId);
        cmd.ReleaseTemporaryRT(BlurRTId);
        cmd.ReleaseTemporaryRT(BlurTempRTId);
        cmd.ReleaseTemporaryRT(FinalRTId);

        CommandBufferPool.Release(cmd);
    }

    // ---------------------------------------------------------------------
    // Render Graph path
    // ---------------------------------------------------------------------
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (OutlineObjects == null || OutlineObjects.Length == 0 || m_OutlineMaterial == null)
        {
            return;
        }

        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        var desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;

        var outlineColorDesc = new TextureDesc(desc.width, desc.height)
        {
            name = "_OutlineColorRT",
            colorFormat = desc.graphicsFormat,
            clearBuffer = true,
            clearColor = Color.clear
        };
        TextureHandle outlineColorTex = renderGraph.CreateTexture(outlineColorDesc);

        int blurWidth = Mathf.Max(1, desc.width >> m_DownSampleScale);
        int blurHeight = Mathf.Max(1, desc.height >> m_DownSampleScale);

        var blurDesc = new TextureDesc(blurWidth, blurHeight)
        {
            name = "_BlurRT",
            colorFormat = desc.graphicsFormat,
            filterMode = FilterMode.Bilinear
        };
        TextureHandle blurTex = renderGraph.CreateTexture(blurDesc);

        blurDesc.name = "_BlurTempRT";
        TextureHandle blurTempTex = renderGraph.CreateTexture(blurDesc);

        var finalDesc = new TextureDesc(desc.width, desc.height)
        {
            name = "_OutlineFinalRT",
            colorFormat = desc.graphicsFormat
        };
        TextureHandle finalTex = renderGraph.CreateTexture(finalDesc);

        using (var builder = renderGraph.AddUnsafePass<PassData>(ProfilerTag, out var passData))
        {
            passData.outlineMaterial = m_OutlineMaterial;
            passData.outlineColor = m_OutlineColor;
            passData.outlineWidth = m_OutlineWidth;
            passData.blurIterations = m_BlurIterations;
            passData.blurSpread = m_BlurSpread;
            passData.outlineObjects = new List<Renderer>(OutlineObjects);
            passData.outlineColorTex = outlineColorTex;
            passData.blurTex = blurTex;
            passData.blurTempTex = blurTempTex;
            passData.cameraColorTex = resourceData.cameraColor;
            passData.finalTex = finalTex;

            builder.UseTexture(outlineColorTex, AccessFlags.Write);
            builder.UseTexture(blurTex, AccessFlags.ReadWrite);
            builder.UseTexture(blurTempTex, AccessFlags.ReadWrite);
            builder.UseTexture(finalTex, AccessFlags.Write);
            builder.UseTexture(resourceData.cameraColor, AccessFlags.ReadWrite);

            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                data.outlineMaterial.SetColor("_OutlineColor", data.outlineColor);
                data.outlineMaterial.SetFloat("_OutlineWidth", data.outlineWidth);

                cmd.SetRenderTarget(data.outlineColorTex);
                cmd.ClearRenderTarget(true, true, Color.clear);
                for (int i = 0; i < data.outlineObjects.Count; ++i)
                {
                    if (data.outlineObjects[i] != null)
                    {
                        cmd.DrawRenderer(data.outlineObjects[i], data.outlineMaterial, 0, 0);
                    }
                }

                cmd.Blit(data.outlineColorTex, data.blurTex);

                for (int i = 0; i < data.blurIterations; ++i)
                {
                    data.outlineMaterial.SetFloat("_BlurSize", 1.0f + i * data.blurSpread);
                    cmd.Blit(data.blurTex, data.blurTempTex, data.outlineMaterial, 1);
                    cmd.Blit(data.blurTempTex, data.blurTex, data.outlineMaterial, 2);
                }

                data.outlineMaterial.SetTexture("_OutlineColorTex", data.outlineColorTex);
                data.outlineMaterial.SetTexture("_BlurTex", data.blurTex);

                cmd.Blit(data.cameraColorTex, data.finalTex, data.outlineMaterial, 3);
                cmd.Blit(data.finalTex, data.cameraColorTex);
            });
        }
    }
}
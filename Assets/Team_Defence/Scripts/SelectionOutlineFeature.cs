using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SelectionOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public LayerMask outlineLayerMask;
        public Color outlineColor = new Color(1f, 0.45f, 0f, 1f);

        [Range(1f, 15f)]
        public float thickness = 3f;

        public RenderPassEvent maskPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public RenderPassEvent outlinePassEvent = RenderPassEvent.AfterRenderingTransparents;

        public Shader maskShader;
        public Shader compositeShader;
    }

    public Settings settings = new Settings();

    private Material _maskMaterial;
    private Material _compositeMaterial;

    private MaskPass _maskPass;
    private CompositePass _compositePass;

    private RTHandle _maskTexture;

    public override void Create()
    {
        if (settings.maskShader == null)
        {
            settings.maskShader = Shader.Find("Hidden/Custom/OutlineMask");
        }

        if (settings.compositeShader == null)
        {
            settings.compositeShader = Shader.Find("Hidden/Custom/SelectionOutlineComposite");
        }

        if (settings.maskShader != null)
        {
            _maskMaterial = CoreUtils.CreateEngineMaterial(settings.maskShader);
        }

        if (settings.compositeShader != null)
        {
            _compositeMaterial = CoreUtils.CreateEngineMaterial(settings.compositeShader);
        }

        _maskPass = new MaskPass(settings, _maskMaterial)
        {
            renderPassEvent = settings.maskPassEvent
        };

        _compositePass = new CompositePass(settings, _compositeMaterial)
        {
            renderPassEvent = settings.outlinePassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_maskMaterial == null || _compositeMaterial == null)
        {
            return;
        }

        Camera camera = renderingData.cameraData.camera;

        if (camera.cameraType == CameraType.Preview)
        {
            return;
        }

        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        desc.colorFormat = RenderTextureFormat.R8;

        RenderingUtils.ReAllocateIfNeeded(
            ref _maskTexture,
            desc,
            FilterMode.Point,
            TextureWrapMode.Clamp,
            name: "_SelectionOutlineMask"
        );

        _maskPass.Setup(_maskTexture);
        _compositePass.Setup(_maskTexture);

        renderer.EnqueuePass(_maskPass);
        renderer.EnqueuePass(_compositePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_maskMaterial);
        CoreUtils.Destroy(_compositeMaterial);
        _maskTexture?.Release();
    }

    private class MaskPass : ScriptableRenderPass
    {
        private readonly Settings _settings;
        private readonly Material _maskMaterial;

        private RTHandle _maskTexture;

        private readonly ShaderTagId[] _shaderTags =
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };

        public MaskPass(Settings settings, Material maskMaterial)
        {
            _settings = settings;
            _maskMaterial = maskMaterial;
        }

        public void Setup(RTHandle maskTexture)
        {
            _maskTexture = maskTexture;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(_maskTexture);
            ConfigureClear(ClearFlag.Color, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Selection Outline Mask");

            using (new ProfilingScope(cmd, new ProfilingSampler("Selection Outline Mask")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

                DrawingSettings drawingSettings = new DrawingSettings(_shaderTags[0], new SortingSettings(renderingData.cameraData.camera)
                {
                    criteria = sortingCriteria
                });

                for (int i = 1; i < _shaderTags.Length; i++)
                {
                    drawingSettings.SetShaderPassName(i, _shaderTags[i]);
                }

                drawingSettings.overrideMaterial = _maskMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;

                FilteringSettings filteringSettings = new FilteringSettings(
                    RenderQueueRange.all,
                    _settings.outlineLayerMask
                );

                context.DrawRenderers(
                    renderingData.cullResults,
                    ref drawingSettings,
                    ref filteringSettings
                );
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private class CompositePass : ScriptableRenderPass
    {
        private readonly Settings _settings;
        private readonly Material _compositeMaterial;

        private RTHandle _maskTexture;
        private RTHandle _tempTexture;

        public CompositePass(Settings settings, Material compositeMaterial)
        {
            _settings = settings;
            _compositeMaterial = compositeMaterial;
        }

        public void Setup(RTHandle maskTexture)
        {
            _maskTexture = maskTexture;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(
                ref _tempTexture,
                desc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_SelectionOutlineTemp"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Selection Outline Composite");

            using (new ProfilingScope(cmd, new ProfilingSampler("Selection Outline Composite")))
            {
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                _compositeMaterial.SetTexture("_MaskTex", _maskTexture);
                _compositeMaterial.SetColor("_OutlineColor", _settings.outlineColor);
                _compositeMaterial.SetFloat("_Thickness", _settings.thickness);

                Vector4 texelSize = new Vector4(
                    1f / _maskTexture.rt.width,
                    1f / _maskTexture.rt.height,
                    _maskTexture.rt.width,
                    _maskTexture.rt.height
                );

                _compositeMaterial.SetVector("_MaskTex_TexelSize", texelSize);

                Blitter.BlitCameraTexture(cmd, source, _tempTexture, _compositeMaterial, 0);
                Blitter.BlitCameraTexture(cmd, _tempTexture, source);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
}
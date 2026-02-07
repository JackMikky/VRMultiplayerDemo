using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Material material;

        [Tooltip("是否在编辑器相机(Scene视图)中也渲染效果")]
        public bool renderInSceneView = false;
    }

    public Settings settings = new Settings();
    private SimplePass pass;

    public override void Create()
    {
        if (settings.material == null)
        {
            Debug.LogError("Material is null!");
            return;
        }

        pass = new SimplePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || settings.material == null) return;

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Cleanup();
    }

    private class SimplePass : ScriptableRenderPass
    {
        private Settings settings;
        private RTHandle tempTexture;
        private static readonly int tempTextureID = Shader.PropertyToID("_TempColorTexture");

        public SimplePass(Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor,
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempColorTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!settings.renderInSceneView && renderingData.cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            if (settings.material == null)
            {
                Debug.LogError("[CustomRendererFeature] Material is null in Execute!");
                return;
            }

            var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            if (cameraTarget == null || !cameraTarget.rt)
            {
                Debug.LogError("[CustomRendererFeature] Camera target is invalid!");
                return;
            }

            if (tempTexture == null || !tempTexture.rt)
            {
                Debug.LogError("[CustomRendererFeature] Temp texture is invalid!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("SimpleColorFilter");

            cmd.Blit(cameraTarget, tempTexture, settings.material, 0);
            cmd.Blit(tempTexture, cameraTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Cleanup()
        {
            tempTexture?.Release();
        }
    }
}
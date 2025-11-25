using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomFullScreenEffect : ScriptableRendererFeature
{
    public class BlitPass : ScriptableRenderPass
    {
        public Material blitMaterial = null;
        public FilterMode filterMode { get; set; }

        private BlitSettings settings;

        private RTHandle source;
        private RTHandle destination;

        RTHandle m_TemporaryColorTexture;
        RTHandle m_DestinationTexture;
        string m_ProfilerTag;

        private ScriptableRenderer renderer;

        public BlitPass(RenderPassEvent renderPassEvent, BlitSettings settings, string tag)
        {
            this.renderPassEvent = renderPassEvent;
            this.settings = settings;
            blitMaterial = settings.blitMaterial;
            m_ProfilerTag = tag;
        }

        public void Setup(ScriptableRenderer scriptableRenderer)
        {
#if UNITY_2020_2_OR_NEWER
            if (settings.requireDepthNormals)
                ConfigureInput(ScriptableRenderPassInput.Normal);
#else
            this.renderer = scriptableRenderer;
#endif
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

#if UNITY_2020_2_OR_NEWER
            var renderer = renderingData.cameraData.renderer;
#else
            var renderer = this.renderer;
#endif
            if (settings.srcType == Target.CameraColor)
            {
#if ENABLE_VR && UNITY_2020_2_OR_NEWER
                if (renderingData.cameraData.xr.enabled)
                {
                    source = RTHandles.Alloc(renderingData.cameraData.xr.renderTarget);
                }
                else
#endif
                {
                    source = renderer.cameraColorTargetHandle;
                }
            }
            else if (settings.srcType == Target.TextureID)
            {
                source = RTHandles.Alloc(settings.srcTextureId);
            }
            else if (settings.srcType == Target.RenderTextureObject)
            {
                source = RTHandles.Alloc(settings.srcTextureObject);
            }

            if (settings.dstType == Target.CameraColor)
            {
#if ENABLE_VR && UNITY_2020_2_OR_NEWER
                if (renderingData.cameraData.xr.enabled)
                {
                    destination = RTHandles.Alloc(renderingData.cameraData.xr.renderTarget);
                }
                else
#endif
                {
                    destination = renderer.cameraColorTargetHandle;
                }
            }
            else if (settings.dstType == Target.TextureID)
            {
                destination = RTHandles.Alloc(settings.dstTextureId);
            }
            else if (settings.dstType == Target.RenderTextureObject)
            {
                destination = RTHandles.Alloc(settings.dstTextureObject);
            }

            if (settings.setInverseViewMatrix)
            {
                Shader.SetGlobalMatrix("_InverseView", renderingData.cameraData.camera.cameraToWorldMatrix);
            }

            if (settings.dstType == Target.TextureID)
            {
                if (settings.overrideGraphicsFormat)
                {
                    opaqueDesc.graphicsFormat = settings.graphicsFormat;
                }

                m_DestinationTexture = RTHandles.Alloc(opaqueDesc.width, opaqueDesc.height, 1, DepthBits.None,
                    opaqueDesc.graphicsFormat, filterMode: filterMode, name: settings.dstTextureId);
            }

            if (source == destination ||
                (settings.srcType == settings.dstType && settings.srcType == Target.CameraColor))
            {
                m_TemporaryColorTexture = RTHandles.Alloc(opaqueDesc.width, opaqueDesc.height, 1, DepthBits.None,
                    opaqueDesc.graphicsFormat, filterMode: filterMode, name: "m_TemporaryColorTexture");
                Blit(cmd, source, m_TemporaryColorTexture, blitMaterial, settings.blitMaterialPassIndex);
                Blit(cmd, m_TemporaryColorTexture, destination);
            }
            else
            {
                Blit(cmd, source, destination, blitMaterial, settings.blitMaterialPassIndex);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (settings.dstType == Target.TextureID && m_DestinationTexture != null)
            {
                RTHandles.Release(m_DestinationTexture);
                m_DestinationTexture = null;
            }

            if ((source == destination ||
                 (settings.srcType == settings.dstType && settings.srcType == Target.CameraColor)) &&
                m_TemporaryColorTexture != null)
            {
                RTHandles.Release(m_TemporaryColorTexture);
                m_TemporaryColorTexture = null;
            }
        }
    }

    [System.Serializable]
    public class BlitSettings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

        public Material blitMaterial = null;
        public int blitMaterialPassIndex = 0;
        public bool setInverseViewMatrix = false;
        public bool requireDepthNormals = false;

        public Target srcType = Target.CameraColor;
        public string srcTextureId = "_CameraColorTexture";
        public RenderTexture srcTextureObject;

        public Target dstType = Target.CameraColor;
        public string dstTextureId = "_BlitPassTexture";
        public RenderTexture dstTextureObject;

        public bool overrideGraphicsFormat = false;
        public UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat;

        public bool canShowInSceneView = true;
    }

    public enum Target
    {
        CameraColor,
        TextureID,
        RenderTextureObject
    }


    public BlitSettings settings = new BlitSettings();
    public BlitPass blitPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isPreviewCamera) return;
        if (!settings.canShowInSceneView && renderingData.cameraData.isSceneViewCamera) return;

        if (settings.blitMaterial == null)
        {
            Debug.LogWarningFormat(
                "Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.",
                GetType().Name);
            return;
        }
#if !UNITY_2021_2_OR_NEWER
		// AfterRenderingPostProcessing event is fixed in 2021.2+ so this workaround is no longer required

		if (settings.Event == RenderPassEvent.AfterRenderingPostProcessing) {
		} else if (settings.Event == RenderPassEvent.AfterRendering && renderingData.postProcessingEnabled) {
			// If event is AfterRendering, and src/dst is using CameraColor, switch to _AfterPostProcessTexture instead.
			if (settings.srcType == Target.CameraColor) {
				settings.srcType = Target.TextureID;
				settings.srcTextureId = "_AfterPostProcessTexture";
			}
			if (settings.dstType == Target.CameraColor) {
				settings.dstType = Target.TextureID;
				settings.dstTextureId = "_AfterPostProcessTexture";
			}
		} else {
			// If src/dst is using _AfterPostProcessTexture, switch back to CameraColor
			if (settings.srcType == Target.TextureID && settings.srcTextureId == "_AfterPostProcessTexture") {
				settings.srcType = Target.CameraColor;
				settings.srcTextureId = "";
			}
			if (settings.dstType == Target.TextureID && settings.dstTextureId == "_AfterPostProcessing") {
				settings.dstType = Target.CameraColor;
				settings.dstTextureId = "";
			}
		}
#endif

        blitPass.Setup(renderer);
        renderer.EnqueuePass(blitPass);
    }

    public override void Create()
    {
        var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
        settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
        blitPass = new BlitPass(settings.Event, settings, name);

#if !UNITY_2021_2_OR_NEWER
		if (settings.Event == RenderPassEvent.AfterRenderingPostProcessing) {
			Debug.LogWarning("Note that the \"After Rendering Post Processing\"'s Color target doesn't seem to work? (or might work, but doesn't contain the post processing) :( -- Use \"After Rendering\" instead!");
		}
#endif

        if (settings.graphicsFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.None)
        {
            settings.graphicsFormat =
                SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
        }
    }
}
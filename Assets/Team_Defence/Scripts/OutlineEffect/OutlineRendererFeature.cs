using UnityEngine;
using UnityEngine.Rendering.Universal;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    public Material m_OutlineMaterial;

    [System.Serializable]
    public class Settings
    {
        public Color outlineColor = Color.red;

        [Range(0.1f, 10.0f)]
        public float outlineWidth = 2.0f;

        [Range(0, 8)]
        public int downSampleScale = 2;

        [Range(0, 4)]
        public int blurIterations = 1;

        [Range(0.2f, 3.0f)]
        public float blurSpread = 0.6f;
    }

    public Settings settings = new Settings();

    private OutlineRenderPass m_Pass;

    public static OutlineRendererFeature Instance { get; private set; }

    public override void Create()
    {
        Instance = this;
        m_Pass = new OutlineRenderPass(m_OutlineMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass.OutlineObjects == null || m_Pass.OutlineObjects.Length == 0)
        {
            return;
        }

        m_Pass.Setup(settings.outlineColor, settings.outlineWidth, settings.downSampleScale, settings.blurIterations, settings.blurSpread);
        renderer.EnqueuePass(m_Pass);
    }

    public void SetOutlineTargets(Renderer[] targets)
    {
        m_Pass.OutlineObjects = targets;
    }

    protected override void Dispose(bool disposing)
    {
    }
}
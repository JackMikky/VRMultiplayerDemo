using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Content.Interaction;

public class ScreenFilterController : MonoBehaviour
{
    [System.Serializable]
    public class ScreenFilterLever
    {
        public GameObject leverObject;
        public Material fullScreenFilterMaterial;
        public Material renderObjectsFilterMaterial;
    }

    [SerializeField] private List<ScreenFilterLever> screenFilterObjects = new List<ScreenFilterLever>();

    [SerializeField] private float autoDisableTime = 10f;
    [SerializeField] private string blendParameterName = "_Blend";

    private const string rendererFeatureName = "FullScreenFilter";
    private const string renderObjectsFeatureName = "FullScreenRenderObject";

    private int currentActiveIndex = -1;
    private Coroutine autoDisableCoroutine;
    private FullScreenPassRendererFeature cachedRendererFeature;
    private RenderObjects cachedRenderObjectsFeature;

    private void Awake()
    {
        for (int i = 0; i < screenFilterObjects.Count; i++)
        {
            var item = screenFilterObjects[i];
            int index = i;
            item.leverObject.GetComponent<XRLever>().onLeverActivate.AddListener(() =>
            {
                ToggleFilter(index);
            });
        }

        cachedRendererFeature = FindRendererFeature<FullScreenPassRendererFeature>(rendererFeatureName);
        cachedRenderObjectsFeature = FindRendererFeature<RenderObjects>(renderObjectsFeatureName);
    }

    private void Start()
    {
        DisableAllFeatures();
    }

    /// <summary>
    /// Get a ScriptableRendererFeature of the specified type and name from the active URP Renderer
    /// </summary>
    private T FindRendererFeature<T>(string featureName) where T : ScriptableRendererFeature
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null)
        {
            Debug.LogError("[ScreenFilterController] URP is not currently in use!");
            return null;
        }

        var scriptableRenderer = urpAsset.scriptableRenderer;
        if (scriptableRenderer == null)
        {
            Debug.LogError("[ScreenFilterController] Unable to get ScriptableRenderer!");
            return null;
        }

        var rendererFeaturesProperty = scriptableRenderer.GetType().GetProperty("rendererFeatures",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (rendererFeaturesProperty == null)
        {
            Debug.LogError("[ScreenFilterController] Unable to get rendererFeatures property!");
            return null;
        }

        var rendererFeatures = rendererFeaturesProperty.GetValue(scriptableRenderer) as List<ScriptableRendererFeature>;
        if (rendererFeatures == null)
        {
            Debug.LogError("[ScreenFilterController] RendererFeatures list is null!");
            return null;
        }

        foreach (var feature in rendererFeatures)
        {
            if (feature is T typedFeature && typedFeature.name.Equals(featureName))
            {
                Debug.Log($"[ScreenFilterController] Found {typeof(T).Name}: {feature.name}");
                return typedFeature;
            }
        }

        Debug.LogWarning($"[ScreenFilterController] {typeof(T).Name} '{featureName}' not found!");
        return null;
    }

    /// <summary>
    /// Disable all renderer features regardless of type
    /// </summary>
    private void DisableAllFeatures()
    {
        if (cachedRendererFeature != null && cachedRendererFeature.isActive)
        {
            cachedRendererFeature.SetActive(false);
        }
        if (cachedRenderObjectsFeature != null && cachedRenderObjectsFeature.isActive)
        {
            cachedRenderObjectsFeature.SetActive(false);
        }
    }

    /// <summary>
    /// Activate the filter at the specified index.
    /// Enables FullScreenPass if fullScreenFilterMaterial is set,
    /// and enables RenderObjects if renderObjectsFilterMaterial is set.
    /// </summary>
    public void ActivateFilter(int index)
    {
        if (index < 0 || index >= screenFilterObjects.Count)
        {
            Debug.LogWarning($"[ScreenFilterController] Index {index} is out of range!");
            return;
        }

        var screenFilter = screenFilterObjects[index];

        if (screenFilter.fullScreenFilterMaterial == null && screenFilter.renderObjectsFilterMaterial == null)
        {
            Debug.LogWarning($"[ScreenFilterController] No materials set at index {index}!");
            return;
        }

        DisableAllFeatures();

        // FullScreenPass
        if (screenFilter.fullScreenFilterMaterial != null)
        {
            if (cachedRendererFeature == null)
            {
                Debug.LogError("[ScreenFilterController] FullScreenPassRendererFeature not found!");
            }
            else
            {
                screenFilter.fullScreenFilterMaterial.SetFloat(blendParameterName, 1f);
                cachedRendererFeature.passMaterial = screenFilter.fullScreenFilterMaterial;
                cachedRendererFeature.SetActive(true);
            }
        }

        // RenderObjects
        if (screenFilter.renderObjectsFilterMaterial != null)
        {
            if (cachedRenderObjectsFeature == null)
            {
                Debug.LogError("[ScreenFilterController] RenderObjects Feature not found!");
            }
            else
            {
                cachedRenderObjectsFeature.SetActive(true);
            }
        }

        currentActiveIndex = index;

        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
        }

        autoDisableCoroutine = StartCoroutine(AutoDisableFilterAfterDelay());
    }

    /// <summary>
    /// Disable the filter effect
    /// </summary>
    public void DisableFilter()
    {
        DisableAllFeatures();

        currentActiveIndex = -1;

        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
            autoDisableCoroutine = null;
        }
    }

    /// <summary>
    /// Toggle filter on/off (for lever interaction)
    /// </summary>
    public void ToggleFilter(int index)
    {
        if (index < 0 || index >= screenFilterObjects.Count)
        {
            Debug.LogWarning($"[ScreenFilterController] Index {index} is out of range!");
            return;
        }

        if (currentActiveIndex == index)
        {
            DisableFilter();
        }
        else
        {
            ActivateFilter(index);
        }
    }

    /// <summary>
    /// Get the index of the currently active filter
    /// </summary>
    public int GetCurrentFilterIndex()
    {
        return currentActiveIndex;
    }

    /// <summary>
    /// Coroutine to automatically disable the filter after a delay
    /// </summary>
    private IEnumerator AutoDisableFilterAfterDelay()
    {
        float elapsedTime = 0f;

        var screenFilter = screenFilterObjects[currentActiveIndex];
        var fullScreenMat = screenFilter.fullScreenFilterMaterial;
        var hasBlendParameter = fullScreenMat != null && fullScreenMat.HasFloat(blendParameterName);

        while (elapsedTime < autoDisableTime)
        {
            elapsedTime += Time.deltaTime;

            if (hasBlendParameter)
            {
                float blendValue = Mathf.Clamp01(1f - (elapsedTime / autoDisableTime));
                fullScreenMat.SetFloat(blendParameterName, blendValue);
            }

            yield return null;
        }

        if (hasBlendParameter)
        {
            fullScreenMat.SetFloat(blendParameterName, 0f);
        }

        DisableFilter();
    }

    private void OnDestroy()
    {
        foreach (var item in screenFilterObjects)
        {
            if (item.leverObject != null)
            {
                var lever = item.leverObject.GetComponent<XRLever>();
                if (lever != null)
                {
                    lever.onLeverActivate.RemoveAllListeners();
                }
            }
        }

        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
        }

        DisableAllFeatures();
    }
}
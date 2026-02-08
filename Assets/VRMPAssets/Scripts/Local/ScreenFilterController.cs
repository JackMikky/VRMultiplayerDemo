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
        public Material filterMaterial;
    }

    [SerializeField] private List<ScreenFilterLever> screenFilterObjects = new List<ScreenFilterLever>();

    [SerializeField] private float autoDisableTime = 10f;

    private readonly string rendererFeatureName = "FullScreenFilter";

    private int currentActiveIndex = -1;
    private Coroutine autoDisableCoroutine;
    private FullScreenPassRendererFeature cachedRendererFeature;

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

        cachedRendererFeature = GetCustomRendererFeature();
    }

    private void Start()
    {
        if (cachedRendererFeature != null)
        {
            cachedRendererFeature.SetActive(false);
        }
    }

    /// <summary>
    /// Get the FullScreenPassRendererFeature from the currently active URP Renderer Data
    /// </summary>
    private FullScreenPassRendererFeature GetCustomRendererFeature()
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

        var rendererDataType = scriptableRenderer.GetType();
        var rendererFeaturesProperty = rendererDataType.GetProperty("rendererFeatures",
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
            if (feature is FullScreenPassRendererFeature fullScreenFeature && fullScreenFeature.name.Equals(rendererFeatureName))
            {
                Debug.Log($"[ScreenFilterController] Found FullScreenPassRendererFeature: {feature.name}");
                return fullScreenFeature;
            }
        }

        Debug.LogWarning("[ScreenFilterController] FullScreenPassRendererFeature not found!");
        return null;
    }

    /// <summary>
    /// Activate the filter material at the specified index
    /// </summary>
    public void ActivateFilter(int index)
    {
        if (cachedRendererFeature == null)
        {
            Debug.LogError("[ScreenFilterController] CustomRendererFeature not found!");
            return;
        }

        if (index < 0 || index >= screenFilterObjects.Count)
        {
            Debug.LogWarning($"[ScreenFilterController] Index {index} is out of range!");
            return;
        }

        var filterMaterial = screenFilterObjects[index].filterMaterial;
        if (filterMaterial == null)
        {
            Debug.LogWarning($"[ScreenFilterController] Material at index {index} is not set!");
            return;
        }

        cachedRendererFeature.passMaterial = filterMaterial;

        if (!cachedRendererFeature.isActive)
        {
            cachedRendererFeature.SetActive(true);
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
        if (cachedRendererFeature != null && cachedRendererFeature.isActive)
        {
            cachedRendererFeature.SetActive(false);
        }

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
        yield return new WaitForSeconds(autoDisableTime);
        DisableFilter();
    }

    private void OnDestroy()
    {
        // Clean up all listeners
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

        // Stop coroutine
        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
        }

        // Disable renderer feature
        if (cachedRendererFeature != null)
        {
            cachedRendererFeature.SetActive(false);
        }
    }
}
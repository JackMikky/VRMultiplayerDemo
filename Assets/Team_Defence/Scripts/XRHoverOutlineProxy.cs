using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class XRHoverOutlineProxy : MonoBehaviour
{
    [Header("Outline")]
    [SerializeField] private string outlineLayerName = "Outline";

    [SerializeField] private bool includeChildren = true;

    [Header("Stability")]
    [SerializeField] private float hideDelay = 0.05f;

    [SerializeField] private bool keepOutlineWhileSelected = true;

    private XRSimpleInteractable interactable;
    private int outlineLayer = -1;

    private int hoverCount;
    private int selectCount;
    private float hideTimer = -1f;

    private readonly List<Renderer> outlineRenderers = new List<Renderer>();

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        outlineLayer = LayerMask.NameToLayer(outlineLayerName);
        if (outlineLayer == -1)
        {
            Debug.LogError($"Layer '{outlineLayerName}' does not exist.", this);
            return;
        }

        CreateOutlineProxies();
        SetOutlineVisible(false);
    }

    private void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);

        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);

        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);

        hoverCount = 0;
        selectCount = 0;
        hideTimer = -1f;
        SetOutlineVisible(false);
    }

    private void Update()
    {
        if (hideTimer < 0f)
        {
            return;
        }

        hideTimer -= Time.deltaTime;

        if (hideTimer <= 0f)
        {
            hideTimer = -1f;

            if (hoverCount == 0 && (!keepOutlineWhileSelected || selectCount == 0))
            {
                SetOutlineVisible(false);
            }
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        hoverCount++;
        hideTimer = -1f;
        SetOutlineVisible(true);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        hoverCount = Mathf.Max(0, hoverCount - 1);

        if (hoverCount == 0 && (!keepOutlineWhileSelected || selectCount == 0))
        {
            hideTimer = hideDelay;
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        selectCount++;

        if (keepOutlineWhileSelected)
        {
            hideTimer = -1f;
            SetOutlineVisible(true);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        selectCount = Mathf.Max(0, selectCount - 1);

        if (hoverCount == 0 && selectCount == 0)
        {
            hideTimer = hideDelay;
        }
    }

    private void CreateOutlineProxies()
    {
        MeshRenderer[] meshRenderers = includeChildren
            ? GetComponentsInChildren<MeshRenderer>(true)
            : GetComponents<MeshRenderer>();

        foreach (MeshRenderer sourceRenderer in meshRenderers)
        {
            if (sourceRenderer.gameObject.name.Contains("OutlineProxy"))
            {
                continue;
            }

            MeshFilter sourceMeshFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
            {
                continue;
            }

            GameObject proxy = new GameObject(sourceRenderer.gameObject.name + "_OutlineProxy");
            proxy.layer = outlineLayer;

            proxy.transform.SetParent(sourceRenderer.transform, false);
            proxy.transform.localPosition = Vector3.zero;
            proxy.transform.localRotation = Quaternion.identity;
            proxy.transform.localScale = Vector3.one;

            MeshFilter proxyMeshFilter = proxy.AddComponent<MeshFilter>();
            proxyMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

            MeshRenderer proxyRenderer = proxy.AddComponent<MeshRenderer>();

            // Mask pass では overrideMaterial を使うので、見た目用 material は何でもよいです。
            // ただし Renderer が有効に描画対象になるため、元 material を入れておきます。
            proxyRenderer.sharedMaterials = sourceRenderer.sharedMaterials;

            // 影や通常描画には使わない
            proxyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            proxyRenderer.receiveShadows = false;

            outlineRenderers.Add(proxyRenderer);
        }
    }

    private void SetOutlineVisible(bool visible)
    {
        foreach (Renderer outlineRenderer in outlineRenderers)
        {
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = visible;
            }
        }
    }
}
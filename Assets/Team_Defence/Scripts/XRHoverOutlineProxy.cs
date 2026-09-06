using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class XRHoverOutlineProxy : MonoBehaviour
{
    [Header("Outline")]
    [SerializeField] private Renderer[] outlineRenderers;

    [SerializeField] private bool includeChildren = true;

    [Header("Stability")]
    [SerializeField] private float hideDelay = 0.05f;

    [SerializeField] private bool keepOutlineWhileSelected = true;

    private XRSimpleInteractable interactable;

    private int hoverCount;
    private int selectCount;
    private float hideTimer = -1f;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (outlineRenderers == null || outlineRenderers.Length == 0)
        {
            outlineRenderers = includeChildren
                ? GetComponentsInChildren<Renderer>(true)
                : GetComponents<Renderer>();
        }
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

    private void SetOutlineVisible(bool visible)
    {
        if (OutlineRendererFeature.Instance == null)
        {
            return;
        }

        OutlineRendererFeature.Instance.SetOutlineTargets(visible ? outlineRenderers : null);
    }
}
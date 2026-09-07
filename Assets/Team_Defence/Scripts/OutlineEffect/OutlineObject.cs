using UnityEngine;
using UnityEngine.EventSystems;

public class OutlineObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Renderer[] outlineObjects;

    private void Start()
    {
        if (outlineObjects == null || outlineObjects.Length == 0)
        {
            outlineObjects = GetComponentsInChildren<Renderer>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OutlineRendererFeature.Instance == null)
        {
            return;
        }
        OutlineRendererFeature.Instance.SetOutlineTargets(outlineObjects);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (OutlineRendererFeature.Instance == null)
        {
            return;
        }
        OutlineRendererFeature.Instance.SetOutlineTargets(null);
    }
}
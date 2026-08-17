using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRMPAssets.Scripts.UI
{
    public class VRInfiniteCarouselUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        public RectTransform contentRect;

        public HorizontalLayoutGroup layoutGroup;

        [Header("Scroll Settings")]
        public float scrollSpeed = 60f;

        private bool isHovering = false;
        private float movedDistance = 0f;
        private RectTransform viewportRect;

        private void OnEnable()
        {
            viewportRect = GetComponent<RectTransform>();
            AutoDuplicateItemsIfNeeded();
        }

        private void AutoDuplicateItemsIfNeeded()
        {
            if (contentRect == null || contentRect.childCount == 0 || viewportRect == null)
                return;

            Canvas.ForceUpdateCanvases();

            float totalContentWidth = contentRect.rect.width;
            float requiredWidth = viewportRect.rect.width * 2f;

            int originalChildCount = contentRect.childCount;

            while (totalContentWidth < requiredWidth && originalChildCount > 0)
            {
                for (int i = 0; i < originalChildCount; i++)
                {
                    Transform childToClone = contentRect.GetChild(i);
                    Instantiate(childToClone.gameObject, contentRect);
                }

                Canvas.ForceUpdateCanvases();
                totalContentWidth = contentRect.rect.width;
            }
        }

        private void Update()
        {
            if (isHovering || contentRect.childCount < 2)
                return;

            float step = scrollSpeed * Time.deltaTime;
            contentRect.anchoredPosition -= new Vector2(step, 0);
            movedDistance += step;

            RectTransform firstChild = contentRect.GetChild(0) as RectTransform;
            if (firstChild == null) return;

            float childWidth = firstChild.rect.width;
            float spacing = layoutGroup != null ? layoutGroup.spacing : 0;
            float itemSpan = childWidth + spacing;

            if (movedDistance >= itemSpan)
            {
                firstChild.SetAsLastSibling();
                contentRect.anchoredPosition += new Vector2(itemSpan, 0);
                movedDistance -= itemSpan;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => isHovering = true;

        public void OnPointerExit(PointerEventData eventData) => isHovering = false;
    }
}
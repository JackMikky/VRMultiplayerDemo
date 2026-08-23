using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRMPAssets.Scripts.UI
{
    public enum ScrollDirection
    {
        Left,
        Right
    }

    public class VRInfiniteCarouselUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        public RectTransform contentRect;

        public HorizontalLayoutGroup layoutGroup;

        [Header("Scroll Settings")]
        public float scrollSpeed = 60f;

        public ScrollDirection scrollDirection = ScrollDirection.Right;

        private const int MaxDuplicatePasses = 8;

        private bool isHovering = false;
        private bool isInitialized = false;
        private float movedDistance = 0f;
        private RectTransform viewportRect;

        private void Awake()
        {
            viewportRect = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            if (!isInitialized)
            {
                AutoDuplicateItemsIfNeeded();
                isInitialized = true;
            }
        }

        private void OnDisable()
        {
            isHovering = false;
        }

        private void AutoDuplicateItemsIfNeeded()
        {
            if (contentRect == null || contentRect.childCount == 0 || viewportRect == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            float requiredWidth = viewportRect.rect.width * 2f;

            int originalChildCount = contentRect.childCount;
            int pass = 0;

            while (contentRect.rect.width < requiredWidth && pass++ < MaxDuplicatePasses)
            {
                float previousWidth = contentRect.rect.width;

                for (int i = 0; i < originalChildCount; i++)
                {
                    Transform childToClone = contentRect.GetChild(i);
                    Instantiate(childToClone.gameObject, contentRect);
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

                if (contentRect.rect.width <= previousWidth)
                    break;
            }
        }

        private void Update()
        {
            if (isHovering || contentRect == null || contentRect.childCount < 2)
                return;

            float direction = scrollDirection == ScrollDirection.Right ? -1f : 1f;
            float step = scrollSpeed * Time.deltaTime;
            float spacing = layoutGroup != null ? layoutGroup.spacing : 0f;

            contentRect.anchoredPosition += new Vector2(step * direction, 0f);
            movedDistance += step;

            while (true)
            {
                int edgeIndex = scrollDirection == ScrollDirection.Right ? 0 : contentRect.childCount - 1;

                if (!(contentRect.GetChild(edgeIndex) is RectTransform edgeChild))
                    return;

                float itemSpan = edgeChild.rect.width + spacing;

                if (itemSpan <= 0f || movedDistance < itemSpan)
                    return;

                if (scrollDirection == ScrollDirection.Right)
                    edgeChild.SetAsLastSibling();
                else
                    edgeChild.SetAsFirstSibling();

                contentRect.anchoredPosition -= new Vector2(itemSpan * direction, 0f);
                movedDistance -= itemSpan;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => isHovering = true;

        public void OnPointerExit(PointerEventData eventData) => isHovering = false;
    }
}
using System;
using UnityEngine;

namespace PXR.Construction.Runtime
{
    public class ViewObjectController : MonoBehaviour
    {
        [SerializeField] private ViewObjectAssets assets;

        public Transform Parent;

        private GameObject viewObject;

        private void Awake()
        {
            if (viewObject == null) 
            {
                foreach (Transform child in transform) 
                {
                    viewObject = child.gameObject;
                    return;
                }
            }
        }

        public void ResetView(int idx)
        {
            viewObject = null;
            foreach(Transform child in this.transform)
            {
                DestroyImmediate(child.gameObject);
            }

            try
            {
                var asset = assets[idx];
                viewObject = Instantiate(asset.GameObject, this.transform);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Show()
        {
            viewObject?.SetActive(true);
        }

        public void Hide()
        {
            viewObject?.SetActive(false);
        }
    }
}
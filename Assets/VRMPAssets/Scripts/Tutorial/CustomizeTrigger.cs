using System;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// A simple class used for callbacks when OnTriggerEnter or OnTriggerExit is called.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CutomizeTrigger : MonoBehaviour
    {
        [SerializeField]private string targetTag = "";
        public Action<Collider, bool> OnTriggerAction;
        public Collider subTriggerCollider;

        private void Awake()
        {
            if (subTriggerCollider == null)
                TryGetComponent(out subTriggerCollider);
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log(other.tag);
            if (!String.IsNullOrEmpty(targetTag)&& other.CompareTag(targetTag))
            {
                OnTriggerAction?.Invoke(other, true);
                return;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!String.IsNullOrEmpty(targetTag) && other.CompareTag(targetTag))
            {
                OnTriggerAction?.Invoke(other, false);
                return;
            }
        }
    }
}
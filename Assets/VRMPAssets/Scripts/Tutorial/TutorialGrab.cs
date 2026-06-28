using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRMultiplayer
{
    public class TutorialGrab : MonoBehaviour
    {
        [SerializeField] private Transform resetPosition;
        [SerializeField] private TMP_Text pointText;
        [SerializeField] private CutomizeTrigger cutomizeTrigger;

        private int currentPoints = 0;

        private void Start()
        {
            UpdatePointText();

            if (cutomizeTrigger != null)
            {
                cutomizeTrigger.OnTriggerAction += HandleTriggerAction;
            }
        }

        private void OnDestroy()
        {
            if (cutomizeTrigger != null)
            {
                cutomizeTrigger.OnTriggerAction -= HandleTriggerAction;
            }
        }

        private void HandleTriggerAction(Collider other, bool isEnter)
        {
            if (isEnter)
            {
                OnBallEnter(other);
            }
        }

        private void OnBallEnter(Collider ballCollider)
        {
            currentPoints++;
            UpdatePointText();
            GameObject rootBallObject = ballCollider.transform.root.gameObject;
            ForceDropBall(rootBallObject);
            ResetBallPosition(rootBallObject);
        }

        private void ForceDropBall(GameObject ballRoot)
        {
            if (ballRoot.TryGetComponent<XRGrabInteractable>(out XRGrabInteractable grabInteractable))
            {
                if (grabInteractable.isSelected)
                {
                    var interactionManager = grabInteractable.interactionManager;
                    if (interactionManager != null)
                    {
                        interactionManager.CancelInteractableSelection((IXRSelectInteractable)grabInteractable);
                    }
                }
            }
        }

        private void ResetBallPosition(GameObject ballRoot)
        {
            if (resetPosition == null)
            {
                return;
            }

            if (ballRoot.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = true;

                ballRoot.transform.position = resetPosition.position;
                ballRoot.transform.rotation = resetPosition.rotation;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();

                ballRoot.SetActive(false);

                StartCoroutine(ReenablePhysicsNextFrame(rb));
            }
            else
            {
                ballRoot.transform.position = resetPosition.position;
                ballRoot.transform.rotation = resetPosition.rotation;
            }
        }

        private IEnumerator ReenablePhysicsNextFrame(Rigidbody rb)
        {
            yield return new WaitForSeconds(0.5f);
                rb.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void UpdatePointText()
        {
            if (pointText != null)
            {
                pointText.text = currentPoints.ToString();
            }
        }
    }
}
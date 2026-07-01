using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace XRMultiplayer
{
    public class TutorialGrab : MonoBehaviour
    {
        [SerializeField] private Transform resetPosition;
        [SerializeField] private TMP_Text pointText;
        [SerializeField] private CustomizeTrigger cutomizeTrigger;

        [Header("Ball Settings")]
        [SerializeField] private PhysicsMaterial bouncy;

        [SerializeField] private PhysicsMaterial notBouncy;
        [SerializeField] private Collider ballCollider;
        [SerializeField] private XRGrabInteractable xRGrabInteractable;

        [Header("Shader Settings")]
        [SerializeField] private string ballDissolveProperty = "_Blend";

        [SerializeField] private float effectDuration = 0.5f;
        [SerializeField] private float delayBeforeDissolve = 0.1f;
        [SerializeField] private Material ballMat;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;

        [SerializeField] private AudioClip scoreSFX;
        [SerializeField] private AudioClip respawnSFX;

        private int currentPoints = 0;
        private int propertyID;
        private bool isEntered = false;

        private InteractionLayerMask originalInteractionLayer;

        private void Start()
        {
            UpdatePointText();
            propertyID = Shader.PropertyToID(ballDissolveProperty);

            if (cutomizeTrigger != null)
            {
                cutomizeTrigger.OnTriggerAction += HandleTriggerAction;
            }

            if (ballMat != null)
            {
                ballMat.SetFloat(propertyID, 1f);
            }

            if (audioSource == null)
            {
                TryGetComponent(out audioSource);
            }

            if (xRGrabInteractable != null)
            {
                originalInteractionLayer = xRGrabInteractable.interactionLayers;
            }
        }

        private void OnDestroy()
        {
            if (cutomizeTrigger != null)
            {
                cutomizeTrigger.OnTriggerAction -= HandleTriggerAction;
            }
        }

        private void HandleTriggerAction(Collider _ballCollider, bool isEnter)
        {
            if (isEnter)
            {
                OnBallEnter(_ballCollider);
            }
        }

        private void OnBallEnter(Collider ballCollider)
        {
            if (isEntered) return;
            isEntered = true;

            currentPoints++;
            UpdatePointText();
            PlaySound(scoreSFX);

            GameObject rootBallObject = ballCollider.transform.root.gameObject;

            ForceDropAndDisableInteraction(rootBallObject);

            StartCoroutine(BallTeleportFlow(rootBallObject));
        }

        private void ForceDropAndDisableInteraction(GameObject ballRoot)
        {
            if (xRGrabInteractable != null)
            {
                if (xRGrabInteractable.isSelected)
                {
                    var interactionManager = xRGrabInteractable.interactionManager;
                    if (interactionManager != null)
                    {
                        interactionManager.CancelInteractableSelection((IXRSelectInteractable)xRGrabInteractable);
                    }
                }

                xRGrabInteractable.interactionLayers = 0;
            }
        }

        private IEnumerator BallTeleportFlow(GameObject ballRoot)
        {
            Rigidbody rb = ballRoot.GetComponent<Rigidbody>();

            yield return new WaitForSeconds(delayBeforeDissolve);

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (ballMat != null && ballMat.HasProperty(propertyID))
            {
                float elapsed = 0f;
                while (elapsed < effectDuration)
                {
                    elapsed += Time.deltaTime;
                    float blendValue = Mathf.Lerp(1f, 0f, elapsed / effectDuration);
                    ballMat.SetFloat(propertyID, blendValue);
                    yield return null;
                }
                ballMat.SetFloat(propertyID, 0f);
            }

            ballRoot.transform.position = resetPosition.position;
            ballRoot.transform.rotation = resetPosition.rotation;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }

            yield return new WaitForSeconds(0.125f);
            PlaySound(respawnSFX);

            if (ballMat != null && ballMat.HasProperty(propertyID))
            {
                float elapsed = 0f;
                while (elapsed < effectDuration)
                {
                    elapsed += Time.deltaTime;
                    float blendValue = Mathf.Lerp(0f, 1f, elapsed / effectDuration);
                    ballMat.SetFloat(propertyID, blendValue);
                    yield return null;
                }
                ballMat.SetFloat(propertyID, 1f);
            }

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            yield return new WaitForFixedUpdate();

            if (xRGrabInteractable != null)
            {
                xRGrabInteractable.interactionLayers = originalInteractionLayer;
            }

            isEntered = false;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void UpdatePointText()
        {
            if (pointText != null)
            {
                pointText.text = currentPoints.ToString();
            }
        }

        public void OnBallGrabbed()
        {
            this.ballCollider.material = this.notBouncy;
        }

        public void OnBallReleased()
        {
            this.ballCollider.material = this.bouncy;
        }
    }
}
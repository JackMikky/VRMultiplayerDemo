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

        [Header("Shader Settings")]
        [Tooltip("Shader property name, e.g., _Blend or Blend")]
        [SerializeField] private string ballDissolvProperty = "_Blend";

        [Tooltip("Duration of the dissolve or materialization effect (seconds)")]
        [SerializeField] private float effectDuration = 0.5f;

        [Tooltip("Time the ball falls naturally through the hoop before disappearing")]
        [SerializeField] private float delayBeforeDissolve = 0.1f;

        [SerializeField] private Material ballMat;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Sound played immediately when entering the hoop")]
        [SerializeField] private AudioClip scoreSFX;

        [Tooltip("Sound played when the ball fully reappears at the reset position")]
        [SerializeField] private AudioClip respawnSFX;

        private int currentPoints = 0;
        private int propertyID;
        private bool isEntered = false;

        private void Start()
        {
            UpdatePointText();

            propertyID = Shader.PropertyToID(ballDissolvProperty);

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
            if (isEntered)
            {
                return;
            }
            isEntered = true;

            currentPoints++;
            UpdatePointText();

            // Play scoring sound effect instantly
            PlaySound(scoreSFX);

            GameObject rootBallObject = ballCollider.transform.root.gameObject;

            // Force the XR Interaction Toolkit to drop the ball if held
            ForceDropBall(rootBallObject);

            // Execute the sequential visual/physical lifecycle flow
            StartCoroutine(BallTeleportFlow(rootBallObject));
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

            yield return new WaitForFixedUpdate();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
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
    }
}
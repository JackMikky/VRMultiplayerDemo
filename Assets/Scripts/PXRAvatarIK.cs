using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// This class is used to represent the Avatar IK system in both the <see cref="PXRNetworkPlayer"/> and the Offline Player"/>.
    /// </summary>
    public class PXRAvatarIK : MonoBehaviour
    {
        /// <summary>
        /// Transform for the Network Player Head.
        /// </summary>
        [SerializeField, Tooltip("Transform for the Network Player Head.")]
        Transform headTransform;

        /// <summary>
        /// Torso Parent Transform.
        /// </summary>
        [SerializeField, Tooltip("Torso Parent Transform.")]
        Transform torsoParentTransform;

        /// <summary>
        /// Root of the Head Visuals.
        /// </summary>
        [SerializeField, Tooltip("Root of the Head Visuals.")]
        Transform headVisualsRoot;

        /// <summary>
        /// Neck Transform.
        /// </summary>
        [SerializeField, Tooltip("Neck Transform.")]
        Transform neck;

        /// <summary>
        /// Offset to be applied to the head height.
        /// </summary>
        [SerializeField, Tooltip("Offset to be applied to the head height.")]
        float headHeightOffset = .3f;

        /// <summary>
        /// Theshold to where body rotation appoximation is applied.
        /// </summary>
        [Range(0, 360.0f)] [SerializeField, Tooltip("Theshold to where body rotation appoximation is applied.")]
        float rotateThreshold = 25.0f;

        /// <summary>
        /// Speed at which the body rotates.
        /// </summary>
        [SerializeField, Tooltip("Speed at which the body rotates.")]
        float rotateSpeed = 3.0f;

        /// <summary>
        /// Transform associated with this script.
        /// </summary>
        Transform _transform;

        /// <summary>
        /// Rotation destination for the Y euler value.
        /// </summary>
        float _destinationY;

        /// <inheritdoc/>
        private void Start()
        {
            _transform = GetComponent<Transform>();
            _destinationY = headTransform.transform.eulerAngles.y;
        }

        /// <inheritdoc/>
        private void Update()
        {
            // Update Head.
            headVisualsRoot.position = headTransform.position;
            headVisualsRoot.position -= headTransform.up * headHeightOffset;
            neck.rotation = headTransform.rotation;

            // Update Body.
            _transform.position = headTransform.position;
            torsoParentTransform.rotation = Quaternion.Slerp(torsoParentTransform.rotation,
                Quaternion.Euler(new Vector3(0, _destinationY, 0)), Time.deltaTime * rotateSpeed);

            // Rotate Body if past threshold.
            if (Mathf.Abs(torsoParentTransform.eulerAngles.y - headTransform.eulerAngles.y) >= rotateThreshold)
            {
                _destinationY = headTransform.transform.eulerAngles.y;
            }

            // Update scale.
            headVisualsRoot.localScale = headTransform.localScale;
        }
    }
}
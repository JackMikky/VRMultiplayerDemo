using UnityEngine;

public enum FloatingAxis
{
    X,
    NegativeX,
    Y,
    NegativeY,
    Z,
    NegativeZ
}

public class DisplayAnimator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation axis vector")]
    public Vector3 rotationAxis = new Vector3(0, 1, 0);

    [Tooltip("Rotate speed (degrees/fixedDeltaTime)")]
    public float rotationSpeed = 45f;

    [Tooltip("Rotation space: local or world coordinates")]
    public Space rotationSpace = Space.Self;

    [Header("Floating Settings")]
    [Tooltip("Enable floating effect")]
    public bool enableFloating = true;

    [Tooltip("Floating amplitude")]
    [Range(0f, 2f)]
    public float floatAmplitude = 0.2f;

    [Tooltip("Floating frequency")]
    [Range(0.1f, 10f)]
    public float floatFrequency = 2f;

    [Tooltip("Floating space: world coordinates or custom axis")]
    public Space floatingSpace = Space.World;

    [Tooltip("Custom floating axis (only effective when floating space is Self)")]
    public FloatingAxis floatingAxis = FloatingAxis.Y;

    private Vector3 startPos;
    private float floatOffset;

    private Vector3 localAxis;

    private void Start()
    {
        startPos = transform.localPosition;
        floatOffset = Random.Range(0f, Mathf.PI * 2f);
        localAxis = transform.TransformDirection(GetFloatingAxisVector());
    }

    [System.Obsolete]
    private void FixedUpdate()
    {
        if (rotationSpeed != 0f)
        {
            if (rotationSpace == Space.Self)
                transform.RotateAroundLocal(localAxis, rotationSpeed * Time.fixedDeltaTime);
            else
                transform.Rotate(rotationAxis, rotationSpeed * Time.fixedDeltaTime, Space.World);
        }

        if (enableFloating && floatAmplitude > 0f)
        {
            float offset = Mathf.Sin((Time.time * floatFrequency) + floatOffset) * floatAmplitude;

            if (floatingSpace == Space.World)
            {
                float newY = startPos.y + offset;
                transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
            }
            else
            {
                transform.localPosition = startPos + localAxis * offset;
            }
        }
    }

    private Vector3 GetFloatingAxisVector()
    {
        return floatingAxis switch
        {
            FloatingAxis.X => Vector3.right,
            FloatingAxis.NegativeX => Vector3.left,
            FloatingAxis.Y => Vector3.up,
            FloatingAxis.NegativeY => Vector3.down,
            FloatingAxis.Z => Vector3.forward,
            FloatingAxis.NegativeZ => Vector3.back,
            _ => Vector3.up,
        };
    }

    /// <summary>
    /// Reset to start position
    /// </summary>
    public void ResetPosition()
    {
        transform.localPosition = startPos;
    }

    /// <summary>
    /// Set a new start position
    /// </summary>
    public void SetStartPosition(Vector3 newStartPos)
    {
        startPos = newStartPos;
        transform.localPosition = startPos;
    }

    /// <summary>
    /// Pause or resume the animation
    /// </summary>
    public void SetAnimationEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    private void OnValidate()
    {
        // Ensure rotation axis is not a zero vector
        if (rotationAxis == Vector3.zero)
        {
            rotationAxis = Vector3.up;
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class FreeFlyCamera : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private bool useFreeCamera = false;

    [Header("Mouse Movement")]
    [Range(0, 10)]
    [SerializeField] private float speed = 2;

    private float speedCash;
    private int maxSpeed = 100;
    [SerializeField] private float boostSpeed = 15;

    [Tooltip("Scroll To Change BoostSpeed")]
    [SerializeField] private float scrollSensity = 0.5f;

    [Space(10)]
    [Header("Mouse Rotation")]
    [Range(0, 10)]
    [SerializeField] private float X_RotateSensity = 2;

    [Range(0, 10)]
    [SerializeField] private float Y_RotateSensity = 2;

    private float x, y;

    private Action<bool> OnChangeToFreeCamera;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        speedCash = speed;
        boostSpeed = Mathf.Lerp(0.1f, 50, speed * 2.5f);
        OnChangeToFreeCamera += MouseStateChange;
        OnChangeToFreeCamera += Scroll_ChangeSpeed;
        OnChangeToFreeCamera += BoostSpeed;
        OnChangeToFreeCamera += MouseRotation;
        OnChangeToFreeCamera += CameraMovement;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitFreeCamera();
        }

        if (useFreeCamera)
            OnChangeToFreeCamera?.Invoke(useFreeCamera);
    }

    private void MouseStateChange(bool useFreeCamera)
    {
        if (useFreeCamera)
            Cursor.lockState = CursorLockMode.Locked;
        else
            Cursor.lockState = CursorLockMode.Confined;
    }

    private void MouseRotation(bool useFreeCamera)
    {
        if (!useFreeCamera)
        {
            return;
        }

        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        y = mouseDelta.x;
        x = mouseDelta.y;
        var rotate = new Vector3(x * X_RotateSensity, -y * Y_RotateSensity, 0);
        mainCamera.transform.eulerAngles = mainCamera.transform.eulerAngles - rotate;
    }

    private void BoostSpeed(bool useFreeCammera)
    {
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
        {
            speed = boostSpeed;
        }
        else
        {
            speed = speedCash;
        }
    }

    private void CameraMovement(bool useFreeCamera)
    {
        if (!useFreeCamera)
        {
            return;
        }

        var forward_back = 0f;
        if (Keyboard.current != null && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed))
        {
            forward_back += 1;
        }
        else if (Keyboard.current != null && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed))
        {
            forward_back -= 1;
        }

        var lft_right = 0f;
        if (Keyboard.current != null && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed))
        {
            lft_right -= 1;
        }
        else if (Keyboard.current != null && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed))
        {
            lft_right += 1;
        }

        float Up_Down = 0f;
        if (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed)
        {
            Up_Down += 1;
        }
        else if (Keyboard.current != null && Keyboard.current.cKey.isPressed)
        {
            Up_Down -= 1;
        }

        Vector3 delta = new Vector3(lft_right, Up_Down, forward_back) * speed * Time.deltaTime;
        mainCamera.transform.localPosition += mainCamera.transform.TransformDirection(delta);
    }

    private void ExitFreeCamera()
    {
        useFreeCamera = !useFreeCamera;
        if (useFreeCamera == true)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
        }
    }

    private void Scroll_ChangeSpeed(bool useFreeCamera)
    {
        if (!useFreeCamera)
        {
            return;
        }

        float scrollY = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
        if (scrollY > 0f)
        {
            boostSpeed += scrollSensity * scrollY;
            boostSpeed = Mathf.Clamp(boostSpeed, 0.1f, maxSpeed);
        }
        else if (scrollY < 0f)
        {
            boostSpeed += scrollSensity * scrollY;
            boostSpeed = Mathf.Clamp(boostSpeed, 0.1f, maxSpeed);
        }
    }
}
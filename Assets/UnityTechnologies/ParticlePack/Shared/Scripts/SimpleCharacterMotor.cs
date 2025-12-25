using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterMotor : MonoBehaviour
{
    public CursorLockMode cursorLockMode = CursorLockMode.Locked;
    public bool cursorVisible = false;
    [Header("Movement")]
    public float walkSpeed = 2;
    public float runSpeed = 4;
    public float gravity = 9.8f;
    [Space]
    [Header("Look")]
    public Transform cameraPivot;
    public float lookSpeed = 45;
    public bool invertY = true;
    [Space]
    [Header("Smoothing")]
    public float movementAcceleration = 1;

    CharacterController controller;
    Vector3 movement, finalMovement;
    float speed;
    Quaternion targetRotation, targetPivotRotation;


    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = cursorLockMode;
        Cursor.visible = cursorVisible;
        targetRotation = targetPivotRotation = Quaternion.identity;
    }

    void Update()
    {
        UpdateTranslation();
        UpdateLookRotation();
    }


    void UpdateLookRotation()
    {
        if (Mouse.current != null)
        {
            // 新Input Systemでマウス移動量を取得
            var mouseDelta = Mouse.current.delta.ReadValue();
            var x = mouseDelta.y;
            var y = mouseDelta.x;

            x *= invertY ? -1 : 1;

            targetRotation = transform.localRotation * Quaternion.AngleAxis(y * lookSpeed * Time.deltaTime, Vector3.up);
            targetPivotRotation = cameraPivot.localRotation * Quaternion.AngleAxis(x * lookSpeed * Time.deltaTime, Vector3.right);

            transform.localRotation = targetRotation;
            cameraPivot.localRotation = targetPivotRotation;
        }
    }

    void UpdateTranslation()
    {
        float x = 0f;
        float z = 0f;

        // キーボード入力を確認
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed) x = 1f;
            if (Keyboard.current.wKey.isPressed) z = 1f;
            if (Keyboard.current.sKey.isPressed) z = -1f;
        }

        if (controller.isGrounded)
        {
            bool run = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

            // 移動方向を正規化して斜め移動時の速度を一定に
            Vector3 translation = new Vector3(x, 0, z).normalized;

            speed = run ? runSpeed : walkSpeed;
            movement = transform.TransformDirection(translation * speed);
        }
        else
        {
            // 空中時の重力処理
            movement.y -= gravity * Time.deltaTime;
        }

        // スムーズな移動補間
        finalMovement = Vector3.Lerp(finalMovement, movement, Time.deltaTime * movementAcceleration);
        controller.Move(finalMovement * Time.deltaTime);
    }
}

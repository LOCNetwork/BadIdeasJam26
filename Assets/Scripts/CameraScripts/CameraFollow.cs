using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;
    public float mouseMoveSpeed = 0.1f;
    public bool enableParallax = true;
    public float parallaxAmount = 0.2f;

    private Vector3 offset;
    private Camera mainCamera;

    // NUEVO: constante para ajuste pixel-perfect
    private const float pixelsPerUnit = 32f; // Ajusta esto según el PPU de tus sprites

    private void Start()
    {
        offset = transform.position - target.position;
        mainCamera = Camera.main;
    }

    // CAMBIO: se movió de LateUpdate() a FixedUpdate() para sincronizar con la física
    private void FixedUpdate()
    {
        Vector3 desiredPosition = target.position + offset;
        Vector2 mousePosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);

        if (mousePosition.x < 0.1f || mousePosition.x > 0.9f || mousePosition.y < 0.1f || mousePosition.y > 0.9f)
        {
            Vector3 mouseDirection = new Vector3(mousePosition.x - 0.5f, mousePosition.y - 0.5f, 0f);
            desiredPosition += mouseDirection * mouseMoveSpeed;
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // NUEVO: redondeo para alineación a la cuadrícula de píxeles
        smoothedPosition.x = Mathf.Round(smoothedPosition.x * pixelsPerUnit) / pixelsPerUnit;
        smoothedPosition.y = Mathf.Round(smoothedPosition.y * pixelsPerUnit) / pixelsPerUnit;

        transform.position = smoothedPosition;

        if (enableParallax)
        {
            Vector3 parallaxOffset = new Vector3(mousePosition.x - 0.5f, mousePosition.y - 0.5f, 0f) * parallaxAmount;
            transform.position += parallaxOffset;
        }
    }
}
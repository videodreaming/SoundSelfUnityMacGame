using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField, HideInInspector] private float rotationSpeed = 100f;
    [Tooltip("Spin direction. Base °/s is set from InputLevelChantingEllipsesVisual for chant UI, or assign BaseRotationDegreesPerSecond from code.")]
    [SerializeField] private bool rotateClockwise = true;

    /// <summary>Base spin in degrees per second before <see cref="SpeedMultiplier"/> (e.g. set from <c>InputLevelChantingEllipsesVisual</c>).</summary>
    public float BaseRotationDegreesPerSecond
    {
        get => rotationSpeed;
        set => rotationSpeed = value;
    }
    /// <summary>Per-frame multiplier (e.g. chant-driven). Default 1; other scripts may assign each frame.</summary>
    private float _speedMultiplier = 1f;
    public float SpeedMultiplier
    {
        get => _speedMultiplier;
        set => _speedMultiplier = value;
    }

    [Header("Delay Settings")]
    [SerializeField] private float startDelay = 0f;

    private bool canRotate = false;

    private void Start()
    {
        StartCoroutine(StartRotationAfterDelay());
    }

    IEnumerator StartRotationAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        canRotate = true;
    }

    private void Update()
    {
        if (!canRotate) return;

        float direction = rotateClockwise ? -1f : 1f;

        transform.Rotate(0f, 0f, direction * rotationSpeed * _speedMultiplier * Time.deltaTime);
    }
}
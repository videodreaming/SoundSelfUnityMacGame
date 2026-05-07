using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f; // degrees per second
    [SerializeField] private bool rotateClockwise = true;

    [Header("Delay Settings")]
    [SerializeField] private float startDelay = 1f;

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

        transform.Rotate(0f, 0f, direction * rotationSpeed * Time.deltaTime);
    }
}
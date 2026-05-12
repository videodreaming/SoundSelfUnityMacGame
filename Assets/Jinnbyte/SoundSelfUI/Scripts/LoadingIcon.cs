using UnityEngine;
using System.Collections;

public class LoadingIcon : MonoBehaviour
{
    private float rotationSpeed = 100f; // degrees per second
    private bool rotateClockwise = true;

    private void Update()
    {

        float direction = rotateClockwise ? -1f : 1f;

        transform.Rotate(0f, 0f, direction * rotationSpeed * Time.deltaTime);
    }
}
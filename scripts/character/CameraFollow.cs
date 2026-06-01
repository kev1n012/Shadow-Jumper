using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.1f;
    public Vector3 offset = new Vector3(0, 1, -10);

    private float fixedX;

    void Start()
    {
        fixedX = transform.position.x; // Lock in the starting X position
    }

    void FixedUpdate()
    {
        Vector3 desired = new Vector3(
            fixedX,                        // Horizontally fixed
            target.position.y + offset.y,  // Follows player vertically
            target.position.z + offset.z   // Keeps Z offset
        );

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed);
    }
}
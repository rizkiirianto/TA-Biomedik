using UnityEngine;

public class CameraLimits : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;
    public float smoothSpeed = 0.125f;

    public float minX = -1.4f;
    public float maxX = 0.8f;
    public float minY = -5f;
    public float maxY = 2f;
    void LateUpdate()
    {
        Vector3 desiredPosition = player.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
        smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);

        transform.position = smoothedPosition;
    }
}

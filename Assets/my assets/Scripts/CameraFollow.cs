using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("Target to follow (usually the Player)")]
    public Transform target;

    [Tooltip("How smoothly the camera follows the target")]
    public float smoothSpeed = 5f;

    [Tooltip("Offset from the target")]
    public Vector3 offset;

    [Tooltip("If true, only follow on the X axis")]
    public bool followXOnly = false;
    
    [Tooltip("If true, only follow on the Y axis")]
    public bool followYOnly = false;

    [Header("Camera Shake")]
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    private void Start()
    {
        // 긴급 복구: 혹시 꼬여버린 카메라 뷰포트를 무조건 정상(100%)으로 강제 초기화
        Camera cam = GetComponent<Camera>();
        if (cam != null) cam.rect = new Rect(0, 0, 1, 1);

        // If target is not set, try to find the player
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        // Set initial offset if it's zero
        if (offset == Vector3.zero && target != null)
        {
            offset = transform.position - target.position;
            offset.x = 0f; // Usually we want to follow exactly on X horizontally
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate the desired position based on the target's position and the offset
        Vector3 desiredPosition = target.position + offset;
        
        // Keep the original Z position of the camera
        desiredPosition.z = transform.position.z;

        if (followXOnly)
        {
            desiredPosition.y = transform.position.y;
        }
        else if (followYOnly)
        {
            desiredPosition.x = transform.position.x;
        }

        // Smoothly interpolate between the camera's current position and the desired position
        Vector3 currentUnshaken = transform.position - shakeOffset;
        Vector3 smoothedPosition = Vector3.Lerp(currentUnshaken, desiredPosition, smoothSpeed * Time.deltaTime);
        
        if (shakeDuration > 0)
        {
            shakeOffset = (Vector3)Random.insideUnitCircle * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeDuration = 0f;
            shakeOffset = Vector3.zero;
        }
        
        // Apply the new position
        transform.position = smoothedPosition + shakeOffset;
    }
}

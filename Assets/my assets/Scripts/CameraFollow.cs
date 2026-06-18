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

    [Header("Intro Cinematic")]
    [HideInInspector] public bool isIntroCinematic = false;
    private float originalOrthoSize = 5f;

    [Tooltip("보스(또는 지정 타겟)로 줌인하는 시간 (초)")]
    public float zoomInDuration = 0.8f;
    [Tooltip("보스를 비추며 멈춰있는 대기 시간 (초)")]
    public float zoomHoldDuration = 0.4f;
    [Tooltip("플레이어로 줌아웃하며 복귀하는 시간 (초)")]
    public float zoomOutDuration = 0.8f;

    /// <summary>
    /// 게임 진입 시 보스 쪽으로 화면을 줌인했다가 부드럽게 플레이어로 복귀시키는 인트로 연출을 수행합니다.
    /// </summary>
    public void StartIntroCinematic(Transform bossTransform, float duration = 2.5f)
    {
        StartCoroutine(IntroCinematicRoutine(bossTransform));
    }

    private System.Collections.IEnumerator IntroCinematicRoutine(Transform bossTransform)
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null || bossTransform == null) yield break;

        // 타겟이 없으면 플레이어를 찾아둡니다.
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        if (target == null) yield break;

        isIntroCinematic = true;
        originalOrthoSize = cam.orthographicSize;

        // 줌인 목표 사이즈 (원래 사이즈의 65%)
        float targetOrthoSize = originalOrthoSize * 0.65f;
        
        // 1. 보스 위치로 줌인하며 이동
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 타임스케일 영향 없이 동작
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomInDuration);
            
            Vector3 targetPos = bossTransform.position;
            targetPos.z = transform.position.z;
            
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.orthographicSize = Mathf.Lerp(originalOrthoSize, targetOrthoSize, t);
            yield return null;
        }
        
        // 2. 보스에게 줌인한 상태로 잠깐 대기
        yield return new WaitForSecondsRealtime(zoomHoldDuration);

        // 3. 다시 플레이어 위치로 줌아웃하며 복귀
        elapsed = 0f;
        Vector3 zoomOutStartPos = transform.position;
        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomOutDuration);
            
            Vector3 targetPos = target.position + offset;
            targetPos.z = transform.position.z;
            
            transform.position = Vector3.Lerp(zoomOutStartPos, targetPos, t);
            cam.orthographicSize = Mathf.Lerp(targetOrthoSize, originalOrthoSize, t);
            yield return null;
        }

        // 상태 리셋
        cam.orthographicSize = originalOrthoSize;
        isIntroCinematic = false;
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
        if (isIntroCinematic) return; // 🌟 인트로 연출 중에는 플레이어 자동 추적 정지
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

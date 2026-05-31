using UnityEngine;
using System;
using UnityEngine.Events;

/// <summary>
/// 플레이어를 특정 목적지로 자동 이동시키는 시네마틱/이벤트용 연출 컴포넌트입니다.
/// 수동 이동(PlayerMoving) 스크립트와의 물리적 충돌(경쟁 상태)을 차단하고,
/// Rigidbody2D를 이용하여 부드럽고 안전하게 이동을 처리합니다.
/// </summary>
public class PlayerAutoMove : MonoBehaviour
{
    [Header("=== 자동 시작 설정 ===")]
    [Tooltip("게임 시작 시 자동으로 목적지로 주행을 시작할지 여부")]
    public bool autoStartOnPlay = false;
    [Tooltip("자동 시작 시 목적지 (인스펙터에서 Checkpoint_01_Trigger 등 연결)")]
    public Transform autoStartTarget;
    [Tooltip("자동 주행 속도")]
    public float autoSpeed = 8.5f;

    [Header("=== 이벤트 설정 ===")]
    [Tooltip("목적지에 도착했을 때 실행할 인스펙터 이벤트")]
    public UnityEvent onArrivedEvent;

    private Rigidbody2D rb;
    private Vector3 targetPosition;
    private float moveSpeed;
    private Action onArrivedCallback;
    private bool isAutoMoving = false;

    /// <summary>
    /// 현재 플레이어가 자동 이동 중인지 여부
    /// </summary>
    public bool IsAutoMoving => isAutoMoving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // 인스펙터에서 자동 시작이 설정되어 있는 경우 주행 기동
        if (autoStartOnPlay && autoStartTarget != null)
        {
            MoveTo(autoStartTarget.position, autoSpeed, null);
        }
    }

    /// <summary>
    /// 플레이어를 목적지까지 자동으로 이동시킵니다.
    /// </summary>
    /// <param name="destination">목적지 월드 좌표</param>
    /// <param name="speed">이동 속도</param>
    /// <param name="onArrived">도착 시 실행할 콜백 함수 (C# 델리게이트용)</param>
    public void MoveTo(Vector3 destination, float speed, Action onArrived)
    {
        targetPosition = destination;
        moveSpeed = speed;
        onArrivedCallback = onArrived;
        isAutoMoving = true;
        
        Debug.Log($"🏃 [PlayerAutoMove] 자동 이동을 시작합니다. 목적지: {destination}, 속도: {speed}");
    }

    /// <summary>
    /// 자동 이동을 즉시 중단합니다.
    /// </summary>
    public void Stop()
    {
        isAutoMoving = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        Debug.Log("🛑 [PlayerAutoMove] 자동 이동을 강제 중단했습니다.");
    }

    private void FixedUpdate()
    {
        if (!isAutoMoving || rb == null) return;

        float distance = Vector3.Distance(transform.position, targetPosition);
        
        // 목표 지점에 매우 가깝게 도달했을 때 정차 처리
        if (distance > 0.25f)
        {
            Vector2 direction = ((Vector2)targetPosition - rb.position).normalized;
            rb.velocity = direction * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
            isAutoMoving = false;
            
            Debug.Log("🎯 [PlayerAutoMove] 자동 이동 목적지에 도착 완료!");
            
            // 도착 알림 콜백 및 유니티 이벤트 실행
            onArrivedCallback?.Invoke();
            onArrivedEvent?.Invoke();
        }
    }
}

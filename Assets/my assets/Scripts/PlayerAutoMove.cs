using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// 각 목적지별 도달 이벤트를 인스펙터에서 설정할 수 있도록 돕는 경로 데이터 클래스입니다.
/// </summary>
[System.Serializable]
public class WaypointEvent
{
    [Tooltip("이동할 목적지 좌표")]
    public Transform targetTransform;
    
    [Tooltip("이 목적지에 도착했을 때 개별적으로 실행할 인스펙터 이벤트 (예: 말풍선 띄우기 등)")]
    public UnityEvent onArrivedWaypoint;
}

/// <summary>
/// 플레이어를 여러 목적지(경유지)로 순차적 자동 이동시키는 시네마틱/이벤트용 연출 컴포넌트입니다.
/// 수동 이동(PlayerMoving) 스크립트와의 물리적 충돌(경쟁 상태)을 차단하고,
/// Rigidbody2D를 이용하여 부드럽고 안전하게 이동을 처리합니다.
/// </summary>
public class PlayerAutoMove : MonoBehaviour
{
    [Header("=== 자동 시작 설정 ===")]
    [Tooltip("게임 시작 시 자동으로 목적지 경로로 주행을 시작할지 여부")]
    public bool autoStartOnPlay = false;
    
    [Tooltip("순차적으로 거쳐 갈 목적지 경로 목록 (여러 개를 연결하여 구불구불한 주행 및 개별 연출 가능)")]
    public List<WaypointEvent> autoStartTargets = new List<WaypointEvent>();
    
    [Tooltip("자동 주행 속도")]
    public float autoSpeed = 8.5f;

    [Header("=== 이벤트 설정 ===")]
    [Tooltip("등록된 모든 목적지에 최종적으로 완료 도달했을 때 실행할 인스펙터 이벤트")]
    public UnityEvent onArrivedEvent;

    private Rigidbody2D rb;
    private List<WaypointEvent> movePath = new List<WaypointEvent>();
    private int currentPathIndex = 0;
    private float moveSpeed;
    private Action onArrivedCallback;
    private bool isAutoMoving = false;
    private bool isPaused = false; // ⏸️ 일시정지 상태 여부

    /// <summary>
    /// 현재 플레이어가 자동 이동 중인지 여부
    /// </summary>
    public bool IsAutoMoving => isAutoMoving;

    /// <summary>
    /// 현재 플레이어가 자동 주행 중 일시정지 상태인지 여부
    /// </summary>
    public bool IsPaused => isPaused;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // 인스펙터에 등록된 다중 목적지 경로가 있다면 즉시 순차 이동 개시
        if (autoStartOnPlay && autoStartTargets != null && autoStartTargets.Count > 0)
        {
            MoveToPath(autoStartTargets, autoSpeed, null);
        }
    }

    /// <summary>
    /// 단일 목적지로 자동 이동시킵니다. (이전 버전 스크립트들과의 호환성 완벽 유지)
    /// </summary>
    public void MoveTo(Vector3 destination, float speed, Action onArrived)
    {
        // 호환용 더미 WaypointEvent 구성
        GameObject dummyObj = new GameObject("DummyWaypoint");
        dummyObj.transform.position = destination;
        
        WaypointEvent dummyWaypoint = new WaypointEvent
        {
            targetTransform = dummyObj.transform,
            onArrivedWaypoint = new UnityEvent()
        };
        
        List<WaypointEvent> singlePath = new List<WaypointEvent> { dummyWaypoint };
        
        // 이동 후에 더미 오브젝트 삭제되도록 바인딩
        MoveToPath(singlePath, speed, () => {
            onArrived?.Invoke();
            Destroy(dummyObj);
        });
    }

    /// <summary>
    /// 여러 목적지 좌표를 순차적으로 거쳐 이동하도록 만듭니다.
    /// </summary>
    /// <param name="path">이동할 웨이포인트 이벤트 목록</param>
    /// <param name="speed">이동 속도</param>
    /// <param name="onArrived">최종 목적지 도착 시 실행할 콜백</param>
    public void MoveToPath(List<WaypointEvent> path, float speed, Action onArrived)
    {
        if (path == null || path.Count == 0) return;

        movePath = path;
        currentPathIndex = 0;
        moveSpeed = speed;
        onArrivedCallback = onArrived;
        isAutoMoving = true;
        isPaused = false; // 시작 시 리셋

        Debug.Log($"🏃 [PlayerAutoMove] 순차 자동 주행 개시! 총 경유지: {path.Count}개, 속도: {speed}");
    }

    /// <summary>
    /// 자동 주행 중인 플레이어를 그 자리에 즉시 일시 정지시킵니다. (대사 출력 시 호출하기 좋음)
    /// </summary>
    public void PauseMove()
    {
        if (!isAutoMoving) return;
        isPaused = true;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // 🎬 정지 시 블렌드 트리 이동 활성화 꺼줌
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("isMoving", false);
        }

        Debug.Log("⏸️ [PlayerAutoMove] 자동 주행을 일시 정지(Pause)했습니다.");
    }

    /// <summary>
    /// 일시 정지되었던 자동 주행을 다시 해제하고 남은 경로로 주행을 이어갑니다.
    /// </summary>
    public void ResumeMove()
    {
        if (!isAutoMoving) return;
        isPaused = false;
        Debug.Log("▶️ [PlayerAutoMove] 일시 정지를 해제하고 주행을 재개(Resume)합니다.");
    }

    /// <summary>
    /// 자동 이동을 즉시 중단합니다.
    /// </summary>
    public void Stop()
    {
        isAutoMoving = false;
        isPaused = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // 🎬 애니메이션 즉시 정지
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("isMoving", false);
        }

        Debug.Log("🛑 [PlayerAutoMove] 자동 이동을 강제 중단했습니다.");
    }

    private void FixedUpdate()
    {
        if (!isAutoMoving || rb == null || movePath.Count == 0) return;

        // ⏸️ 일시 정지 상태인 경우, 속도를 0으로 고정 유지하고 업데이트 대기
        if (isPaused)
        {
            rb.velocity = Vector2.zero;
            
            // 🎬 정지 상태를 블렌드 트리에 주입
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("isMoving", false);
            }
            return;
        }

        WaypointEvent currentWaypoint = movePath[currentPathIndex];
        if (currentWaypoint == null || currentWaypoint.targetTransform == null)
        {
            HandleWaypointArrival();
            return;
        }

        Vector3 targetPos = currentWaypoint.targetTransform.position;
        float distance = Vector2.Distance(rb.position, targetPos); // Z축 좌표 어긋남을 원천 차단하기 위해 2D 거리 검사

        // 현재 목표 경유지에 도달했는지 체크
        if (distance > 0.25f)
        {
            Vector2 direction = ((Vector2)targetPos - rb.position).normalized;
            rb.velocity = direction * moveSpeed;

            // 🎬 [8방향 블렌드 트리 실시간 방향값 동기화!]
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("isMoving", true);
                anim.SetFloat("InputX", direction.x);
                anim.SetFloat("InputY", direction.y);
            }
        }
        else
        {
            HandleWaypointArrival();
        }
    }

    private void HandleWaypointArrival()
    {
        // 1. 해당 웨이포인트 개별 이벤트 발동!
        WaypointEvent currentWaypoint = movePath[currentPathIndex];
        if (currentWaypoint != null && currentWaypoint.onArrivedWaypoint != null)
        {
            Debug.Log($"🎉 [PlayerAutoMove] 경유지 ({currentPathIndex + 1}/{movePath.Count}) 도착! 개별 이벤트 실행");
            currentWaypoint.onArrivedWaypoint.Invoke();
        }

        // 2. 다음 경로로 스위칭하거나 완료 처리
        if (currentPathIndex < movePath.Count - 1)
        {
            currentPathIndex++;
            
            // 안전 보강: 다음 타겟의 Transform과 이름 널체크 처리
            string nextTargetName = "None";
            if (movePath[currentPathIndex] != null && movePath[currentPathIndex].targetTransform != null)
            {
                nextTargetName = movePath[currentPathIndex].targetTransform.name;
            }
            Debug.Log($"🎯 [PlayerAutoMove] 다음 경유지 ({currentPathIndex + 1}/{movePath.Count})로 선회: {nextTargetName}");
        }
        else
        {
            // 최종 목적지 도달 시 완전히 정지
            rb.velocity = Vector2.zero;
            isAutoMoving = false;
            isPaused = false;

            // 🎬 완주 정지 상태를 블렌드 트리에 최종 적용
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("isMoving", false);
            }

            Debug.Log("🏁 [PlayerAutoMove] 최종 목적지까지 완벽히 주행 완료!");

            // 최종 완료 콜백 및 이벤트 발동
            onArrivedCallback?.Invoke();
            onArrivedEvent?.Invoke();
        }
    }
}

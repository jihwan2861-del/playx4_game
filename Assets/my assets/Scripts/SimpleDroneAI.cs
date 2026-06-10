using UnityEngine;

/// <summary>
/// 플레이어를 타겟 조준하여 지정된 속도로 천천히 유도 추적하는 극도로 단순한 드론 AI 스크립트입니다.
/// 처음에는 인스펙터에서 이 컴포넌트를 꺼두었다가(enabled = false), 
/// 트리거/대사 이벤트가 완료되는 시점에 스크립트를 켜주면(enabled = true) 플레이어 추적을 개시합니다.
/// </summary>
public class SimpleDroneAI : MonoBehaviour
{
    [Header("=== 추적 설정 ===")]
    [Tooltip("플레이어를 향해 다가갈 이동 속도")]
    public float speed = 1.2f;
    [Tooltip("플레이어와 너무 겹치지 않게 멈출 최소 거리")]
    public float stopDistance = 0.5f;

    private Transform playerTarget;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 플레이어 검색 및 캐싱
        if (Player.instance != null)
        {
            playerTarget = Player.instance.transform;
        }
    }

    private void Update()
    {
        // 실시간 플레이어 소생 및 상태 검색
        if (playerTarget == null)
        {
            if (Player.instance != null)
            {
                playerTarget = Player.instance.transform;
            }
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTarget.position);

        // 일정 거리 이상 떨어져 있을 때만 플레이어 위치로 이동
        if (distance > stopDistance)
        {
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, speed * Time.deltaTime);

            // 이동 방향에 맞춰 좌우 스프라이트 반전 제어
            if (spriteRenderer != null)
            {
                if (direction.x > 0.05f)
                {
                    spriteRenderer.flipX = false; // 오른쪽 바라봄
                }
                else if (direction.x < -0.05f)
                {
                    spriteRenderer.flipX = true; // 왼쪽 바라봄
                }
            }
        }
    }
}

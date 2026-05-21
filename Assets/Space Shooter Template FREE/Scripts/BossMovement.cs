using UnityEngine;

/// <summary>
/// 보스가 플레이어를 서서히 쫓아가게 만들고, 이동 방향에 따라 바라보는 방향(좌/우)을 뒤집는 스크립트입니다.
/// </summary>
public class BossMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 2.0f;        // 보스가 쫓아오는 속도
    public bool isChasingPlayer = true;   // 플레이어를 쫓아갈지 여부
    public float stopDistance = 3.0f;     // 플레이어와 이 거리보다 가까워지면 멈춤 (Idle)

    [Header("애니메이션 설정")]
    public Animator bossAnimator;         // 보스의 애니메이터
    [Tooltip("이동 중일 때 Animator에서 켤 Bool 파라미터 이름 (예: isRunning)")]
    public string runBoolName = "isRunning"; 

    [Header("6방향 및 8방향 정지 이미지 설정 (애니메이션이 없는 정지 단일 이미지용)")]
    [Tooltip("아래쪽 이동 시 이미지 (앞모습)")]
    public Sprite spriteFront;
    [Tooltip("위쪽 이동 시 이미지 (뒷모습)")]
    public Sprite spriteBack;
    [Tooltip("좌상단 대각선 이동 시 이미지")]
    public Sprite spriteUpLeft;
    [Tooltip("우상단 대각선 이동 시 이미지")]
    public Sprite spriteUpRight;
    [Tooltip("좌하단 대각선 이동 시 이미지")]
    public Sprite spriteDownLeft;
    [Tooltip("우하단 대각선 이동 시 이미지")]
    public Sprite spriteDownRight;
    [Tooltip("좌측 이동 시 이미지 (8방향용 옆모습)")]
    public Sprite spriteLeft;
    [Tooltip("우측 이동 시 이미지 (8방향용 옆모습)")]
    public Sprite spriteRight;

    private SpriteRenderer spriteRenderer;
    private Player playerTarget;

    void Start()
    {
        // 컴포넌트 자동 연결
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (bossAnimator == null)
        {
            bossAnimator = GetComponentInChildren<Animator>();
            if (bossAnimator == null) bossAnimator = GetComponent<Animator>();
        }

        // 플레이어 찾기
        if (Player.instance != null)
        {
            playerTarget = Player.instance;
        }
    }

    void Update()
    {
        if (playerTarget == null)
        {
            if (Player.instance != null) playerTarget = Player.instance;
            else 
            {
                Debug.LogWarning("⚠️ [BossMovement] 플레이어를 찾을 수 없습니다! Player.instance가 null입니다.");
                return;
            }
        }

        if (isChasingPlayer)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.transform.position);
            Vector3 direction = (playerTarget.transform.position - transform.position).normalized;

            if (bossAnimator != null)
            {
                bossAnimator.SetFloat("dirX", direction.x);
                bossAnimator.SetFloat("dirY", direction.y);
            }

            // 6방향/8방향 스마트 스프라이트 실시간 이미지 교체 적용
            UpdateBossSprite(direction);

            if (distanceToPlayer > stopDistance)
            {
                transform.position += direction * moveSpeed * Time.deltaTime;
                if (bossAnimator != null && !string.IsNullOrEmpty(runBoolName))
                {
                    bossAnimator.SetBool(runBoolName, true);
                }
            }
            else
            {
                if (bossAnimator != null && !string.IsNullOrEmpty(runBoolName))
                {
                    bossAnimator.SetBool(runBoolName, false);
                }
            }
        }
    }

    /// <summary>
    /// 이동 방향 각도를 계산하여 6방향/8방향 정지 스프라이트를 실시간 교체합니다.
    /// </summary>
    private void UpdateBossSprite(Vector3 direction)
    {
        if (spriteRenderer == null || direction == Vector3.zero) return;

        // 아무 이미지도 슬롯에 채워져 있지 않다면 스킵
        if (spriteFront == null && spriteBack == null && spriteUpLeft == null && 
            spriteUpRight == null && spriteDownLeft == null && spriteDownRight == null &&
            spriteLeft == null && spriteRight == null)
        {
            return; 
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 좌/우 스프라이트 중 하나라도 등록되어 있으면 8방향 모드 가동
        bool use8Direction = (spriteLeft != null || spriteRight != null);

        if (use8Direction)
        {
            // 8방향 각도 (45도 기준 분할)
            if (angle >= 67.5f && angle < 112.5f)
            {
                if (spriteBack != null) spriteRenderer.sprite = spriteBack;         // 위 (상)
            }
            else if (angle >= -112.5f && angle < -67.5f)
            {
                if (spriteFront != null) spriteRenderer.sprite = spriteFront;       // 아래 (하)
            }
            else if (angle >= 22.5f && angle < 67.5f)
            {
                if (spriteUpRight != null) spriteRenderer.sprite = spriteUpRight;   // 우상
            }
            else if (angle >= -67.5f && angle < -22.5f)
            {
                if (spriteDownRight != null) spriteRenderer.sprite = spriteDownRight; // 우하
            }
            else if (angle >= 112.5f && angle < 157.5f)
            {
                if (spriteUpLeft != null) spriteRenderer.sprite = spriteUpLeft;     // 좌상
            }
            else if (angle >= -157.5f && angle < -112.5f)
            {
                if (spriteDownLeft != null) spriteRenderer.sprite = spriteDownLeft; // 좌하
            }
            else if (angle >= -22.5f && angle < 22.5f)
            {
                if (spriteRight != null) spriteRenderer.sprite = spriteRight;       // 우 (우측)
                else if (spriteUpRight != null) spriteRenderer.sprite = spriteUpRight; // 우측이 없을 때 우상단으로 대체
            }
            else // (angle >= 157.5f && angle <= 180f || angle >= -180f && angle < -157.5f)
            {
                if (spriteLeft != null) spriteRenderer.sprite = spriteLeft;         // 좌 (좌측)
                else if (spriteUpLeft != null) spriteRenderer.sprite = spriteUpLeft;   // 좌측이 없을 때 좌상단으로 대체
            }
        }
        else
        {
            // 기존 6방향 매핑 (좌/우가 없을 때 대각선 위주로 처리)
            if (angle >= 67.5f && angle < 112.5f)
            {
                if (spriteBack != null) spriteRenderer.sprite = spriteBack;         // 위 (수직)
            }
            else if (angle >= -112.5f && angle < -67.5f)
            {
                if (spriteFront != null) spriteRenderer.sprite = spriteFront;       // 아래 (수직)
            }
            else if (angle >= 0f && angle < 67.5f)
            {
                if (spriteUpRight != null) spriteRenderer.sprite = spriteUpRight;   // 우상단
            }
            else if (angle >= -67.5f && angle < 0f)
            {
                if (spriteDownRight != null) spriteRenderer.sprite = spriteDownRight; // 우하단
            }
            else if (angle >= 112.5f && angle <= 180f || angle >= -180f && angle < -157.5f)
            {
                if (spriteUpLeft != null) spriteRenderer.sprite = spriteUpLeft;     // 좌상단
            }
            else
            {
                if (spriteDownLeft != null) spriteRenderer.sprite = spriteDownLeft; // 좌하단
            }
        }
    }
}

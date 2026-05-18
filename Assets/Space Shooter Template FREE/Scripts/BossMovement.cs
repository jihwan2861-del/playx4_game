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
            else
            {
                Debug.LogError("🚨 [BossMovement] Animator가 연결되지 않았습니다! Inspector에서 Boss Animator 칸을 채워주세요.");
            }

            if (distanceToPlayer > stopDistance)
            {
                transform.position += direction * moveSpeed * Time.deltaTime;
                if (bossAnimator != null && !string.IsNullOrEmpty(runBoolName))
                {
                    bossAnimator.SetBool(runBoolName, true);
                    // Debug.Log($"🏃 [BossMovement] 이동 중! 거리: {distanceToPlayer}, dirX: {direction.x}, dirY: {direction.y}");
                }
            }
            else
            {
                if (bossAnimator != null && !string.IsNullOrEmpty(runBoolName))
                {
                    bossAnimator.SetBool(runBoolName, false);
                    // Debug.Log($"🛑 [BossMovement] 타겟 도착하여 정지! 거리: {distanceToPlayer}");
                }
            }
        }
    }
}

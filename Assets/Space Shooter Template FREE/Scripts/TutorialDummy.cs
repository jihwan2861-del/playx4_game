using System.Collections;
using UnityEngine;

/// <summary>
/// 튜토리얼에서 유저의 연습을 도와주는 샌드백(더미) 봇입니다.
/// </summary>
public class TutorialDummy : MonoBehaviour
{
    [Header("더미 모드 설정")]
    public bool isShooterMode = true;   // 켜두면 플레이어에게 규칙적으로 총알을 쏩니다 (회피 연습용)
    public bool isHackingMode = false;  // 켜두면 다가갔을 때 체력이 깎입니다 (해킹 연습용)

    [Header("사격 연습 (회피용)")]
    public GameObject bulletPrefab;     // 발사할 총알 프리팹
    public float shootInterval = 3.0f;  // 총알 쏘는 간격
    public float bulletSpeed = 5.0f;    // 튜토리얼용이므로 피하기 쉽게 속도 조절 가능

    [Header("해킹 연습 (보스용)")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float hackingRadius = 4.0f;  // 해킹 반경
    
    [Tooltip("해킹존 밖에서도 평소에 닳는 기본 체력(시간)")]
    public float baseDrainPerSecond = 5f; 
    [Tooltip("해킹존 안에서 닳는 추가 데미지")]
    public float damagePerSecond = 20f; 

    private LineRenderer auraLine;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Color mintColor = new Color(0.3f, 1f, 0.9f, 1f); // 민트색
    private bool isInHackingZone = false;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (isShooterMode && bulletPrefab != null)
        {
            StartCoroutine(ShootRoutine());
        }

        if (isHackingMode)
        {
            SetupAuraLine();
        }
    }

    void Update()
    {
        if (isHackingMode && Player.instance != null)
        {
            float dist = Vector3.Distance(transform.position, Player.instance.transform.position);

            // 항상 기본적으로 체력이 닳게 설정 (보스 생존 타이머처럼)
            currentHealth -= baseDrainPerSecond * Time.deltaTime;

            if (dist <= hackingRadius)
            {
                // 해킹 중: 게임씬의 LevelController를 연동해서 UI와 타이머를 깎음
                if (LevelController.instance != null)
                    LevelController.instance.isHacking = true;

                // 해킹 데미지 추가
                currentHealth -= damagePerSecond * Time.deltaTime;

                // 처음 진입했을 때만 민트색으로 전환
                if (!isInHackingZone)
                {
                    isInHackingZone = true;
                    if (spriteRenderer != null) spriteRenderer.color = mintColor;
                    StartCoroutine(HackingGlowRoutine());
                }

                // 해킹 진행도: 감소된 체력만큼 진행도를 업데이트 (100 - 남은체력%)
                if (MissionPanel.instance != null)
                {
                    int hackIndex = MissionPanel.instance.FindMissionIndexByKeyword("해킹");
                    if (hackIndex != -1)
                    {
                        int target = MissionPanel.instance.missions[hackIndex].targetCount;
                        int hacked = Mathf.RoundToInt((1f - currentHealth / maxHealth) * target);
                        MissionPanel.instance.SetProgress(hackIndex, hacked);
                    }
                }

                if (auraLine != null)
                {
                    auraLine.startColor = mintColor;
                    auraLine.endColor = mintColor;
                    auraLine.startWidth = 0.15f;
                    auraLine.endWidth = 0.15f;
                }
            }
            else
            {
                // 해킹 아님: 원상 복구
                if (LevelController.instance != null)
                    LevelController.instance.isHacking = false;

                // 해킹존을 벗어났을 때 원래 색상으로 복구
                if (isInHackingZone)
                {
                    isInHackingZone = false;
                    StopCoroutine(HackingGlowRoutine());
                    if (spriteRenderer != null) spriteRenderer.color = originalColor;
                }

                if (auraLine != null)
                {
                    auraLine.startColor = new Color(1f, 1f, 1f, 0.2f);
                    auraLine.endColor = new Color(1f, 1f, 1f, 0.2f);
                    auraLine.startWidth = 0.08f;
                    auraLine.endWidth = 0.08f;
                }
            }

            // 공통 사망 처리
            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }

    void OnDisable()
    {
        // 봇이 파괴되거나 비활성화될 때 해킹 모드 안전하게 끄기
        if (isHackingMode && LevelController.instance != null)
        {
            LevelController.instance.isHacking = false;
        }
    }

    IEnumerator ShootRoutine()
    {
        // 튜토리얼이므로 여유롭게 발사
        while (true)
        {
            yield return new WaitForSeconds(shootInterval);

            if (Player.instance != null && bulletPrefab != null)
            {
                // 플레이어를 향해 45도 범위 내에서 6발의 부채꼴(Fan) 발사
                Vector3 baseDirection = (Player.instance.transform.position - transform.position).normalized;
                float startAngle = -22.5f; // 45도의 절반
                float angleStep = 9.0f;    // 45 / (6 - 1) = 9도 간격
                
                // 프리팹에서 visualAngleOffset 값을 사전에 파악합니다.
                float visualOffset = 0f;
                DirectMoving prefabDm = bulletPrefab.GetComponent<DirectMoving>();
                if (prefabDm != null)
                {
                    visualOffset = prefabDm.visualAngleOffset;
                }

                for (int i = 0; i < 6; i++)
                {
                    float angle = startAngle + (i * angleStep);
                    Vector3 bulletDir = Quaternion.Euler(0, 0, angle) * baseDirection;
                    
                    // 총알이 날아갈 방향(bulletDir)을 기반으로 정확한 회전 각도 계산
                    float targetAngleDeg = Mathf.Atan2(bulletDir.y, bulletDir.x) * Mathf.Rad2Deg;
                    float rotZ = targetAngleDeg - 90f + visualOffset;
                    Quaternion rotation = Quaternion.Euler(0, 0, rotZ);
                    
                    GameObject bullet = Instantiate(bulletPrefab, transform.position, rotation);
                    
                    // DirectMoving 설정 조정 (Update 이동 방향 보정 및 중복 조준 방지)
                    DirectMoving bulletDm = bullet.GetComponent<DirectMoving>();
                    if (bulletDm != null)
                    {
                        bulletDm.aimAtPlayerOnStart = false; // 수동으로 계산한 각도를 덮어쓰지 않도록 강제 비활성화
                        bulletDm.speed = bulletSpeed;        // 튜토리얼용 속도로 명시적 갱신
                    }

                    // Rigidbody2D를 사용하는 물리 이동도 함께 연동
                    Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.velocity = bulletDir * bulletSpeed;
                    }
                }
                // ShootRoutine 안에서 총 쏘기 직전에 추가
                GetComponent<Animator>().SetTrigger("Attack");

                // 튜토리얼 봇은 총을 쏠 때 살짝 깜빡임 (전조 증상)
                if (spriteRenderer != null)
                {
                    StartCoroutine(FlashRoutine());
                }
            }
        }
    }

    IEnumerator FlashRoutine()
    {
        // 총 쏠 때 잠깐 빨간색으로 번쩍임 (해킹 중이면 민트→빨강→민트, 평소엔 원본→빨강→원본)
        Color beforeFlash = isInHackingZone ? mintColor : originalColor;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = beforeFlash;
    }

    IEnumerator HackingGlowRoutine()
    {
        // 해킹존에 있는 동안 민트색이 파르르 떨리면서 빛나는 효과
        while (isInHackingZone)
        {
            if (spriteRenderer != null)
            {
                // 밝게
                float brightness = Mathf.PingPong(Time.time * 3f, 0.4f);
                spriteRenderer.color = new Color(
                    mintColor.r,
                    mintColor.g,
                    mintColor.b,
                    0.6f + brightness
                );
            }
            yield return null;
        }
        // 루프 종료 시 색상 복구
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    void SetupAuraLine()
    {
        auraLine = gameObject.AddComponent<LineRenderer>();
        auraLine.useWorldSpace = false; 
        auraLine.loop = true;
        auraLine.positionCount = 64;

        // 두께를 넉넉하게 (너무 얇으면 안 보임)
        auraLine.startWidth = 0.12f;
        auraLine.endWidth = 0.12f;

        // 스프라이트 위에 확실히 그려지도록 소팅 레이어 높임
        auraLine.sortingLayerName = "Default";
        auraLine.sortingOrder = 10;

        // 머티리얼 - Sprites/Default 없으면 Legacy/Particles 로 폴백
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
        auraLine.material = new Material(shader);

        // 원 좌표 계산
        float angle = 0f;
        for (int i = 0; i < 64; i++)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * hackingRadius;
            float y = Mathf.Sin(Mathf.Deg2Rad * angle) * hackingRadius;
            auraLine.SetPosition(i, new Vector3(x, y, 0));
            angle += (360f / 64f);
        }

        // 기본 색상: 반투명 흰색이 아닌 좀 더 불투명하게
        Color defaultColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);
        auraLine.startColor = defaultColor;
        auraLine.endColor = defaultColor;
    }

    void Die()
    {
        Debug.Log("🎉 더미 봇 처치 완료!");

        // 미션 패널 - "체력" / "처치" / "격퇴" 관련 미션 완료
        if (MissionPanel.instance != null)
        {
            MissionPanel.instance.AddProgressByKeyword("체력", 1);
            MissionPanel.instance.AddProgressByKeyword("처치", 1);
            MissionPanel.instance.AddProgressByKeyword("격퇴", 1);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (isHackingMode)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, hackingRadius);
        }
    }
}

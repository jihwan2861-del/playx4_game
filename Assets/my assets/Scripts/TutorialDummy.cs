using System.Collections;
using UnityEngine;

/// <summary>
/// 튜토리얼에서 유저의 연습을 도와주는 샌드백(더미) 봇입니다.
/// 슈팅 패턴(사격) 및 공격 전조 연출만 담당하며, 체력과 피격 판정은 Enemy 컴포넌트에 위임합니다.
/// </summary>
public class TutorialDummy : MonoBehaviour
{
    [Header("더미 모드 설정")]
    public bool isShooterMode = true;   // 켜두면 플레이어에게 규칙적으로 총알을 쏩니다 (회피 연습용)

    [Header("사격 연습 (회피용)")]
    public GameObject bulletPrefab;     // 발사할 총알 프리팹
    public float shootInterval = 3.0f;  // 총알 쏘는 간격
    public float bulletSpeed = 5.0f;    // 튜토리얼용이므로 피하기 쉽게 속도 조절 가능

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine shootCoroutine;
    private bool isPlayerInRange = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public void SetPlayerInRange(bool inRange)
    {
        isPlayerInRange = inRange;

        if (isPlayerInRange)
        {
            if (isShooterMode && bulletPrefab != null && shootCoroutine == null)
            {
                shootCoroutine = StartCoroutine(ShootRoutine());
            }
        }
        else
        {
            if (shootCoroutine != null)
            {
                StopCoroutine(shootCoroutine);
                shootCoroutine = null;
            }
        }
    }

    void OnDisable()
    {
        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
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
                
                Animator anim = GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Attack");
                }

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
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
}

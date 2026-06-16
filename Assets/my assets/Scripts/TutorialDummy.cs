using System.Collections;
using UnityEngine;

/// <summary>
/// 튜토리얼 씬에서 활성화되는 즉시 플레이어를 조준해 
/// 주기적으로 부채꼴 총알을 쏘는 심플한 더미봇 AI입니다.
/// </summary>
public class TutorialDummy : MonoBehaviour
{
    [Header("사격 설정")]
    public GameObject bulletPrefab;     // 발사할 총알 프리팹
    public float shootInterval = 3.0f;  // 총알 발사 주기
    public float bulletSpeed = 5.0f;    // 총알 날아가는 속도

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine shootCoroutine;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 활성화되는 즉시 사격 코루틴을 시작합니다.
        if (bulletPrefab != null)
        {
            shootCoroutine = StartCoroutine(ShootRoutine());
        }
    }

    private IEnumerator ShootRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(shootInterval);

            if (Player.instance != null && bulletPrefab != null)
            {
                // 공격 애니메이션 실행
                Animator anim = GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Attack");
                }

                // 공격 전조 깜빡임 연출
                if (spriteRenderer != null)
                {
                    StartCoroutine(FlashRoutine());
                }

                // 플레이어 조준 부채꼴 6발 사격
                Vector3 targetDir = (Player.instance.transform.position - transform.position).normalized;
                float startAngle = -22.5f; // 부채꼴 시작 각도 (45도의 절반)
                float angleStep = 9.0f;    // 각도 간격

                float visualOffset = 0f;
                DirectMoving prefabDm = bulletPrefab.GetComponent<DirectMoving>();
                if (prefabDm != null)
                {
                    visualOffset = prefabDm.visualAngleOffset;
                }

                for (int i = 0; i < 6; i++)
                {
                    float angle = startAngle + (i * angleStep);
                    Vector3 bulletDir = Quaternion.Euler(0, 0, angle) * targetDir;
                    
                    float targetAngleDeg = Mathf.Atan2(bulletDir.y, bulletDir.x) * Mathf.Rad2Deg;
                    float rotZ = targetAngleDeg - 90f + visualOffset;
                    Quaternion rotation = Quaternion.Euler(0, 0, rotZ);
                    
                    GameObject bullet = Instantiate(bulletPrefab, transform.position, rotation);
                    
                    // 총알 사격 각도 고정 및 속도 설정
                    DirectMoving bulletDm = bullet.GetComponent<DirectMoving>();
                    if (bulletDm != null)
                    {
                        bulletDm.aimAtPlayerOnStart = false;
                        bulletDm.speed = bulletSpeed;
                    }

                    Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.velocity = bulletDir * bulletSpeed;
                    }
                }
            }
        }
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = originalColor;
    }

    private void OnDisable()
    {
        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
        }
    }
}

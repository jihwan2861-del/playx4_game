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

    [Header("체력 및 사망 설정")]
    [Tooltip("더미봇의 체력")]
    public int maxHealth = 10;
    private int currentHealth;
    private bool isDead = false;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine shootCoroutine;
    private int nextPatternIndex = 0;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        // 🤖 꺼졌다 다시 켜질 때(활성화 시)마다 상태 리셋 및 사격 코루틴을 완벽하게 재시작합니다.
        isDead = false;
        currentHealth = maxHealth;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;

        if (bulletPrefab != null)
        {
            if (shootCoroutine != null)
            {
                StopCoroutine(shootCoroutine);
            }
            shootCoroutine = StartCoroutine(ShootRoutine());
        }
    }

    /// <summary>
    /// 플레이어 총알에 피격되었을 때 호출되어 데미지를 입고 사망 여부를 체크합니다.
    /// </summary>
    public void GetDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"🤖 [TutorialDummy] 피격됨! 체력: {currentHealth}/{maxHealth}");

        // 빨갛게 깜빡이는 피격 피드백 연출
        if (spriteRenderer != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("💀 [TutorialDummy] 격파됨! 사망 연출을 시작합니다.");

        // 사격 정지
        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
        }

        // 충돌체 및 외형 끔
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // 🌟 [개선] 씬 전환 코루틴을 항상 켜져 있는 TutorialController에게 위임해 실행
        // (더미봇 오브젝트가 비활성화되더라도 코루틴이 꼬여서 정지하지 않게 보장합니다.)
        if (TutorialController.instance != null && 
            TutorialController.instance.gameObject.activeInHierarchy && 
            TutorialController.instance.enabled)
        {
            TutorialController.instance.StartCoroutine(TutorialController.instance.TutorialClearRoutine());
        }
        else
        {
            Debug.LogWarning("⚠️ [TutorialDummy] TutorialController를 통해 코루틴을 실행할 수 없는 상태입니다. 즉시 허브 씬으로 이동합니다.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Hub_Scene");
        }

        // 더미봇 자신은 안전하게 꺼둡니다.
        gameObject.SetActive(false);
    }

    private IEnumerator ShootRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(shootInterval);

            if (Player.instance != null && bulletPrefab != null)
            {
                // 🌟 플레이어가 패럴만으로 바뀌었는지 여부 감지
                bool isParalman = false;
                if (TutorialController.instance != null)
                {
                    isParalman = TutorialController.instance.isParalman;
                }

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

                if (!isParalman)
                {
                    // 1) 패럴만 변신 전: 기존 기본 부채꼴 사격
                    ShootSpreadPattern(6, 9.0f, -22.5f);
                }
                else
                {
                    // 2) 패럴만 변신 후: 다채로운 3종 패턴 순환 발사
                    switch (nextPatternIndex)
                    {
                        case 0:
                            // 패턴 A: 360도 전방향 방사 사격 (12발)
                            ShootRadialPattern(12);
                            nextPatternIndex = 1;
                            break;
                        case 1:
                            // 패턴 B: 플레이어 조준 3연속 고속 사격
                            yield return StartCoroutine(ShootTripleTargetedRoutine());
                            nextPatternIndex = 2;
                            break;
                        case 2:
                            // 패턴 C: 교차 부채꼴 엇갈려 2연사
                            yield return StartCoroutine(ShootDoubleSpreadRoutine());
                            nextPatternIndex = 0;
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 플레이어 조준선 기준으로 부채꼴 확산 탄을 발사합니다.
    /// </summary>
    private void ShootSpreadPattern(int bulletCount, float angleStep, float startAngleOffset)
    {
        if (Player.instance == null || bulletPrefab == null) return;

        Vector3 targetDir = (Player.instance.transform.position - transform.position).normalized;
        float startAngle = startAngleOffset;

        float visualOffset = 0f;
        DirectMoving prefabDm = bulletPrefab.GetComponent<DirectMoving>();
        if (prefabDm != null)
        {
            visualOffset = prefabDm.visualAngleOffset;
        }

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = startAngle + (i * angleStep);
            Vector3 bulletDir = Quaternion.Euler(0, 0, angle) * targetDir;
            
            float targetAngleDeg = Mathf.Atan2(bulletDir.y, bulletDir.x) * Mathf.Rad2Deg;
            float rotZ = targetAngleDeg - 90f + visualOffset;
            Quaternion rotation = Quaternion.Euler(0, 0, rotZ);
            
            GameObject bullet = Instantiate(bulletPrefab, transform.position, rotation);
            
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

    /// <summary>
    /// 더미봇 주변으로 360도 전방향 방사형 사격을 수행합니다.
    /// </summary>
    private void ShootRadialPattern(int bulletCount)
    {
        if (bulletPrefab == null) return;

        float angleStep = 360f / bulletCount;
        float visualOffset = 0f;
        DirectMoving prefabDm = bulletPrefab.GetComponent<DirectMoving>();
        if (prefabDm != null)
        {
            visualOffset = prefabDm.visualAngleOffset;
        }

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep;
            Vector3 bulletDir = Quaternion.Euler(0, 0, angle) * Vector3.up;
            
            float targetAngleDeg = Mathf.Atan2(bulletDir.y, bulletDir.x) * Mathf.Rad2Deg;
            float rotZ = targetAngleDeg - 90f + visualOffset;
            Quaternion rotation = Quaternion.Euler(0, 0, rotZ);
            
            GameObject bullet = Instantiate(bulletPrefab, transform.position, rotation);
            
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
        Debug.Log("🤖 [TutorialDummy] 패턴 A 발사: 360도 전방향 12발 방사!");
    }

    /// <summary>
    /// 플레이어의 현재 위치를 타겟팅하여 빠르게 3점사를 쏩니다.
    /// </summary>
    private IEnumerator ShootTripleTargetedRoutine()
    {
        float visualOffset = 0f;
        DirectMoving prefabDm = bulletPrefab.GetComponent<DirectMoving>();
        if (prefabDm != null)
        {
            visualOffset = prefabDm.visualAngleOffset;
        }

        for (int shot = 0; shot < 3; shot++)
        {
            if (Player.instance == null || bulletPrefab == null) yield break;

            Vector3 targetDir = (Player.instance.transform.position - transform.position).normalized;
            float targetAngleDeg = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
            float rotZ = targetAngleDeg - 90f + visualOffset;
            Quaternion rotation = Quaternion.Euler(0, 0, rotZ);

            GameObject bullet = Instantiate(bulletPrefab, transform.position, rotation);

            DirectMoving bulletDm = bullet.GetComponent<DirectMoving>();
            if (bulletDm != null)
            {
                bulletDm.aimAtPlayerOnStart = false;
                bulletDm.speed = bulletSpeed * 1.3f; // 3점사는 날아오는 탄막 속도를 약간 더 빠르게 시프트
            }

            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = targetDir * (bulletSpeed * 1.3f);
            }

            yield return new WaitForSeconds(0.25f);
        }
        Debug.Log("🤖 [TutorialDummy] 패턴 B 발사: 플레이어 조준 고속 3점사!");
    }

    /// <summary>
    /// 기존 부채꼴 사격을 시간차를 두고 엇갈려 두 번 발사합니다.
    /// </summary>
    private IEnumerator ShootDoubleSpreadRoutine()
    {
        // 첫 번째 일반 부채꼴
        ShootSpreadPattern(6, 9.0f, -22.5f);
        
        yield return new WaitForSeconds(0.4f);

        // 두 번째 교차 부채꼴 (각도를 엇갈려 발사)
        ShootSpreadPattern(6, 9.0f, -18.0f);
        Debug.Log("🤖 [TutorialDummy] 패턴 C 발사: 교차 부채꼴 엇갈려 2연사!");
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

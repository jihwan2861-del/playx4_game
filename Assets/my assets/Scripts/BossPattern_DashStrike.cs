using System.Collections;
using UnityEngine;

[System.Serializable]
public class Vector3UnityEvent : UnityEngine.Events.UnityEvent<Vector3> { }

/// <summary>
/// 보스 3 전용 대시 및 패턴 발사 전담 스크립트입니다.
/// 코드를 극도로 단순화하여 오직 대시(이동) 기능과 도착 후 패턴 발사(이벤트/슬롯) 역할만 수행합니다.
/// </summary>
public class BossPattern_DashStrike : MonoBehaviour
{
    [Header("대시 및 연출 설정")]
    [Tooltip("대시 발동 주기 (초)")]
    public float dashCooldown = 5.0f;
    [Tooltip("대시 돌진 시간 (초)")]
    public float dashDuration = 0.4f;
    [Tooltip("대시 직전 충전/예고 시간 (초)")]
    public float warnDuration = 0.5f;
    [Tooltip("도착 후 사격 직후 경직/후딜레이 시간 (초)")]
    public float stunDuration = 0.4f;
    [Tooltip("최대 대시 돌진 제한 거리 (0 이하면 무제한)")]
    public float maxDashDistance = 12.0f;

    [Header("대시 후 발사 패턴 슬롯 공간")]
    [Tooltip("대시 완료 후 이 위치에 인스턴스화할 패턴 프리팹 공간입니다.")]
    public GameObject arrivalPatternPrefab;
    [Tooltip("대시 완료 후 실행할 UnityEvent입니다.")]
    public UnityEngine.Events.UnityEvent onDashArrival;
    [Tooltip("대시 완료 후 실행할 위치 전달용 UnityEvent입니다.")]
    public Vector3UnityEvent onDashArrivalWithPosition;
    [Tooltip("아무런 커스텀 패턴을 등록하지 않았을 경우 기본 360도 탄막을 폴백 사격할지 여부")]
    public bool useDefault360AsFallback = true;

    [Header("기본 폴백 360도 탄막 설정")]
    public int bulletCount360 = 24;
    public float bulletSpeed = 6.0f;
    public GameObject bulletPrefab;

    [Header("효과음 설정")]
    public AudioClip dashSFX;

    private BossMovement bossMovement;
    private EnemySmartAI enemySmartAI;
    private SpriteRenderer spriteRenderer;
    private Coroutine dashRoutine;

    void Start()
    {
        // 핵심 컴포넌트 캐싱
        bossMovement = GetComponent<BossMovement>();
        if (bossMovement == null) bossMovement = GetComponentInChildren<BossMovement>();

        enemySmartAI = GetComponent<EnemySmartAI>();
        if (enemySmartAI == null) enemySmartAI = GetComponentInChildren<EnemySmartAI>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 패턴 루프 기동
        dashRoutine = StartCoroutine(DashStrikeLoopRoutine());
    }

    void OnDestroy()
    {
        if (dashRoutine != null) StopCoroutine(dashRoutine);
    }

    private IEnumerator DashStrikeLoopRoutine()
    {
        yield return new WaitForSeconds(2.0f); // 등장 후 초기 대기

        while (true)
        {
            // 플레이어 및 활성화 여부 대기
            if (Player.instance == null || (LevelController.instance != null && !LevelController.instance.isFrenzyPhase))
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 1. 대시 주기 대기
            yield return new WaitForSeconds(dashCooldown);

            if (Player.instance == null) continue;

            // 2. 대시 준비 단계 (일반 이동 컴포넌트 일시 정지)
            if (bossMovement != null) bossMovement.enabled = false;
            if (enemySmartAI != null) enemySmartAI.enabled = false;

            // 목표 좌표 설정 및 거리 클램프
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = Player.instance.transform.position;
            if (maxDashDistance > 0f)
            {
                Vector3 toPlayer = targetPosition - startPosition;
                if (toPlayer.magnitude > maxDashDistance)
                {
                    targetPosition = startPosition + toPlayer.normalized * maxDashDistance;
                }
            }

            // 예고 깜빡임 연출
            Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            float elapsedWarn = 0f;
            while (elapsedWarn < warnDuration)
            {
                elapsedWarn += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = (Mathf.Repeat(elapsedWarn * 12f, 1f) > 0.5f) ? new Color(0f, 0.75f, 1f, 1f) : originalColor;
                }
                yield return null;
            }
            if (spriteRenderer != null) spriteRenderer.color = originalColor;

            // 3. 대시 실행 (Ease-Out Lerp)
            if (dashSFX != null) AudioSource.PlayClipAtPoint(dashSFX, transform.position);

            float elapsedDash = 0f;
            while (elapsedDash < dashDuration)
            {
                elapsedDash += Time.deltaTime;
                float progress = elapsedDash / dashDuration;
                float easedProgress = Mathf.Sin(progress * Mathf.PI * 0.5f); // Ease-Out 보간
                transform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress);
                yield return null;
            }
            transform.position = targetPosition;

            // 4. 도착 후 즉시 패턴 발사 슬롯 실행
            ExecuteArrivalPattern(targetPosition);

            // 5. 후딜레이 경직 시간
            yield return new WaitForSeconds(stunDuration);

            // 6. 컴포넌트 원복 후 다음 사이클 진입
            if (bossMovement != null) bossMovement.enabled = true;
            if (enemySmartAI != null) enemySmartAI.enabled = true;
        }
    }

    /// <summary>
    /// 지정된 안착 위치에 등록된 커스텀 탄막 프리팹을 소환하거나 등록된 이벤트를 실행합니다.
    /// </summary>
    public void ExecuteArrivalPattern(Vector3 position)
    {
        bool hasCustomEvent = (onDashArrival != null && onDashArrival.GetPersistentEventCount() > 0) ||
                               (onDashArrivalWithPosition != null && onDashArrivalWithPosition.GetPersistentEventCount() > 0);
        bool hasCustomPrefab = (arrivalPatternPrefab != null);

        // 1. 프리팹 슬롯 소환
        if (hasCustomPrefab)
        {
            Instantiate(arrivalPatternPrefab, position, Quaternion.identity);
        }

        // 2. 이벤트 트리거
        if (onDashArrival != null) onDashArrival.Invoke();
        if (onDashArrivalWithPosition != null) onDashArrivalWithPosition.Invoke(position);

        // 3. 아무 매핑도 없을 때 예외 방지용 기본 360도 탄막 구동
        if (!hasCustomPrefab && !hasCustomEvent && useDefault360AsFallback)
        {
            Fire360BulletRing();
        }
    }

    /// <summary>
    /// 도착한 지점에서 사방 360도 전방향으로 탄환을 일제 사격합니다. (UnityEvent에서 매핑하여 재사용 가능)
    /// </summary>
    public void Fire360BulletRing()
    {
        EnsureBulletPrefab();
        if (bulletPrefab == null) return;

        float angleStep = 360f / bulletCount360;
        for (int i = 0; i < bulletCount360; i++)
        {
            float angle = i * angleStep;
            SpawnBullet(angle, bulletSpeed);
        }

        if (BloomController.instance != null)
        {
            BloomController.instance.DoBloom(3.5f, 0.25f);
        }
    }

    private void EnsureBulletPrefab()
    {
        if (bulletPrefab != null) return;

        var bulletPatternCtrl = GetComponent<BossBulletPatternController>();
        if (bulletPatternCtrl == null) bulletPatternCtrl = GetComponentInChildren<BossBulletPatternController>();

        if (bulletPatternCtrl != null && bulletPatternCtrl.baseProjectilePrefab != null)
        {
            bulletPrefab = bulletPatternCtrl.baseProjectilePrefab;
        }
        else
        {
            var shooter = FindObjectOfType<PatternShooter>();
            if (shooter != null) bulletPrefab = shooter.projectilePrefab;
        }
    }

    private void SpawnBullet(float angle, float speed)
    {
        GameObject bullet = null;
        if (PoolingController.instance != null)
        {
            bullet = PoolingController.instance.GetPoolingObject(bulletPrefab);
            if (bullet == null) return;
            bullet.transform.position = transform.position;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            bullet.SetActive(true);
        }
        else
        {
            bullet = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0, 0, angle));
        }

        if (bullet != null)
        {
            bullet.tag = "Projectile";
            DirectMoving dm = bullet.GetComponent<DirectMoving>();
            if (dm == null) dm = bullet.AddComponent<DirectMoving>();
            dm.speed = speed;
            dm.isHoming = false;
        }
    }
}

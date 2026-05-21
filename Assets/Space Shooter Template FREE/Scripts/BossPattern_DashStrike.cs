using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 3 전용 시그니처 패턴 스크립트입니다.
/// 평소에는 천천히 플레이어를 추적하다가, 주기적으로 플레이어 위치로 초고속 대시(잔상 효과 동반)를 감행한 후
/// 도착 지점에서 사방 360도 전방향 탄막을 일시에 발사합니다.
/// 
/// [2026-05-22 추가] 체력 50% 미만이 되면:
///  - 분신(무적 홀로그램)을 1회 소환하고
///  - 메인 보스 및 분신 모두 돌진 쿨타임을 0.5초로 단축합니다.
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
    [Tooltip("최대 대시 돌진 제한 거리 (0 이하면 거리 제한 없이 플레이어 위치로 끝까지 돌진)")]
    public float maxDashDistance = 12.0f;

    [Header("잔상(Afterimage) 설정")]
    [Tooltip("잔상 생성 간격 (초)")]
    public float afterimageInterval = 0.04f;
    [Tooltip("잔상 소멸 시간 (초)")]
    public float afterimageFadeDuration = 0.3f;
    [Tooltip("잔상의 네온 컬러")]
    public Color afterimageColor = new Color(0f, 0.75f, 1f, 0.6f); // 푸른 네온 색상

    [Header("360도 탄막 설정")]
    [Tooltip("360도 발사할 탄환 개수")]
    public int bulletCount360 = 24;
    [Tooltip("탄환 속도")]
    public float bulletSpeed = 6.0f;
    [Tooltip("발사할 탄환 프리팹 (비워두면 BossBulletPatternController 또는 씬의 투사체 프리팹에서 자동 탐색)")]
    public GameObject bulletPrefab;

    [Header("분신(Illusion Clone) 설정")]
    [Tooltip("분신 소환 체력 임계값 (0.5 = 50%)")]
    public float cloneHpThreshold = 0.5f;
    [Tooltip("분신 소환 후 돌진 쿨타임 (초)")]
    public float enragedDashCooldown = 0.5f;

    // ── 분신 여부 관련 내부 상태 ──────────────────────────────────────────────
    /// <summary>이 인스턴스가 분신(Clone)인지 여부. 분신은 체력 로직 및 승리 처리를 수행하지 않습니다.</summary>
    [HideInInspector] public bool isClone = false;
    /// <summary>분신이 감시할 메인 보스 레퍼런스. 메인 보스가 소멸하면 분신도 따라 사라집니다.</summary>
    [HideInInspector] public BossPattern_DashStrike mainBoss = null;

    private BossMovement bossMovement;
    private EnemySmartAI enemySmartAI;
    private SpriteRenderer spriteRenderer;
    private Coroutine dashRoutine;
    private bool isDashing = false;

    // 분신 소환 관련 플래그 (메인 보스 전용)
    private bool hasSpawnedClone = false;
    private BossPatternController bossPatternController;

    void Start()
    {
        // 보스의 기본 컴포넌트 자동 캐싱
        bossMovement = GetComponent<BossMovement>();
        if (bossMovement == null) bossMovement = GetComponentInChildren<BossMovement>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 스마트 AI 컴포넌트 캐싱 및 이탈 방지 조치
        enemySmartAI = GetComponent<EnemySmartAI>();
        if (enemySmartAI == null) enemySmartAI = GetComponentInChildren<EnemySmartAI>();

        if (enemySmartAI != null)
        {
            // afterAttackState가 MoveOut(화면 밖 탈출)으로 설정되어 있다면 ChasePlayer(플레이어 추적)로 강제 보정하여 먹통 방지
            if (enemySmartAI.afterAttackState == EnemySmartAI.BehaviorState.MoveOut)
            {
                enemySmartAI.afterAttackState = EnemySmartAI.BehaviorState.ChasePlayer;
                Debug.Log("🛡️ [BossPattern_DashStrike] EnemySmartAI의 afterAttackState가 MoveOut으로 설정되어 있어, 보스의 무한 탈출을 방지하기 위해 ChasePlayer로 자동 보정했습니다.");
            }
        }

        // 체력 감시를 위한 BossPatternController 캐싱 (메인 보스 전용)
        if (!isClone)
        {
            bossPatternController = GetComponent<BossPatternController>();
            if (bossPatternController == null) bossPatternController = GetComponentInChildren<BossPatternController>();
        }

        // 코루틴 가동
        dashRoutine = StartCoroutine(DashStrikeLoopRoutine());
    }

    void Update()
    {
        // ── 분신 전용: 메인 보스가 사라지면 분신도 소멸 ─────────────────────
        if (isClone)
        {
            if (mainBoss == null)
            {
                // 메인 보스가 처치됐으므로 분신도 조용히 소멸
                Destroy(gameObject);
            }
            return; // 분신은 체력 체크 불필요
        }

        // ── 메인 보스 전용: 체력 50% 미만 → 분신 소환 + 쿨타임 단축 ─────────
        if (!hasSpawnedClone && bossPatternController != null)
        {
            float ratio = bossPatternController.currentSurvivalTimer / bossPatternController.bossSurvivalTime;
            if (ratio < cloneHpThreshold && LevelController.instance != null && LevelController.instance.isFrenzyPhase)
            {
                hasSpawnedClone = true;
                // 돌진 쿨타임 즉시 단축
                dashCooldown = enragedDashCooldown;
                Debug.Log("💥 [BossPattern_DashStrike] 보스 체력 50% 미만! 폭주 돌진 쿨타임 단축 + 분신 소환!");
                SpawnIllusionClone();
            }
        }
    }

    void OnDestroy()
    {
        if (dashRoutine != null) StopCoroutine(dashRoutine);
    }

    /// <summary>
    /// 체력 50% 돌파 시 분신을 화면 반대편에 소환합니다.
    /// 분신은 BossPatternController / BossHacking / Enemy 컴포넌트가 제거된
    /// 무적 홀로그램으로, 플레이어 탄환을 흡수하며 독자적으로 돌진-사격 패턴을 수행합니다.
    /// </summary>
    private void SpawnIllusionClone()
    {
        // 메인 보스 위치 기준으로 반대편 오프셋에 안전 스폰
        Vector3 spawnOffset = new Vector3(-transform.position.x * 0.5f, 0f, 0f);
        // 화면 내부로 클램핑 (대략 ±4.5 범위 내)
        float cloneX = Mathf.Clamp(transform.position.x + (transform.position.x > 0 ? -3.5f : 3.5f), -4.5f, 4.5f);
        float cloneY = Mathf.Clamp(transform.position.y, -5f, 5f);
        Vector3 clonePos = new Vector3(cloneX, cloneY, transform.position.z);

        // 메인 보스 오브젝트를 통째로 복제
        GameObject cloneObj = Instantiate(gameObject, clonePos, transform.rotation);
        cloneObj.name = gameObject.name + "_IllusionClone";

        // ── 분신에서 불필요한 게임 로직 컴포넌트 즉시 제거 ──────────────────
        // BossPatternController가 있으면 제거 (체력바 / 승리 신호 중복 방지)
        BossPatternController clonePatternCtrl = cloneObj.GetComponent<BossPatternController>();
        if (clonePatternCtrl != null) Destroy(clonePatternCtrl);

        // BossHacking 제거 (해킹 상호작용 중복 방지)
        BossHacking cloneHacking = cloneObj.GetComponent<BossHacking>();
        if (cloneHacking != null) Destroy(cloneHacking);

        // Enemy 컴포넌트 제거 → 분신은 무적(데미지 불가)
        Enemy cloneEnemy = cloneObj.GetComponent<Enemy>();
        if (cloneEnemy != null) Destroy(cloneEnemy);

        // BossBulletPatternController 제거 (나선형 탄막 중복 방지)
        BossBulletPatternController cloneBulletCtrl = cloneObj.GetComponent<BossBulletPatternController>();
        if (cloneBulletCtrl != null) Destroy(cloneBulletCtrl);

        // PatternShooter 제거 (중복 발사 방지)
        PatternShooter cloneShooter = cloneObj.GetComponent<PatternShooter>();
        if (cloneShooter != null) Destroy(cloneShooter);

        // ── 분신 DashStrike 컴포넌트 설정 ──────────────────────────────────
        BossPattern_DashStrike cloneDash = cloneObj.GetComponent<BossPattern_DashStrike>();
        if (cloneDash != null)
        {
            cloneDash.isClone = true;
            cloneDash.mainBoss = this;
            cloneDash.dashCooldown = enragedDashCooldown;
            // 분신 잔상은 강렬한 분홍 네온(Magenta)으로 시각적 차별화
            cloneDash.afterimageColor = new Color(1f, 0f, 0.85f, 0.7f);
            // 분신은 체력 기반 재분신 없음
            cloneDash.hasSpawnedClone = true;
        }

        // ── 분신 비주얼: 반투명 민트 홀로그램 컬러 적용 ──────────────────────
        SpriteRenderer[] cloneRenderers = cloneObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in cloneRenderers)
        {
            if (sr != null)
            {
                // 반투명 민트 홀로그램 (알파 0.45)
                sr.color = new Color(0f, 1f, 0.9f, 0.45f);
            }
        }

        Debug.Log($"👥 [BossPattern_DashStrike] 분신 소환 완료! 위치: {clonePos}");
    }

    /// <summary>
    /// 대시 및 탄막 사격을 반복하는 메인 패턴 코루틴입니다.
    /// </summary>
    private IEnumerator DashStrikeLoopRoutine()
    {
        // 최초 보스 생성 후 일정 지연을 두어 자연스러운 시작 유도
        yield return new WaitForSeconds(2.0f);

        while (true)
        {
            // 폭주 모드(보스전 활성화)일 때만 작동하도록 레벨 컨트롤러 체크
            if (LevelController.instance == null || !LevelController.instance.isFrenzyPhase)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 분신일 경우 메인 보스가 사라졌는지 추가 확인
            if (isClone && mainBoss == null)
            {
                yield break;
            }

            // 플레이어가 없는 경우 대기
            if (Player.instance == null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 1. 대시 쿨타임 대기 (평상시: 보스가 천천히 플레이어를 졸졸 쫓아감)
            yield return new WaitForSeconds(dashCooldown);

            if (Player.instance == null) continue;
            if (isClone && mainBoss == null) yield break;

            // 2. 대시 예고 (Warning / Charge Phase)
            isDashing = true;
            if (bossMovement != null)
            {
                bossMovement.enabled = false; // 기본 추적 추격 스크립트 비활성화
            }
            if (enemySmartAI != null)
            {
                enemySmartAI.enabled = false; // 스마트 AI 컴포넌트도 비활성화하여 충전/대시 중 이동 간섭 방지
            }

            // 플레이어 조준 타겟 좌표 잠금 (Target Lock) 및 돌진 제한 거리 계산
            Vector3 targetPosition = Player.instance.transform.position;
            if (maxDashDistance > 0f)
            {
                Vector3 toPlayer = targetPosition - transform.position;
                float distance = toPlayer.magnitude;
                if (distance > maxDashDistance)
                {
                    // 최대 돌진 가능 거리만큼만 잘라서 목표 지점 설정
                    targetPosition = transform.position + toPlayer.normalized * maxDashDistance;
                }
            }

            // 충전 연출: 보스 몸체를 푸른 네온 색상으로 빠르게 깜빡임 및 살짝 스케일 확대
            Vector3 originalScale = transform.localScale;
            Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            float elapsedWarn = 0f;
            while (elapsedWarn < warnDuration)
            {
                elapsedWarn += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    // 빠른 깜빡임 연출
                    if (Mathf.Repeat(elapsedWarn * 12f, 1f) > 0.5f)
                    {
                        spriteRenderer.color = afterimageColor;
                    }
                    else
                    {
                        spriteRenderer.color = originalColor;
                    }
                }
                // 살짝 기를 모으는 진동 스케일링
                transform.localScale = originalScale * (1f + Mathf.Sin(elapsedWarn * 40f) * 0.05f);
                yield return null;
            }

            // 색상 및 크기 원래대로 복구
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            transform.localScale = originalScale;

            // 3. 초고속 대시 이동 (잔상 생성 동반)
            Vector3 startPosition = transform.position;
            float elapsedDash = 0f;
            float afterimageTimer = 0f;

            // 대시 방향에 맞춰 보스의 바라보는 방향(스프라이트) 설정
            Vector3 dashDirection = (targetPosition - startPosition).normalized;
            UpdateSpriteDirection(dashDirection);

            while (elapsedDash < dashDuration)
            {
                elapsedDash += Time.deltaTime;
                float progress = elapsedDash / dashDuration;

                // 스무스한 돌진을 위해 Ease-Out Lerp 적용
                float easedProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);
                transform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress);

                // 주기적인 잔상(Ghost Trail) 생성
                afterimageTimer += Time.deltaTime;
                if (afterimageTimer >= afterimageInterval)
                {
                    afterimageTimer = 0f;
                    SpawnAfterimage();
                }

                yield return null;
            }

            // 목표 지점에 완전히 안착
            transform.position = targetPosition;

            // 4. 도착 지점 즉시 360도 전방향 탄막 발사!
            Fire360BulletRing();

            // 5. 발사 후 짧은 후딜레이 경직 (보스의 위압감 및 플레이어 반응 기회 제공)
            yield return new WaitForSeconds(stunDuration);

            // 6. 상태 초기화 및 일반 느린 추적 상태 복귀
            isDashing = false;
            if (bossMovement != null)
            {
                bossMovement.enabled = true; // 기본 추적 재시작
            }
            if (enemySmartAI != null)
            {
                enemySmartAI.enabled = true; // 스마트 AI 컴포넌트 복구
            }
        }
    }

    /// <summary>
    /// 대시 방향에 따라 보스 스프라이트 이미지를 교체해 줍니다.
    /// </summary>
    private void UpdateSpriteDirection(Vector3 direction)
    {
        if (bossMovement == null || spriteRenderer == null || direction == Vector3.zero) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bool use8Direction = (bossMovement.spriteLeft != null || bossMovement.spriteRight != null);

        if (use8Direction)
        {
            if (angle >= 67.5f && angle < 112.5f)
            {
                if (bossMovement.spriteBack != null) spriteRenderer.sprite = bossMovement.spriteBack;
            }
            else if (angle >= -112.5f && angle < -67.5f)
            {
                if (bossMovement.spriteFront != null) spriteRenderer.sprite = bossMovement.spriteFront;
            }
            else if (angle >= 22.5f && angle < 67.5f)
            {
                if (bossMovement.spriteUpRight != null) spriteRenderer.sprite = bossMovement.spriteUpRight;
            }
            else if (angle >= -67.5f && angle < -22.5f)
            {
                if (bossMovement.spriteDownRight != null) spriteRenderer.sprite = bossMovement.spriteDownRight;
            }
            else if (angle >= 112.5f && angle < 157.5f)
            {
                if (bossMovement.spriteUpLeft != null) spriteRenderer.sprite = bossMovement.spriteUpLeft;
            }
            else if (angle >= -157.5f && angle < -112.5f)
            {
                if (bossMovement.spriteDownLeft != null) spriteRenderer.sprite = bossMovement.spriteDownLeft;
            }
            else if (angle >= -22.5f && angle < 22.5f)
            {
                if (bossMovement.spriteRight != null) spriteRenderer.sprite = bossMovement.spriteRight;
                else if (bossMovement.spriteUpRight != null) spriteRenderer.sprite = bossMovement.spriteUpRight;
            }
            else
            {
                if (bossMovement.spriteLeft != null) spriteRenderer.sprite = bossMovement.spriteLeft;
                else if (bossMovement.spriteUpLeft != null) spriteRenderer.sprite = bossMovement.spriteUpLeft;
            }
        }
        else
        {
            if (angle >= 67.5f && angle < 112.5f)
            {
                if (bossMovement.spriteBack != null) spriteRenderer.sprite = bossMovement.spriteBack;
            }
            else if (angle >= -112.5f && angle < -67.5f)
            {
                if (bossMovement.spriteFront != null) spriteRenderer.sprite = bossMovement.spriteFront;
            }
            else if (angle >= 0f && angle < 67.5f)
            {
                if (bossMovement.spriteUpRight != null) spriteRenderer.sprite = bossMovement.spriteUpRight;
            }
            else if (angle >= -67.5f && angle < 0f)
            {
                if (bossMovement.spriteDownRight != null) spriteRenderer.sprite = bossMovement.spriteDownRight;
            }
            else if (angle >= 112.5f && angle <= 180f || angle >= -180f && angle < -157.5f)
            {
                if (bossMovement.spriteUpLeft != null) spriteRenderer.sprite = bossMovement.spriteUpLeft;
            }
            else
            {
                if (bossMovement.spriteDownLeft != null) spriteRenderer.sprite = bossMovement.spriteDownLeft;
            }
        }
    }

    /// <summary>
    /// 보스의 현재 Sprite와 Transform 속성을 기반으로 잔상을 동적 생성합니다.
    /// </summary>
    private void SpawnAfterimage()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        GameObject afterimageObj = new GameObject("BossDashAfterimage");
        afterimageObj.transform.position = transform.position;

        BossAfterimage afterimage = afterimageObj.AddComponent<BossAfterimage>();
        
        // 보스의 현재 스케일, 회전, 스프라이트, 정렬 순서를 잔상 스크립트에 고스란히 복제 주입
        afterimage.Initialize(
            spriteRenderer.sprite,
            afterimageColor,
            afterimageFadeDuration,
            transform.localScale,
            transform.rotation,
            spriteRenderer.flipX,
            spriteRenderer.flipY,
            spriteRenderer.sortingOrder
        );
    }

    /// <summary>
    /// 도착한 지점에서 사방 360도 전방향으로 탄환을 일제 사격합니다.
    /// </summary>
    private void Fire360BulletRing()
    {
        EnsureBulletPrefab();
        if (bulletPrefab == null)
        {
            Debug.LogError("⚠️ [BossPattern_DashStrike] 발사할 탄환 프리팹(bulletPrefab)을 찾을 수 없어 360도 탄막을 발사하지 못했습니다!");
            return;
        }

        float angleStep = 360f / bulletCount360;

        for (int i = 0; i < bulletCount360; i++)
        {
            float angle = i * angleStep;
            SpawnBullet(angle, bulletSpeed);
        }

        // 화려한 뿜어내기 연출을 위해 Bloom 효과 가동 (존재할 경우)
        if (BloomController.instance != null)
        {
            BloomController.instance.DoBloom(3.5f, 0.25f);
        }
    }

    /// <summary>
    /// 탄환 프리팹을 확보합니다.
    /// </summary>
    private void EnsureBulletPrefab()
    {
        if (bulletPrefab != null)
        {
            try { var _ = bulletPrefab.name; }
            catch { bulletPrefab = null; }
        }

        if (bulletPrefab == null)
        {
            // 1. BossBulletPatternController에 캐싱된 프리팹 조회
            var bulletPatternCtrl = GetComponent<BossBulletPatternController>();
            if (bulletPatternCtrl == null) bulletPatternCtrl = GetComponentInChildren<BossBulletPatternController>();

            if (bulletPatternCtrl != null && bulletPatternCtrl.baseProjectilePrefab != null)
            {
                bulletPrefab = bulletPatternCtrl.baseProjectilePrefab;
            }
            else
            {
                // 2. 씬 내의 PatternShooter에서 투사체 프리팹 강제 역추적
                var shooter = FindObjectOfType<PatternShooter>();
                if (shooter != null)
                {
                    bulletPrefab = shooter.projectilePrefab;
                }
            }
        }
    }

    /// <summary>
    /// 오브젝트 풀링 또는 일반 인스턴스화를 지원하여 지정된 각도와 속도로 탄환을 발사합니다.
    /// </summary>
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
            dm.isHoming = false; // 대시 발사 탄막은 타겟 추적이 아닌 직진형 기하학 형태로 사출
        }
    }
}

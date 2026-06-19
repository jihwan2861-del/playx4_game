using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스가 플레이어를 서서히 쫓아가거나, 특정 경계 내를 돌진(Dash) 순간이동하고 도착지에서 360도 탄막을 발사하는 복합 패턴 스크립트입니다.
/// </summary>
public class BossMovement : MonoBehaviour
{
    public enum BossState { Idle, Dashing, Attacking }

    [Header("=== 이동 패턴 모드 ===")]
    [Tooltip("체크 시 신규 순간이동(돌진) + 360도 탄막 발사 패턴을 사용합니다. 해제 시 기존 플레이어 추격 모드로 동작합니다.")]
    public bool usePatternMovement = true;

    [Header("=== 신규 패턴 설정 ===")]
    [Tooltip("돌진 후 제자리 대기 시간 (초)")]
    public float idleTime = 2.0f;
    [Tooltip("돌진 이동 속도")]
    public float dashSpeed = 15.0f;
    [Tooltip("돌진 도착 후 발사 전 대기 시간 (초)")]
    public float attackDelay = 0.3f;
    [Tooltip("발사 후 제자리에 멈춰 서 있는 시간 (초)")]
    public float attackDuration = 1.0f;

    [Header("=== 360도 탄막 발사 설정 ===")]
    [Tooltip("발사할 총알 프리팹 (DirectMoving 포함 필수)")]
    public GameObject bulletPrefab;
    [Tooltip("360도 탄막 발사 갈래 개수")]
    public int radialBulletCount = 12;

    [Header("=== 잔상 효과 설정 ===")]
    [Tooltip("잔상 생성 간격 (초, 값이 작을수록 촘촘하게 생성)")]
    public float afterimageInterval = 0.05f;
    [Tooltip("잔상 페이드아웃 지속 시간 (초)")]
    public float afterimageFadeDuration = 0.4f;
    [Tooltip("잔상 네온 컬러")]
    public Color afterimageColor = new Color(0f, 1f, 1f, 0.6f); // 하늘색 네온 컬러

    [Header("기존 추격 설정 (usePatternMovement 비활성화 시 사용)")]
    public float moveSpeed = 2.0f;        // 보스가 쫓아오는 속도
    public bool isChasingPlayer = true;   // 플레이어를 쫓아갈지 여부
    public float stopDistance = 3.0f;     // 플레이어와 이 거리보다 가까워지면 멈춤 (Idle)

    [Header("애니메이션 설정")]
    public Animator bossAnimator;         // 보스의 애니메이터
    [Tooltip("이동 중일 때 Animator에서 켤 Bool 파라미터 이름 (예: isRunning)")]
    public string runBoolName = "isRunning"; 

    [Header("=== 페이즈 2 설정 ===")]
    [Tooltip("페이즈 2 진입 시 돌진 후 대기 시간 (초)")]
    public float phase2IdleTime = 0.8f;
    [Tooltip("페이즈 2 진입 시 돌진 이동 속도")]
    public float phase2DashSpeed = 22.0f;

    [Header("=== 디버그 확인용 상태 ===")]
    public BossState currentState = BossState.Idle;

    private SpriteRenderer spriteRenderer;
    private Player playerTarget;

    // 애니메이터 파라미터 존재 여부 캐싱
    private bool hasRunParameter = false;
    private bool hasDirXParameter = false;
    private bool hasDirYParameter = false;
    private bool isParameterCached = false;
    private RuntimeAnimatorController lastCachedController = null;

    private void CacheParametersIfNeeded()
    {
        if (bossAnimator == null) return;

        RuntimeAnimatorController currentController = bossAnimator.runtimeAnimatorController;
        if (currentController == null)
        {
            isParameterCached = false;
            lastCachedController = null;
            hasRunParameter = false;
            hasDirXParameter = false;
            hasDirYParameter = false;
            return;
        }

        // 이미 동일한 컨트롤러에 대해 캐싱 완료한 경우 리턴
        if (isParameterCached && lastCachedController == currentController)
        {
            return;
        }

        hasRunParameter = false;
        hasDirXParameter = false;
        hasDirYParameter = false;

        foreach (AnimatorControllerParameter param in bossAnimator.parameters)
        {
            if (param.name == runBoolName && param.type == AnimatorControllerParameterType.Bool)
            {
                hasRunParameter = true;
            }
            if (param.name == "dirX" && param.type == AnimatorControllerParameterType.Float)
            {
                hasDirXParameter = true;
            }
            if (param.name == "dirY" && param.type == AnimatorControllerParameterType.Float)
            {
                hasDirYParameter = true;
            }
        }

        lastCachedController = currentController;
        isParameterCached = true;
        
        Debug.Log($"[BossMovement] Animator parameters cached. HasRunParameter({runBoolName}): {hasRunParameter}, HasDirXParameter(dirX): {hasDirXParameter}, HasDirYParameter(dirY): {hasDirYParameter}");
    }

    void Start()
    {
        // BossTotemManager가 없으면 자동으로 부착하여 연동 보장
        if (GetComponent<BossTotemManager>() == null)
        {
            gameObject.AddComponent<BossTotemManager>();
            Debug.Log("🔧 [BossMovement] BossTotemManager 컴포넌트가 없어 자동으로 추가하여 연결을 보장합니다.");
        }

        // 컴포넌트 자동 연결
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (bossAnimator == null)
        {
            bossAnimator = GetComponentInChildren<Animator>();
            if (bossAnimator == null) bossAnimator = GetComponent<Animator>();
        }

        // 애니메이터 파라미터 유효성 검사 및 초기 캐싱
        CacheParametersIfNeeded();

        // 플레이어 찾기
        if (Player.instance != null)
        {
            playerTarget = Player.instance;
        }

        // 패턴 이동 모드인 경우 패턴 루프 시작
        if (usePatternMovement)
        {
            StartCoroutine(BossPatternRoutine());
        }
    }

    /// <summary>
    /// 애니메이터의 달리기 파라미터를 안전하게 제어합니다.
    /// </summary>
    public void SetRunAnimBool(bool value)
    {
        CacheParametersIfNeeded();
        if (bossAnimator != null && hasRunParameter)
        {
            bossAnimator.SetBool(runBoolName, value);
        }
    }

    /// <summary>
    /// 애니메이터의 방향 Float 파라미터들을 안전하게 제어합니다.
    /// </summary>
    public void SetDirAnimFloat(float x, float y)
    {
        CacheParametersIfNeeded();
        if (bossAnimator != null)
        {
            if (hasDirXParameter) bossAnimator.SetFloat("dirX", x);
            if (hasDirYParameter) bossAnimator.SetFloat("dirY", y);
        }
    }

    /// <summary>
    /// 보스의 페이즈 2 상태를 활성화합니다. (돌진 빈도 및 속도 증가)
    /// </summary>
    public void EnterPhase2()
    {
        idleTime = phase2IdleTime;
        dashSpeed = phase2DashSpeed;
        Debug.Log($"🔥 [BossMovement] 보스 페이즈 2 돌입! 돌진 속도: {dashSpeed}, 대기 시간: {idleTime}");
    }

    void Update()
    {
        // 패턴 이동 모드 활성화 시에는 Update의 추적 로직을 건너뜁니다.
        if (usePatternMovement) return;

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

            SetDirAnimFloat(direction.x, direction.y);

            if (distanceToPlayer > stopDistance)
            {
                transform.position += direction * moveSpeed * Time.deltaTime;
                SetRunAnimBool(true);
            }
            else
            {
                SetRunAnimBool(false);
            }
        }
    }

    /// <summary>
    /// 메인 카메라의 경계를 읽어와 화면 바깥으로 보스가 탈출하지 않도록 안전한 랜덤 대상 위치를 계산합니다.
    /// </summary>
    private Vector2 GetRandomScreenPosition()
    {
        Camera cam = Camera.main;
        if (cam == null) return transform.position;

        // 메인 카메라 기준의 화면 경계 계산 (가로세로 여백 15% 감안하여 화면 안에 안전하게 잡기)
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        float limitX = (width / 2f) * 0.7f;
        float limitY = (height / 2f) * 0.7f;

        float randomX = Random.Range(-limitX, limitX) + cam.transform.position.x;
        float randomY = Random.Range(-limitY, limitY) + cam.transform.position.y;

        return new Vector2(randomX, randomY);
    }

    /// <summary>
    /// 씬 내의 활성화된 토템들 중 하나의 위치를 목표 지점으로 선정합니다. 
    /// 토템이 없다면 보스 자신의 현재 위치를 반환하여 돌진을 차단/봉쇄합니다.
    /// </summary>
    private Vector2 GetTargetTotemPosition()
    {
        if (BossTotemManager.instance != null)
        {
            GameObject targetTotem = BossTotemManager.instance.GetRandomActiveTotem();
            if (targetTotem != null)
            {
                Debug.Log($"🎯 [BossMovement] 토템 '{targetTotem.name}' 위치로 돌진합니다: {targetTotem.transform.position}");
                return targetTotem.transform.position;
            }
        }

        // 토템이 하나도 없을 때의 봉쇄 설정 (제자리 머무름)
        Debug.Log("🚫 [BossMovement] 활성화된 토템이 없어 돌진이 봉쇄되었습니다. 제자리에 머뭅니다.");
        return transform.position;
    }

    /// <summary>
    /// 360도 방향으로 radialBulletCount 만큼 총알을 방사형 발사합니다.
    /// </summary>
    private void Fire360Radial()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("⚠️ [BossMovement] 발사할 총알 프리팹(bulletPrefab)이 지정되지 않았습니다. 인스펙터에 등록해 주세요!");
            return;
        }

        float angleStep = 360f / radialBulletCount;
        for (int i = 0; i < radialBulletCount; i++)
        {
            float angle = i * angleStep;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            
            // 보스 위치에서 각 방향으로 날아가도록 총알 생성
            Instantiate(bulletPrefab, transform.position, rotation);
        }
        Debug.Log($"🔥 [BossMovement] {radialBulletCount}발의 360도 탄막을 발사했습니다.");
    }

    /// <summary>
    /// 돌진 시 네온 잔상을 동적으로 하나 스폰합니다.
    /// </summary>
    private void SpawnAfterimage()
    {
        if (spriteRenderer == null) return;

        // 1. 빈 오브젝트를 생성하고 BossAfterimage 컴포넌트를 붙입니다.
        GameObject afterimageObj = new GameObject("BossDashAfterimage");
        afterimageObj.transform.position = transform.position;

        BossAfterimage afterimageComp = afterimageObj.AddComponent<BossAfterimage>();

        // 2. 현재 보스의 SpriteRenderer 상태를 읽어와 초기화해 줍니다.
        Sprite currentSprite = spriteRenderer.sprite;
        if (currentSprite != null)
        {
            afterimageComp.Initialize(
                currentSprite,
                afterimageColor,
                afterimageFadeDuration,
                transform.localScale,
                transform.rotation,
                spriteRenderer.flipX,
                spriteRenderer.flipY,
                spriteRenderer.sortingOrder
            );
        }
    }

    /// <summary>
    /// 보스의 행동 패턴 상태 머신 루틴입니다.
    /// </summary>
    private IEnumerator BossPatternRoutine()
    {
        while (true)
        {
            // 토템 소환 매니저가 소환 연출 중인 경우 연출이 완료될 때까지 대기
            while (BossTotemManager.instance != null && BossTotemManager.instance.IsSpawning)
            {
                yield return null;
            }

            // 1. 대기 단계 (Idle)
            currentState = BossState.Idle;
            SetRunAnimBool(false);
            yield return new WaitForSeconds(idleTime);

            // 대기 단계 후 다시 한 번 소환 중인지 검사
            while (BossTotemManager.instance != null && BossTotemManager.instance.IsSpawning)
            {
                yield return null;
            }

            // 2. 돌진 단계 (Dashing)
            currentState = BossState.Dashing;
            Vector3 startPos = transform.position;
            Vector3 targetPos = GetTargetTotemPosition();
            Vector3 dashDirection = (targetPos - startPos).normalized;

            // 돌진 방향을 바라보도록 설정
            SetDirAnimFloat(dashDirection.x, dashDirection.y);
            SetRunAnimBool(true);

            // 부드럽고 빠른 돌진 이동 실행 및 잔상 생성
            float distance = Vector3.Distance(transform.position, targetPos);
            float afterimageTimer = 0f;
            
            // 시작하자마자 첫 잔상 생성
            SpawnAfterimage();

            while (distance > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, dashSpeed * Time.deltaTime);
                distance = Vector3.Distance(transform.position, targetPos);

                // 돌진하는 동안 타이머에 맞춰 잔상 스폰
                afterimageTimer += Time.deltaTime;
                if (afterimageTimer >= afterimageInterval)
                {
                    afterimageTimer = 0f;
                    SpawnAfterimage();
                }

                yield return null;
            }
            transform.position = targetPos; // 오프셋 미세 조정

            // 3. 공격 단계 (Attacking)
            currentState = BossState.Attacking;
            SetRunAnimBool(false);

            // 도착 후 공격 전 선딜레이 대기 (완전히 멈춤)
            yield return new WaitForSeconds(attackDelay);

            // 플레이어가 살아있으면 플레이어 방향을 바라보며 사격 정렬
            if (playerTarget != null)
            {
                Vector3 lookDirection = (playerTarget.transform.position - transform.position).normalized;
                SetDirAnimFloat(lookDirection.x, lookDirection.y);
            }

            // 360도 탄막 발사
            Fire360Radial();

            // 사격 직후 제자리에 멈춰 대기하는 후딜레이 시간
            yield return new WaitForSeconds(attackDuration);
        }
    }
}

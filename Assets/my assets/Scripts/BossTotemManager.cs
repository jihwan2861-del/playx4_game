using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스의 체력 조건(예: 50% 이하)에 반응하여 맵에 토템을 스폰하고 연출을 제어하는 컴포넌트 매니저 클래스입니다.
/// </summary>
public class BossTotemManager : MonoBehaviour
{
    public static BossTotemManager instance;

    [Header("=== 보스 체력 조건 ===")]
    [Tooltip("토템이 처음 활성화되는 체력 비율 (0.5 = 50%)")]
    public float triggerHpRatio = 0.5f;

    [Header("=== 연출 설정 ===")]
    [Tooltip("토템 설치 시 보스가 밝게 빛나는 지속 시간 (초)")]
    public float glowDuration = 1.2f;
    [Tooltip("연출 시 보스의 최대 크기 확대 비율")]
    public float maxScaleMultiplier = 1.25f;

    [Header("=== 추가 기믹 ===")]
    [Tooltip("체력 조건으로 최초 소환된 이후, 모든 토템이 파괴되면 다시 재생성할지 여부")]
    public bool reinstallWhenAllDestroyed = false;

    [System.Serializable]
    public struct TotemTemplate
    {
        public GameObject templateObject;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public string originalName;
    }

    private List<TotemTemplate> totemTemplates = new List<TotemTemplate>();
    private List<GameObject> activeTotemObjects = new List<GameObject>();

    private bool hasTriggered = false;
    private SpriteRenderer bossSpriteRenderer;
    private BossMovement bossMovement;
    private float lastLoggedHpRatio = 1f;
    private Enemy bossEnemy;
    private float maxBossHealth = 0f;

    /// <summary>
    /// 보스가 현재 토템을 설치/번쩍이는 연출 중인지 나타내는 플래그입니다. (BossMovement 연동용)
    /// </summary>
    public bool IsSpawning { get; private set; } = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 보스의 컴포넌트 참조 캐싱
        bossSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (bossSpriteRenderer == null) bossSpriteRenderer = GetComponent<SpriteRenderer>();

        bossMovement = GetComponent<BossMovement>();
        bossEnemy = GetComponent<Enemy>();
        if (bossEnemy == null) bossEnemy = GetComponentInChildren<Enemy>();
        if (bossEnemy != null)
        {
            maxBossHealth = bossEnemy.health;
        }

        // 씬 내부의 기존 배치된 모든 토템을 검색하여 템플릿화 및 활성 목록 등록
        TotemAI[] initialTotems = FindObjectsOfType<TotemAI>();
        foreach (var totem in initialTotems)
        {
            if (totem != null)
            {
                // 1. 비활성 복제본을 생성하여 템플릿으로 저장
                GameObject clone = Instantiate(totem.gameObject);
                clone.SetActive(false);
                clone.name = totem.gameObject.name + "_Template";
                clone.transform.SetParent(this.transform); // 보스의 자식으로 연결하여 안전하게 함께 관리

                totemTemplates.Add(new TotemTemplate
                {
                    templateObject = clone,
                    originalPosition = totem.transform.position,
                    originalRotation = totem.transform.rotation,
                    originalName = totem.gameObject.name
                });

                // 2. 초기 씬 활성 목록에 추가 (처음부터 사용 가능)
                activeTotemObjects.Add(totem.gameObject);
            }
        }

        Debug.Log($"🔧 [BossTotemManager] 씬 내의 토템 {totemTemplates.Count}개를 찾아 템플릿 캐싱 및 활성 목록 추적 개시.");
    }

    private void Update()
    {
        if (!hasTriggered)
        {
            float currentHp = 0f;
            float maxHp = 0f;

            // 1순위: BossManager 싱글톤 확인 (공식 보스 체력 관리 매니저)
            if (BossManager.instance != null)
            {
                currentHp = BossManager.instance.currentHp;
                maxHp = BossManager.instance.maxHp;
            }
            // 2순위: 로컬 Enemy 컴포넌트에서 직접 체력 읽기 (GameManager 연동 또는 독립 테스트용)
            else if (bossEnemy != null)
            {
                if (maxBossHealth <= 0)
                {
                    maxBossHealth = bossEnemy.health;
                }
                currentHp = bossEnemy.health;
                maxHp = maxBossHealth;
            }

            if (maxHp > 0f)
            {
                float hpRatio = currentHp / maxHp;
                
                // 체력 비율이 10% 단위로 감소할 때마다 디버그 로그 출력
                if (hpRatio <= lastLoggedHpRatio - 0.1f)
                {
                    lastLoggedHpRatio = Mathf.Floor(hpRatio * 10f) / 10f;
                    Debug.Log($"📊 [BossTotemManager] 보스 HP 감소 감지 중... 현재 HP: {currentHp}/{maxHp} ({hpRatio * 100:F1}%) (소환 목표치: {triggerHpRatio * 100:F1}%)");
                }

                if (hpRatio <= triggerHpRatio)
                {
                    hasTriggered = true;
                    Debug.Log($"🚨 [BossTotemManager] 조건 달성! 보스 HP가 {currentHp}/{maxHp} ({hpRatio * 100:F1}%)로 50% 이하가 되었습니다. 토템 재설치 코루틴을 호출합니다!");
                    StartCoroutine(SpawnTotemsRoutine());
                }
            }
            else
            {
                // 보스 Enemy도 없고 BossManager도 없을 시 3초에 한 번 경고 출력 (프레임 스팸 방지)
                if (Time.frameCount % 180 == 0)
                {
                    Debug.LogWarning("⚠️ [BossTotemManager] 보스의 체력 컴포넌트(Enemy/BossManager)를 감지할 수 없습니다. 스크립트 연결을 확인해주세요.");
                }
            }
        }
        else if (reinstallWhenAllDestroyed && !IsSpawning)
        {
            // 추가 설정 활성화 시: 모든 토템이 파괴되면 재소환
            activeTotemObjects.RemoveAll(t => t == null);
            if (totemTemplates.Count > 0 && activeTotemObjects.Count == 0)
            {
                Debug.Log("🚨 [BossTotemManager] 모든 토템 파괴 감지! 토템 재설치를 개시합니다.");
                StartCoroutine(SpawnTotemsRoutine());
            }
        }
    }

    private void OnDestroy()
    {
        // 템플릿 객체들이 메모리에 누수되지 않도록 명시적 파괴
        if (totemTemplates != null)
        {
            foreach (var template in totemTemplates)
            {
                if (template.templateObject != null)
                {
                    Destroy(template.templateObject);
                }
            }
        }
    }

    /// <summary>
    /// 화면 내의 모든 탄막, 레이저, 격격 폭격 연출 기기 및 활성 토템들을 찾아 일제히 소멸시킵니다.
    /// </summary>
    private void ClearAllBulletsAndActiveTotems()
    {
        // 1. 기존 리스트 추적 토템 제거
        foreach (var activeTotem in activeTotemObjects)
        {
            if (activeTotem != null)
            {
                Destroy(activeTotem);
            }
        }
        activeTotemObjects.Clear();

        // 2. 씬에 미수거 상태로 존재하는 모든 TotemAI 객체들 탐색 및 일괄 파괴
        TotemAI[] sceneTotems = FindObjectsOfType<TotemAI>();
        foreach (var totem in sceneTotems)
        {
            if (totem != null)
            {
                Destroy(totem.gameObject);
            }
        }

        // 3. 씬 내의 모든 투사체(아군 및 적군 탄알 전체) 파괴
        var projectiles = FindObjectsOfType<Projectile>();
        foreach (var p in projectiles)
        {
            if (p != null) Destroy(p.gameObject);
        }

        // 4. 씬 내의 모든 지속성 레이저 빔 파괴
        var lasers = FindObjectsOfType<LaserBeam>();
        foreach (var l in lasers)
        {
            if (l != null) Destroy(l.gameObject);
        }

        // 5. 씬 내의 모든 쿼드/그리드 폭격 연출 본체 파괴
        var gridPatterns = FindObjectsOfType<GridStrikePattern>();
        foreach (var gp in gridPatterns)
        {
            if (gp != null) Destroy(gp.gameObject);
        }

        // 6. 기타 일반 적 탄알 등 DirectMoving을 갖는 투사체 파괴
        var directMovings = FindObjectsOfType<DirectMoving>();
        foreach (var dm in directMovings)
        {
            if (dm != null && dm.gameObject != this.gameObject && !dm.CompareTag("Player") && !dm.CompareTag("Enemy"))
            {
                Destroy(dm.gameObject);
            }
        }

        Debug.Log("Core 정리: 화면 정리를 위해 모든 탄막 및 기존 토템을 제거했습니다.");
    }

    /// <summary>
    /// 보스의 체력이 50% 이하가 되었을 때 모든 탄막을 없애고 카메라를 보스 쪽으로 줌인한 뒤 보스가 커지며 빛나고,
    /// 다시 플레이어 방향으로 줌아웃하며 토템들을 부활 소환하는 시네마틱 연출 코루틴입니다.
    /// </summary>
    private IEnumerator SpawnTotemsRoutine()
    {
        IsSpawning = true;
        Debug.Log("🎬 [BossTotemManager] 50% 체력 달성! 시네마틱 페이즈 전환 연출을 시작합니다.");

        // 보스의 애니메이터 달리기 모션 정지 (안전한 헬퍼 메서드 사용)
        if (bossMovement != null)
        {
            bossMovement.SetRunAnimBool(false);
        }

        // 보스의 원래 스케일 및 컬러 백업
        Vector3 originalScale = transform.localScale;
        Color originalColor = Color.white;
        if (bossSpriteRenderer != null)
        {
            originalColor = bossSpriteRenderer.color;
        }

        // Bloom 효과 작동 (있는 경우)
        if (BloomController.instance != null)
        {
            BloomController.instance.DoBloom(4.0f, 1.5f);
        }

        // 1. GameTransitionManager에 카메라 연출 및 시간 제어 요청
        if (GameTransitionManager.instance != null)
        {
            GameTransitionManager.instance.StartBossPhaseTransition(
                transform,
                // OnMidpoint: 카메라가 줌인되었을 때 호출됨 (글로벌 정지 상태)
                () => {
                    ClearAllBulletsAndActiveTotems();
                    // 토템들을 원래 자리에 인스턴스화하여 복원 생성
                    foreach (var template in totemTemplates)
                    {
                        if (template.templateObject != null)
                        {
                            GameObject newTotem = Instantiate(template.templateObject, template.originalPosition, template.originalRotation);
                            newTotem.name = template.originalName;
                            newTotem.SetActive(true);
                            activeTotemObjects.Add(newTotem);
                        }
                    }
                    Debug.Log($"✨ [BossTotemManager] 토템 {activeTotemObjects.Count}개 재생성 및 배치 완료!");
                },
                // OnEnd: 연출이 완전히 완료되어 시간이 재개될 때 호출됨
                () => {
                    IsSpawning = false;
                    if (bossMovement != null)
                    {
                        bossMovement.EnterPhase2();
                    }
                }
            );

            // 2. 시간 정지 동안 보스가 줌인되면서 밝게 빛나고 커지는 자체 비주얼 연출 코루틴 병렬 실행
            yield return StartCoroutine(BossGlowGrowRoutine(originalScale, originalColor));
        }
        else
        {
            // 폴백: GameTransitionManager가 없으면 기존처럼 독자적으로 연출 처리
            Debug.LogWarning("⚠️ [BossTotemManager] GameTransitionManager.instance가 없어 자체 폴백 연출을 수행합니다.");
            
            ClearAllBulletsAndActiveTotems();
            
            Camera mainCam = Camera.main;
            CameraFollow camFollow = mainCam != null ? mainCam.GetComponent<CameraFollow>() : null;
            float originalOrthoSize = mainCam != null ? mainCam.orthographicSize : 5f;
            Vector3 camStartPos = mainCam != null ? mainCam.transform.position : Vector3.zero;

            if (camFollow != null) camFollow.isIntroCinematic = true;

            float zoomInDuration = 0.8f;
            float elapsed = 0f;
            float targetOrthoSize = originalOrthoSize * 0.6f;

            while (elapsed < zoomInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomInDuration);
                if (mainCam != null)
                {
                    mainCam.orthographicSize = Mathf.Lerp(originalOrthoSize, targetOrthoSize, t);
                    Vector3 targetPos = transform.position;
                    targetPos.z = camStartPos.z;
                    mainCam.transform.position = Vector3.Lerp(camStartPos, targetPos, t);
                }
                float scaleMultiplier = 1.0f + t * (maxScaleMultiplier - 1.0f);
                transform.localScale = originalScale * scaleMultiplier;
                if (bossSpriteRenderer != null)
                {
                    bossSpriteRenderer.color = Color.Lerp(originalColor, new Color(2.5f, 2.5f, 2.5f, 1f), t);
                }
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.4f);

            activeTotemObjects.Clear();
            foreach (var template in totemTemplates)
            {
                if (template.templateObject != null)
                {
                    GameObject newTotem = Instantiate(template.templateObject, template.originalPosition, template.originalRotation);
                    newTotem.name = template.originalName;
                    newTotem.SetActive(true);
                    activeTotemObjects.Add(newTotem);
                }
            }

            elapsed = 0f;
            float zoomOutDuration = 0.8f;
            Vector3 zoomInEndPos = mainCam != null ? mainCam.transform.position : camStartPos;

            while (elapsed < zoomOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomOutDuration);
                if (mainCam != null)
                {
                    mainCam.orthographicSize = Mathf.Lerp(targetOrthoSize, originalOrthoSize, t);
                    Vector3 targetPos = camStartPos;
                    if (camFollow != null && camFollow.target != null)
                    {
                        targetPos = camFollow.target.position + camFollow.offset;
                    }
                    targetPos.z = camStartPos.z;
                    mainCam.transform.position = Vector3.Lerp(zoomInEndPos, targetPos, t);
                }
                float scaleMultiplier = maxScaleMultiplier - t * (maxScaleMultiplier - 1.0f);
                transform.localScale = originalScale * scaleMultiplier;
                if (bossSpriteRenderer != null)
                {
                    bossSpriteRenderer.color = Color.Lerp(new Color(2.5f, 2.5f, 2.5f, 1f), originalColor, t);
                }
                yield return null;
            }

            transform.localScale = originalScale;
            if (bossSpriteRenderer != null) bossSpriteRenderer.color = originalColor;
            if (mainCam != null) mainCam.orthographicSize = originalOrthoSize;
            if (camFollow != null) camFollow.isIntroCinematic = false;
            
            IsSpawning = false;
            if (bossMovement != null) bossMovement.EnterPhase2();
        }
    }

    private IEnumerator BossGlowGrowRoutine(Vector3 originalScale, Color originalColor)
    {
        float duration = 0.8f;
        float elapsed = 0f;

        // 1. 커지고 밝아지는 연출 (줌인 시간 0.8초에 맞춤)
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            
            float scaleMultiplier = 1.0f + t * (maxScaleMultiplier - 1.0f);
            transform.localScale = originalScale * scaleMultiplier;

            if (bossSpriteRenderer != null)
            {
                bossSpriteRenderer.color = Color.Lerp(originalColor, new Color(2.5f, 2.5f, 2.5f, 1f), t);
            }
            yield return null;
        }

        // 2. 유지 대기 (0.4초)
        yield return new WaitForSecondsRealtime(0.4f);

        // 3. 원래 크기와 색상으로 복구 (줌아웃 시간 0.8초에 맞춤)
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            float scaleMultiplier = maxScaleMultiplier - t * (maxScaleMultiplier - 1.0f);
            transform.localScale = originalScale * scaleMultiplier;

            if (bossSpriteRenderer != null)
            {
                bossSpriteRenderer.color = Color.Lerp(new Color(2.5f, 2.5f, 2.5f, 1f), originalColor, t);
            }
            yield return null;
        }

        // 상태 최종 원복 보정
        transform.localScale = originalScale;
        if (bossSpriteRenderer != null)
        {
            bossSpriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// 현재 생존해 있는 토템들 중 하나를 무작위로 반환합니다. (BossMovement 연동용)
    /// </summary>
    public GameObject GetRandomActiveTotem()
    {
        activeTotemObjects.RemoveAll(t => t == null);
        if (activeTotemObjects.Count > 0)
        {
            int randomIndex = Random.Range(0, activeTotemObjects.Count);
            return activeTotemObjects[randomIndex];
        }
        return null;
    }
}

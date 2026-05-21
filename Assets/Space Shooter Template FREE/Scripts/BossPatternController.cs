using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 체력(생존 시간)에 따라 EZ, NORMAL, HARD 패턴을 번갈아가며 소환하며,
/// 추가적으로 백그라운드에서 상시 동방풍 기하학적 나선형 탄막을 발사하는 보스 컨트롤러입니다.
/// </summary>
public class BossPatternController : MonoBehaviour
{
    [Header("패턴 프리팹 (난이도별)")]
    public GameObject[] ezPatterns;      // 100% ~ 70%
    public float ezInterval = 2.2f;      // 이지 난이도 패턴 간격 (더 활발하게 패턴 유도)

    public GameObject[] normalPatterns;  // 70% ~ 30%
    public float normalInterval = 1.2f;  // 노말 난이도 패턴 간격

    public GameObject[] hardPatterns;    // 30% ~ 0%
    public float hardInterval = 1.2f;   // 하드(발악) 패턴 간격

    [Header("보스 체력 (생존 시간) 설정")]
    [Tooltip("보스가 나타난 후 몇 초를 버텨야 죽는지 (체력 역할)")]
    public float bossSurvivalTime = 100f;
    [HideInInspector] public float currentSurvivalTimer;
    [HideInInspector] public bool isHacking = false; // 해킹 여부

    [Header("패턴 소환 설정")]
    public float initialDelay = 1.0f;    // 처음 등장 후 대기 시간

    [Header("총알 패턴 연동")]
    [Tooltip("기하학적 총알 패턴을 전담하는 컴포넌트입니다. 비워두면 자동으로 탐색합니다.")]
    public BossBulletPatternController bulletPatternController;

    private Coroutine patternCoroutine;

    void Start()
    {
        if (bulletPatternController == null)
        {
            bulletPatternController = GetComponent<BossBulletPatternController>();
            if (bulletPatternController == null)
            {
                bulletPatternController = GetComponentInChildren<BossBulletPatternController>();
            }
        }

        currentSurvivalTimer = bossSurvivalTime;

        // 보스가 존재하는 동안 항상 주위를 도는 아우렐리온 솔 스타일 영구 공전 돌 패턴 스폰
        SpawnPermanentRockPattern();

        patternCoroutine = StartCoroutine(PatternRoutine());
    }

    void Update()
    {
        // 레벨컨트롤러가 폭주 모드(보스전)일 때만 체력이 닳기 시작함
        if (LevelController.instance != null && LevelController.instance.isFrenzyPhase && currentSurvivalTimer > 0)
        {
            // 해킹 중이면 체력(시간)이 2.5배 더 빨리 닳음!
            float timeMultiplier = isHacking ? 2.5f : 1.0f;
            currentSurvivalTimer -= Time.deltaTime * timeMultiplier;

            // 추가: 씬에 미션 패널이 존재한다면, "해킹" 미션을 찾아 진행도 연동
            if (MissionPanel.instance != null)
            {
                int hackIndex = MissionPanel.instance.FindMissionIndexByKeyword("해킹");
                if (hackIndex != -1)
                {
                    int target = MissionPanel.instance.missions[hackIndex].targetCount;
                    int hacked = Mathf.RoundToInt((1f - currentSurvivalTimer / bossSurvivalTime) * target);
                    MissionPanel.instance.SetProgress(hackIndex, hacked);
                }
            }

            // 보스 사망 (시간 끝)
            if (currentSurvivalTimer <= 0)
            {
                currentSurvivalTimer = 0;
                
                // 보스가 죽었으니 레벨컨트롤러에 승리 신호를 보냅니다!
                if (LevelController.instance != null)
                {
                    LevelController.instance.TriggerVictory();
                }

                // 추가: 씬에 미션 패널이 존재한다면, 처치 관련 미션을 완료 처리합니다.
                if (MissionPanel.instance != null)
                {
                    MissionPanel.instance.AddProgressByKeyword("처치", 1);
                    MissionPanel.instance.AddProgressByKeyword("격퇴", 1);
                    MissionPanel.instance.AddProgressByKeyword("체력", 1);
                }
                
                // 보스 격퇴 웅장한 폭사 연출 보강: Enemy 컴포넌트의 destructionVFX를 스폰해 줍니다.
                Enemy enemyComp = GetComponent<Enemy>();
                if (enemyComp == null) enemyComp = GetComponentInChildren<Enemy>();
                if (enemyComp != null && enemyComp.destructionVFX != null)
                {
                    Instantiate(enemyComp.destructionVFX, transform.position, Quaternion.identity);
                }
                
                // 보스 본체 파괴
                Destroy(gameObject); 
            }
        }
    }

    IEnumerator PatternRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            if (LevelController.instance == null || !LevelController.instance.isFrenzyPhase)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 보스 자신의 체력 비율 가져오기 (0.0 ~ 1.0)
            float ratio = Mathf.Clamp01(currentSurvivalTimer / bossSurvivalTime);

            // 체력 비율에 따라 현재 페이즈의 배열과 인터벌 선택
            GameObject[] currentPhasePatterns = null;
            float currentInterval = 2.0f;

            if (ratio > 0.7f) // 100% ~ 70%
            {
                currentPhasePatterns = ezPatterns;
                currentInterval = ezInterval;
            }
            else if (ratio > 0.3f) // 70% ~ 30%
            {
                currentPhasePatterns = normalPatterns;
                currentInterval = normalInterval;
            }
            else // 30% ~ 0% (발악)
            {
                currentPhasePatterns = hardPatterns;
                currentInterval = hardInterval;
            }

            // 선택된 페이즈 배열에 프리팹이 있다면 랜덤 소환
            if (currentPhasePatterns != null && currentPhasePatterns.Length > 0)
            {
                int rIndex = Random.Range(0, currentPhasePatterns.Length);
                GameObject selectedPrefab = currentPhasePatterns[rIndex];

                bool isLaserPattern = false;
                if (selectedPrefab != null)
                {
                    string pName = selectedPrefab.name.ToLower();
                    if (pName.Contains("검") || pName.Contains("찌르기") || pName.Contains("laser") || pName.Contains("strike"))
                    {
                        isLaserPattern = true;
                    }
                }

                if (selectedPrefab != null)
                {
                    // 아우렐리온 솔 돌 패턴은 이미 보스 기동 시 영구 구동 중이므로 랜덤 스폰에서 제외!
                    if (selectedPrefab.name.Contains("Rock") || selectedPrefab.GetComponent<BossPattern_RockDrop>() != null)
                    {
                        // 유저님의 피드백 반영: 돌 패턴 턴에는 추가 탄막을 발사하지 않고, 
                        // 플레이어가 공전 바위 회피에만 온전히 집중할 수 있도록 '숨 쉴 틈(Breathing Window)'을 제공합니다!
                        yield return new WaitForSeconds(currentInterval);
                        continue;
                    }

                    if (isLaserPattern)
                    {
                        // 검찌르기(레이저 격자) 패턴은 70% 확률로 화려한 기하학적 코딩 패턴으로 강제 대체! (발생 빈도 조절)
                        if (Random.value < 0.7f)
                        {
                            TriggerGeometricPattern();
                        }
                        else
                        {
                            Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);
                        }
                    }
                    else
                    {
                        Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);
                    }
                }
                else
                {
                    // 프리팹 슬롯이 비어있다면 기하학적 패턴으로 자동 땜질!
                    TriggerGeometricPattern();
                }
            }
            else
            {
                // 소환 셋팅이 아예 안 되어 있어도 완벽 보완!
                TriggerGeometricPattern();
            }

            yield return new WaitForSeconds(currentInterval);
        }
    }

    /// <summary>
    /// 동방풍 탄막 게임 특유의 3가지 아름다운 기하학적 탄막 중 하나를 무작위 선택하여 전개합니다.
    /// </summary>
    private void TriggerGeometricPattern()
    {
        if (bulletPatternController != null)
        {
            bulletPatternController.TriggerGeometricPattern();
        }
        else
        {
            Debug.LogWarning("⚠️ [보스 패턴] BossBulletPatternController가 연결되어 있지 않아 기하학적 총알 패턴을 실행할 수 없습니다!");
        }
    }

    /// <summary>
    /// 보스 기동 시 등록된 난이도별 패턴 목록에서 돌 공전 패턴 프리팹을 찾아 1회 안전 영구 스폰합니다.
    /// </summary>
    private void SpawnPermanentRockPattern()
    {
        GameObject rockPatternPrefab = null;

        // ezPatterns에서 돌 패턴 검색
        if (ezPatterns != null)
        {
            foreach (var prefab in ezPatterns)
            {
                if (prefab != null && (prefab.name.Contains("Rock") || prefab.GetComponent<BossPattern_RockDrop>() != null))
                {
                    rockPatternPrefab = prefab;
                    break;
                }
            }
        }

        // ezPatterns에 없으면 normalPatterns에서 검색
        if (rockPatternPrefab == null && normalPatterns != null)
        {
            foreach (var prefab in normalPatterns)
            {
                if (prefab != null && (prefab.name.Contains("Rock") || prefab.GetComponent<BossPattern_RockDrop>() != null))
                {
                    rockPatternPrefab = prefab;
                    break;
                }
            }
        }

        // 돌 공전 프리팹을 성공적으로 발견했다면 즉각 스폰
        if (rockPatternPrefab != null)
        {
            Instantiate(rockPatternPrefab, Vector3.zero, Quaternion.identity);
            Debug.Log("🛡️ [보스2] 아우렐리온 솔 스타일 영구 공전 돌 장벽을 보스 시작과 함께 활성화했습니다.");
        }
        else
        {
            Debug.LogWarning("⚠️ [보스2] 패턴 목록에서 BossPattern_RockDrop 프리팹을 찾지 못해 영구 공전 돌 장벽을 스폰하지 못했습니다.");
        }
    }

    void OnDestroy()
    {
        if (patternCoroutine != null) StopCoroutine(patternCoroutine);
    }
}

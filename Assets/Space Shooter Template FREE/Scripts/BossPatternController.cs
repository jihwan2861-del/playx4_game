using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 체력(생존 시간)에 따라 EZ, NORMAL, HARD 패턴을 번갈아가며 소환하는 보스 전용 스크립트입니다.
/// </summary>
public class BossPatternController : MonoBehaviour
{
    [Header("패턴 프리팹 (난이도별)")]
    public GameObject[] ezPatterns;      // 100% ~ 70%
    public float ezInterval = 2.5f;      // 이지 난이도 패턴 간격

    public GameObject[] normalPatterns;  // 70% ~ 30%
    public float normalInterval = 1.5f;  // 노말 난이도 패턴 간격

    public GameObject[] hardPatterns;    // 30% ~ 0%
    public float hardInterval = 0.8f;    // 하드(발악) 패턴 간격

    [Header("보스 체력 (생존 시간) 설정")]
    [Tooltip("보스가 나타난 후 몇 초를 버텨야 죽는지 (체력 역할)")]
    public float bossSurvivalTime = 180f;
    [HideInInspector] public float currentSurvivalTimer;
    [HideInInspector] public bool isHacking = false; // 해킹 여부

    [Header("패턴 소환 설정")]
    public float initialDelay = 1.0f;    // 처음 등장 후 대기 시간

    private Coroutine patternCoroutine;

    void Start()
    {
        currentSurvivalTimer = bossSurvivalTime;
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

            // 보스 사망 (시간 끝)
            if (currentSurvivalTimer <= 0)
            {
                currentSurvivalTimer = 0;
                
                // 보스가 죽었으니 레벨컨트롤러에 승리 신호를 보냅니다!
                if (LevelController.instance != null)
                {
                    LevelController.instance.TriggerVictory();
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

                if (selectedPrefab != null)
                {
                    Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);
                }
            }

            // 현재 난이도에 맞는 인터벌만큼 대기
            yield return new WaitForSeconds(currentInterval);
        }
    }

    void OnDestroy()
    {
        if (patternCoroutine != null)
        {
            StopCoroutine(patternCoroutine);
        }
    }
}

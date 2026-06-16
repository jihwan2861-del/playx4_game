using UnityEngine;

/// <summary>
/// 패링 튜토리얼의 단계별(1번째, 2번째 등) 슬로우모션 연출과 
/// DialoguePlayer 컴포넌트 실행 흐름을 통합 제어하는 연출 매니저 클래스입니다.
/// </summary>
public class ParryStop : MonoBehaviour
{
    public static ParryStop instance;

    [Header("단계별 대화 플레이어")]
    [Tooltip("패링 시도 횟수별로 실행할 DialoguePlayer 컴포넌트 리스트입니다. 각 대화 플레이어에서 카메라 흔들림, 효과음, 나레이션/말풍선 전환을 자유롭게 조절할 수 있습니다.")]
    public DialoguePlayer[] dialogueSteps;

    [Header("시간 및 쿨다운 설정")]
    [Tooltip("패링 성공 직후 다음 총알이 바로 오자마자 슬로우모션이 재발동하는 것을 막는 쿨다운 시간(초)입니다.")]
    public float cooldownDuration = 1.2f;

    [HideInInspector] public int parryCount = 0; // 현재 진행 중인 패링 횟수 인덱스
    private bool isWaitingForInput = false;
    private float cooldownTimer = 0f;

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

    private void Update()
    {
        // 쿨다운 타이머 갱신 (슬로우모션 상태와 무관하게 현실 시간 기준으로 감소)
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.unscaledDeltaTime;
        }

        // 슬로우모션 연출 중 Spacebar(패링 키) 입력 감지 대기
        if (isWaitingForInput && Input.GetKeyDown(KeyCode.Space))
        {
            isWaitingForInput = false;
            cooldownTimer = cooldownDuration; // 쿨다운 돌입

            // 1. 시간 정상화
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            // 2. 현재 실행 중인 DialoguePlayer 정지 및 대화창 강제 닫기
            if (parryCount < dialogueSteps.Length && dialogueSteps[parryCount] != null)
            {
                dialogueSteps[parryCount].StopAllCoroutines();
                
                // DialoguePlayer가 코루틴 중단으로 인해 미처 처리하지 못한 UI 및 플레이어 해금 강제 복구
                if (NarrationUI.instance != null && NarrationUI.instance.gameObject.activeInHierarchy) 
                    NarrationUI.instance.Close();
                if (SpeechBubble.playerBubbleInstance != null && SpeechBubble.playerBubbleInstance.gameObject.activeInHierarchy) 
                    SpeechBubble.playerBubbleInstance.Close();
                
                // 대화로 인해 얼어붙어 있던 플레이어 기체 복원
                if (PlayerMoving.instance != null)
                {
                    PlayerMoving.instance.enabled = true;
                }
                
                // 대화로 인해 멈춰섰던 자동 이동 복원
                if (PlayerMoving.instance != null)
                {
                    PlayerAutoMove autoMove = PlayerMoving.instance.GetComponent<PlayerAutoMove>();
                    if (autoMove != null)
                    {
                        autoMove.ResumeMove();
                    }
                }
            }

            // 3. 인덱스 증가
            parryCount++;
            Debug.Log($"🛡️ [ParryStop] {parryCount}번째 패링 입력 감지! DialoguePlayer 강제 정지 및 시간 복원 완료.");

            // 4. 모든 가이드 연출이 끝나면 센서 오브젝트 제거
            if (parryCount >= dialogueSteps.Length)
            {
                DisableAllTutorialSensors();
            }
        }
    }

    /// <summary>
    /// 센서(ParryTutorialSensor)로부터 적 총알 감지 신호를 받았을 때 호출됩니다.
    /// </summary>
    public void OnBulletDetected()
    {
        // 이미 대기 중이거나, 쿨다운 중이거나, 연출이 모두 완료된 상태라면 무시
        if (isWaitingForInput || cooldownTimer > 0f || parryCount >= dialogueSteps.Length)
        {
            return;
        }

        TriggerTutorialSlowMotion();
    }

    private void TriggerTutorialSlowMotion()
    {
        if (parryCount >= dialogueSteps.Length || dialogueSteps[parryCount] == null)
        {
            return;
        }

        isWaitingForInput = true;
        Debug.Log($"⏳ [ParryStop] {parryCount + 1}번째 DialoguePlayer 연출 트리거!");

        // 1. 극적인 슬로우 모션 (시간 속도를 5%로 낮춤)
        Time.timeScale = 0.05f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 2. 등록된 DialoguePlayer의 대사 재생 시작
        dialogueSteps[parryCount].PlayDialogues();
    }

    private void DisableAllTutorialSensors()
    {
        Debug.Log("🎉 [ParryStop] 모든 지정된 패링 튜토리얼 연출이 완료되었습니다. 센서를 비활성화합니다.");
        
        // 씬 내의 모든 ParryTutorialSensor 컴포넌트를 찾아 파괴/비활성화 처리
        ParryTutorialSensor[] sensors = FindObjectsOfType<ParryTutorialSensor>();
        foreach (var sensor in sensors)
        {
            if (sensor != null)
            {
                // 부모 플레이어 기체를 날려버리지 않기 위해, 센서가 붙어있는 자식 오브젝트만 골라 파괴합니다.
                if (sensor.gameObject != Player.instance?.gameObject)
                {
                    Destroy(sensor.gameObject);
                }
                else
                {
                    Destroy(sensor);
                }
            }
        }
    }
}

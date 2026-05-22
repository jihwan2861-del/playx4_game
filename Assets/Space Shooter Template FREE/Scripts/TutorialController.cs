using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 튜토리얼의 3단계 흐름(1단계 대쉬 회피 -> 2단계 해킹 설명 -> 3단계 더미봇 처치 완료)을
/// 각각의 다이얼로그 대화 및 미션 조건과 동적으로 연동하여 제어하는 마스터 컨트롤러입니다.
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController instance;

    [Header("=== 1단계: 시작 & 대쉬 회피 대화 ===")]
    [Tooltip("튜토리얼 시작 시 재생될 대화 (대쉬 조작법 설명 등)")]
    public List<DialogueLine> phase1Dialogue = new List<DialogueLine>()
    {
        new DialogueLine { speakerName = "사령부 AI", message = "경고! 도시 보안용 사이보그 기체들이 바이러스에 감염되어 무장 반란을 일으켰습니다.", isLeft = true },
        new DialogueLine { speakerName = "사령부 AI", message = "제이 수석 엔지니어님, 개조한 디버깅선(H.A.C.K)의 성능을 즉시 점검해야 합니다. 폭주하는 더미 로봇의 공격 프로토콜이 시작됩니다!", isLeft = true },
        new DialogueLine { speakerName = "사령부 AI", message = "[Space] 키나 마우스 우클릭을 눌러 패링 보호막을 전개해 보세요. 날아오는 유해 데이터(탄환)를 안전하게 파괴/격리할 수 있습니다.", isLeft = true },
        new DialogueLine { speakerName = "엔지니어 제이", message = "걱정 마십시오. 제가 직접 튜닝한 실시간 디버깅 쉴드라면 사이보그의 오염 데이터 탄막쯤은 문제없이 분쇄해 격리할 수 있습니다!", isLeft = false }
    };

    [Header("=== 2단계: 대쉬 완료 & 해킹 설명 대화 ===")]
    [Tooltip("대쉬 3번이 끝난 직후 재생될 대화 (해킹 메커니즘 설명 등)")]
    public List<DialogueLine> phase2Dialogue = new List<DialogueLine>()
    {
        new DialogueLine { speakerName = "사령부 AI", message = "쉴드 격리 데이터 복구율 100%! 아주 매끄러운 튜닝 타이밍입니다.", isLeft = true },
        new DialogueLine { speakerName = "사령부 AI", message = "이제 반격 단계입니다. 저 반란 사이보그는 외부에 일반 복구 빔이 통하지 않는 강력한 데이터 방화벽을 두르고 있습니다.", isLeft = true },
        new DialogueLine { speakerName = "사령부 AI", message = "적 사이보그 주변의 파란색 해킹 포트(원 영역) 안으로 직접 접근하십시오! 함선의 디버거가 활성화되어 놈들의 보안 프로토콜을 강제 해킹할 것입니다.", isLeft = true },
        new DialogueLine { speakerName = "엔지니어 제이", message = "알겠습니다. 탄막을 파고들어 해킹존 내부에서 오버라이드가 완료될 때까지 신속하게 오염 코드를 재구성하겠습니다!", isLeft = false }
    };

    [Header("=== 3단계: 더미 처치 완료 & 훈련 종료 대화 ===")]
    [Tooltip("더미 봇을 최종 해킹 및 처치했을 때 재생될 마무리 대화")]
    public List<DialogueLine> phase3Dialogue = new List<DialogueLine>()
    {
        new DialogueLine { speakerName = "사령부 AI", message = "코드 오버라이드 완료! 오염 코드가 성공적으로 디버깅되어 더미 사이보그가 비활성화되었습니다.", isLeft = true },
        new DialogueLine { speakerName = "엔지니어 제이", message = "성공이군요. 적의 핵심 코드를 가로채어 바이러스를 완벽하게 격리하고 정상 시스템을 복원했습니다.", isLeft = false },
        new DialogueLine { speakerName = "사령부 AI", message = "훌륭한 엔지니어링 능력입니다. 기본 성능 점검은 완료되었습니다. 즉시 메인 커맨드 센터(HUB)로 복귀하여 출격 준비를 진행하십시오!", isLeft = true }
    };

    [Header("=== 연결할 씬 내 오브젝트 ===")]
    [Tooltip("훈련 대상이 될 더미 봇 오브젝트")]
    public TutorialDummy dummyBot;
    
    [Tooltip("대화를 출력할 IntroDialogueManager 컴포넌트")]
    public IntroDialogueManager dialogueManager;

    [Tooltip("1단계 대쉬 훈련 중 화면에 표시할 가이드 텍스트 오브젝트 (예: '스페이스바로 회피하세요!' 빨간 글씨)")]
    public GameObject dashGuideTextObject;

    [Header("=== 튜토리얼 BGM 설정 ===")]
    [Tooltip("튜토리얼 씬에서 재생할 배경음악 (BGM)")]
    public AudioClip tutorialBGM;
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;

    [Header("=== 미션 종료 후 복귀할 씬 이름 ===")]
    [Tooltip("튜토리얼 완료 후 전환될 로비/마을 씬의 정확한 이름")]
    public string lobbySceneName = "Hub_Scene";

    // 진행 상태 추적 변수
    private int currentPhase = 1;
    private AudioSource bgmAudioSource;
    private bool phase2Triggered = false;
    private bool phase3Triggered = false;
    private bool isDummyDead = false;

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
        // BGM 자동 재생 설정
        if (tutorialBGM != null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
            bgmAudioSource.clip = tutorialBGM;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bgmVolume;
            bgmAudioSource.spatialBlend = 0f; // 2D BGM
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.Play();
            Debug.Log($"🎵 [TutorialController] 튜토리얼 BGM '{tutorialBGM.name}' 재생을 시작합니다.");
        }

        // 1단계 시작: 더미 봇의 설정을 대쉬 훈련에 맞게 초기화
        if (dummyBot != null)
        {
            dummyBot.isShooterMode = true;   // 총알 사격 ON
            dummyBot.isHackingMode = false;  // 해킹은 아직 OFF (대쉬 다 피해야 해킹 시작하도록 봉쇄)
        }

        // 1단계 시작 대화 주입 및 강제 실행 (실행 순서 버그 방지)
        if (dialogueManager != null)
        {
            // 게임 시작 직후 가이드 텍스트는 꺼둔 상태로 시작 (대화 종료 후 활성화)
            if (dashGuideTextObject != null) dashGuideTextObject.SetActive(false);

            dialogueManager.StartDialogueWithCallback(phase1Dialogue.ToArray(), () => 
            {
                Debug.Log("🎯 1단계 활성화: 대쉬 회피 훈련 시작!");
                // 1단계 대화가 모두 완료되어 실전 돌입 시 가이드 텍스트 활성화!
                if (dashGuideTextObject != null) dashGuideTextObject.SetActive(true);
            });
        }
    }

    private void Update()
    {
        // 1단계 감시: 대쉬 회피 미션이 완료되었는지 체크
        if (currentPhase == 1 && !phase2Triggered && MissionPanel.instance != null)
        {
            int dashIndex = MissionPanel.instance.FindMissionIndexByKeyword("파괴");
            if (dashIndex != -1 && MissionPanel.instance.missions[dashIndex].isCompleted)
            {
                phase2Triggered = true;
                TriggerPhase2();
            }
        }

        // 2단계 감시: 더미 봇이 파괴되었는지 감시
        if (currentPhase == 2 && !phase3Triggered)
        {
            // 더미 봇이 파괴되어 null이 되었거나 죽었다고 판정될 때
            if (dummyBot == null && !isDummyDead)
            {
                isDummyDead = true;
                phase3Triggered = true;
                TriggerPhase3();
            }
        }
    }

    /// <summary>
    /// 2단계: 해킹 가이드 대화 시작 및 더미 봇 상태 변경
    /// </summary>
    private void TriggerPhase2()
    {
        currentPhase = 2;

        // 1단계 회피 훈련이 끝났으므로 가이드 텍스트 비활성화!
        if (dashGuideTextObject != null) dashGuideTextObject.SetActive(false);

        // 대화 진행 중에는 안전하게 물리 및 조작을 다시 멈춤
        Time.timeScale = 0f;
        SetPlayerControl(false);

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogueWithCallback(phase2Dialogue.ToArray(), () =>
            {
                // 2단계 대화가 모두 끝나면 실행할 실제 플레이 모드 설정:
                // 더미 봇이 해킹이 가능하도록 전환
                // (만약 dummyBot이 파괴되지 않았다면 실시간으로 모드 변경)
                if (dummyBot != null)
                {
                    dummyBot.isHackingMode = true;   // 해킹 가능 ON
                    dummyBot.isShooterMode = true;   // 사격도 함께 유지 (난이도 및 긴장감 제공)
                    
                    // 더미 봇의 색상이나 LineRenderer 아우라 라인을 켜주기 위해 내부 시작 함수를 리셋/재작동 유도
                    dummyBot.gameObject.SetActive(false);
                    dummyBot.gameObject.SetActive(true);
                }
                
                Debug.Log("🎯 2단계 활성화: 더미 봇 해킹 훈련 시작!");
            });
        }
    }

    /// <summary>
    /// 3단계: 더미 처치 완료 마무리 대화 및 씬 전환 연동
    /// </summary>
    private void TriggerPhase3()
    {
        currentPhase = 3;

        // 더미가 죽는 순간 즉시 시간을 멈추고 축하 대화를 시작
        Time.timeScale = 0f;
        SetPlayerControl(false);

        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }

        if (dialogueManager != null)
        {
            dialogueManager.StartDialogueWithCallback(phase3Dialogue.ToArray(), () =>
            {
                // 3단계 대화가 완전히 끝나면 기지(로비 씬)로 출격 전환!
                Debug.Log($"🎉 튜토리얼 완수! {lobbySceneName} 씬으로 복귀합니다.");

                // 튜토리얼 클리어 정보 저장 및 자동 보존
                if (PlayerDataManager.instance != null)
                {
                    PlayerDataManager.instance.tutorialCompleted = true;
                    PlayerDataManager.instance.SaveData();
                }
                PlayerPrefs.SetInt("Mission_Tutorial_Completed", 1);
                PlayerPrefs.Save();

                SceneManager.LoadScene(lobbySceneName);
            });
        }
    }

    private void SetPlayerControl(bool canMove)
    {
        PlayerMoving player = FindObjectOfType<PlayerMoving>();
        if (player != null)
        {
            player.enabled = canMove;
            if (!canMove)
            {
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
            }
        }
    }
}

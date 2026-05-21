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
        new DialogueLine { speakerName = "사령관", message = "좋아, 신입 파일럿! 전투 훈련장에 온 것을 환영한다.", isLeft = true },
        new DialogueLine { speakerName = "사령관", message = "앞에 보이는 훈련용 더미가 탄막을 쏠 것이다. [Space] 키나 마우스 우클릭을 눌러 보호막을 전개하고(패링), 날아오는 총알을 파괴해 보아라!", isLeft = true },
        new DialogueLine { speakerName = "주인공", message = "알겠습니다. 사격 타이밍에 맞춰 패링 보호막을 펼쳐 총알을 격파하겠습니다!", isLeft = false }
    };

    [Header("=== 2단계: 대쉬 완료 & 해킹 설명 대화 ===")]
    [Tooltip("대쉬 3번이 끝난 직후 재생될 대화 (해킹 메커니즘 설명 등)")]
    public List<DialogueLine> phase2Dialogue = new List<DialogueLine>()
    {
        new DialogueLine { speakerName = "사령관", message = "회피 기동 훈련 통과! 아주 훌륭한 타이밍이었다.", isLeft = true },
        new DialogueLine { speakerName = "사령관", message = "이제 반격할 차례다. 저 녀석은 일반 공격이 통하지 않는 나노 배리어를 두르고 있다.", isLeft = true },
        new DialogueLine { speakerName = "사령관", message = "더미 봇 주변의 파란색 해킹존(원 영역) 안으로 들어가서 접근해라! 시스템이 자동으로 적의 방어막을 해킹할 것이다.", isLeft = true },
        new DialogueLine { speakerName = "주인공", message = "알겠습니다. 탄막을 피하며 영역 내에서 해킹 완료 시까지 버티겠습니다!", isLeft = false }
    };

    [Header("=== 3단계: 더미 처치 완료 & 훈련 종료 대화 ===")]
    [Tooltip("더미 봇을 최종 해킹 및 처치했을 때 재생될 마무리 대화")]
    public List<DialogueLine> phase3Dialogue = new List<DialogueLine>()
    {
        new DialogueLine { speakerName = "사령관", message = "훌륭하게 완수했군! 더미 봇의 내부 시스템이 완벽하게 과부하되어 파괴되었다.", isLeft = true },
        new DialogueLine { speakerName = "주인공", message = "비무장 기체로도 해킹을 연계해 반격하는 요령을 완벽히 파악했습니다.", isLeft = false },
        new DialogueLine { speakerName = "사령관", message = "기초 훈련은 이것으로 종료한다. 이제 메인 기지로 복귀하여 첫 출격 준비를 하도록 해라. 고생했다!", isLeft = true }
    };

    [Header("=== 연결할 씬 내 오브젝트 ===")]
    [Tooltip("훈련 대상이 될 더미 봇 오브젝트")]
    public TutorialDummy dummyBot;
    
    [Tooltip("대화를 출력할 IntroDialogueManager 컴포넌트")]
    public IntroDialogueManager dialogueManager;

    [Tooltip("1단계 대쉬 훈련 중 화면에 표시할 가이드 텍스트 오브젝트 (예: '스페이스바로 회피하세요!' 빨간 글씨)")]
    public GameObject dashGuideTextObject;

    [Header("=== 미션 종료 후 복귀할 씬 이름 ===")]
    [Tooltip("튜토리얼 완료 후 전환될 로비/마을 씬의 정확한 이름")]
    public string lobbySceneName = "Hub_Scene";

    // 진행 상태 추적 변수
    private int currentPhase = 1;
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

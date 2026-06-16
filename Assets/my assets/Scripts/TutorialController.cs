using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 씬 제어 및 기본적인 유틸리티 이벤트를 제공하는 경량 시네마틱 매니저입니다.
/// 무거운 하드코딩 대사 관리를 모두 걷어내고, 유니티 인스펙터(UnityEvent)에서
/// 호출할 수 있는 유연한 기능(씬 이동, 플레이어 제어, 연출 유틸리티)만 제공합니다.
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController instance;

    // 외부 호환성 유지를 위한 컴포넌트 자동 검색 필드
    [HideInInspector] public PlayerMoving player;

    [HideInInspector] public int currentPhase = 2; // 이전 스크립트들과의 컴파일 호환성을 위한 유지

    [Header("=== UI 안내 연출 설정 ===")]
    [Tooltip("안내 가이드 UI 패널 (비어있어도 연출 실행 시 자동 무시됨)")]
    public GameObject guidePanel;
    [Tooltip("안내 가이드 텍스트 컴포넌트")]
    public TextMeshProUGUI guideText;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 플레이어 캐릭터 자동 검색 및 캐싱
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerMoving>();
        }
    }

    /// <summary>
    /// 인스펙터 이벤트 슬롯에서 다른 씬으로 넘어갈 때 사용하는 함수입니다.
    /// </summary>
    /// <param name="sceneName">이동할 씬 이름 (예: Hub_Scene)</param>
    public void LoadNextScene(string sceneName)
    {
        Debug.Log($"🎬 [TutorialController] 씬 전환을 시도합니다: -> {sceneName}");
        Time.timeScale = 1f;
        if (GameTransitionManager.instance != null)
        {
            GameTransitionManager.instance.TriggerTransition(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// 대화나 시네마틱 연출 중에 플레이어 조작을 일시적으로 차단/허용하는 함수입니다.
    /// </summary>
    /// <param name="freeze">true이면 조작 차단, false이면 조작 허용</param>
    public void FreezePlayer(bool freeze)
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.GetComponent<PlayerMoving>();
        }

        if (player != null)
        {
            player.enabled = !freeze;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            
            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("isMoving", false);
            }
            Debug.Log($"   [TutorialController] 플레이어 기체 제어 상태 변경: Freeze = {freeze}");
        }
    }

    /// <summary>
    /// 가이드 안내 텍스트UI를 켭니다. (예: '마우스로 클릭해서 적을 조준하세요')
    /// </summary>
    public void ShowGuideText(string text)
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(true);
        }
        else if (guideText != null)
        {
            guideText.gameObject.SetActive(true);
        }

        if (guideText != null)
        {
            guideText.text = text;
        }
    }

    /// <summary>
    /// 가이드 안내 텍스트UI를 끕니다.
    /// </summary>
    public void HideGuideText()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
        else if (guideText != null)
        {
            guideText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 플레이어 머리 위에 말풍선을 띄웁니다.
    /// </summary>
    public void ShowPlayerSpeech(string text)
    {
        if (SpeechBubble.playerBubbleInstance != null)
        {
            SpeechBubble.playerBubbleInstance.ShowDialogueInspector(text);
        }
        else
        {
            Debug.LogWarning("⚠️ [TutorialController] 플레이어 말풍선(SpeechBubble.playerBubbleInstance)이 씬에 존재하지 않습니다.");
        }
    }

    /// <summary>
    /// 플레이어를 특정 타겟 좌표 위치로 빠르게 넉백(날려보내기)시킵니다.
    /// </summary>
    public void KnockbackPlayerTo(Transform targetPos)
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.GetComponent<PlayerMoving>();
        }

        if (player != null && targetPos != null)
        {
            PlayerAutoMove autoMove = player.GetComponent<PlayerAutoMove>();
            if (autoMove != null)
            {
                Debug.Log($"   [TutorialController] 플레이어 넉백 개시 -> 목적지: {targetPos.name}");
                // 빠른 속도로 강제 이동 처리
                autoMove.MoveTo(targetPos.position, 24f, null);
            }
            else
            {
                Debug.LogError("⚠️ [TutorialController] 플레이어에게 PlayerAutoMove 컴포넌트가 존재하지 않습니다.");
            }
        }
    }

    /// <summary>
    /// 플레이어 캐릭터의 외형(Sprite)과 애니메이터(Animator Controller)를 패럴만 사양으로 교체합니다.
    /// </summary>
    public void ChangePlayerToParalman(Sprite paralmanSprite, RuntimeAnimatorController paralmanAnimator)
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.GetComponent<PlayerMoving>();
        }

        if (player != null)
        {
            GameObject playerObj = player.gameObject;

            // 1. SpriteRenderer 교체
            SpriteRenderer sr = playerObj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && paralmanSprite != null)
            {
                sr.sprite = paralmanSprite;
                Debug.Log("👤 [외형 교체] 플레이어 스프라이트 -> 패럴만");
            }

            // 2. AnimatorController 교체
            Animator anim = playerObj.GetComponentInChildren<Animator>();
            if (anim != null && paralmanAnimator != null)
            {
                anim.runtimeAnimatorController = paralmanAnimator;
                Debug.Log("👤 [외형 교체] 플레이어 애니메이터 -> 패럴만");
            }

            // 보너스: 체력 완충
            if (Player.instance != null)
            {
                Player.instance.health = 5; 
            }
            player.currentEnergy = player.maxEnergy;
        }
    }

    /// <summary>
    /// 화면의 타임스케일을 강제로 느리게(슬로우 모션) 만들거나 원래 속도로 리셋합니다.
    /// </summary>
    /// <param name="active">true이면 20% 속도 슬로우, false이면 100% 정상 속도</param>
    public void SetSlowMotion(bool active)
    {
        if (active)
        {
            Time.timeScale = 0.2f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            Debug.Log("⏳ [TutorialController] 슬로우 모션 활성화 (TimeScale = 0.2)");
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            Debug.Log("⏳ [TutorialController] 슬로우 모션 해제 (TimeScale = 1.0)");
        }
    }
}

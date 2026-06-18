using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // 🌟 코루틴 IEnumerator 사용을 위해 추가
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

    [Header("=== 패럴만 변신 설정 ===")]
    [Tooltip("패럴만 캐릭터의 스프라이트")]
    public Sprite paralmanSprite;
    [Tooltip("패럴만 캐릭터의 애니메이터 컨트롤러")]
    public RuntimeAnimatorController paralmanAnimator;
    [Tooltip("기존 주인공 기체를 옆에 소환할 프리팹 (비워두면 현재 기체 이미지를 자동 복사하여 껍데기 더미로 생성합니다)")]
    public GameObject originalPlayerDummyPrefab;
    [Tooltip("기존 주인공 기체의 앞을 바라보는 스프라이트 (더미 소환 시 적용됩니다)")]
    public Sprite originalPlayerFrontSprite;

    [Header("=== UI 제어 설정 (변신 연출 연동) ===")]
    [Tooltip("플레이어 체력/에너지 UI 캔버스 오브젝트")]
    public GameObject playerCanvas;
    [Tooltip("보스 체력/시간 UI 캔버스 오브젝트")]
    public GameObject bossCanvas;

    [Header("=== 연출 상태 저장 ===")]
    [HideInInspector] public bool isParalman = false;

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

    private void OnDestroy()
    {
        if (instance == this) instance = null;
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
    /// 페이드 아웃 연출과 함께 기존 기체를 플레이어 옆에 스폰하고, 조종 캐릭터를 패럴만으로 변경하는 전체 연출을 시작합니다.
    /// 유니티 UnityEvent(트리거)에서 직접 호출하기 적합합니다.
    /// </summary>
    public void ChangeToParalmanWithFade()
    {
        StartCoroutine(ParalmanTransitionRoutine());
    }

    private IEnumerator ParalmanTransitionRoutine()
    {
        // 1. 임시 페이드용 블랙 캔버스 동적 생성 (씬 UI 의존성 제거)
        GameObject transitionCanvas = new GameObject("TemporaryFadeCanvas");
        Canvas canvas = transitionCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        var scaler = transitionCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject blackImageObj = new GameObject("BlackImage");
        blackImageObj.transform.SetParent(transitionCanvas.transform, false);
        UnityEngine.UI.Image blackImg = blackImageObj.AddComponent<UnityEngine.UI.Image>();
        blackImg.color = new Color(0f, 0f, 0f, 0f); // 투명 상태로 시작

        RectTransform rect = blackImg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 2. 플레이어 조작 차단 및 UI 끄기 (연출 몰입도 향상)
        FreezePlayer(true);
        SetGameUIsActive(false);

        // 3. 페이드 아웃 (화면 어두워짐)
        float elapsed = 0f;
        float fadeDuration = 0.8f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackImg.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        blackImg.color = new Color(0f, 0f, 0f, 1f);

        // 암전 상태에서 0.3초 대기
        yield return new WaitForSecondsRealtime(0.3f);

        // 🌟 [추가] 씬 내에 주차되어 있던 기존 '패럴만' 기체(오브젝트) 비활성화
        GameObject fieldParalman = GameObject.Find("패럴만");
        if (fieldParalman != null)
        {
            fieldParalman.SetActive(false);
            Debug.Log("👤 [Paralman Transition] 기존 필드에 배치된 '패럴만' 오브젝트를 비활성화했습니다.");
        }
        else
        {
            GameObject fieldParalmanEng = GameObject.Find("Paralman");
            if (fieldParalmanEng != null)
            {
                fieldParalmanEng.SetActive(false);
                Debug.Log("👤 [Paralman Transition] 기존 필드에 배치된 'Paralman' 오브젝트를 비활성화했습니다.");
            }
        }

        // 4. 기존 주인공 기체 옆에 스폰
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.GetComponent<PlayerMoving>();
        }

        if (player != null)
        {
            // 플레이어 왼쪽 1.3미터 지점 계산
            Vector3 spawnPos = player.transform.position + new Vector3(-1.3f, 0f, 0f);
            GameObject dummyInstance = null;
            
            if (originalPlayerDummyPrefab != null)
            {
                // 프리팹을 쓸 경우 앞을 바르게 보도록 회전 없이(Quaternion.identity) 생성
                dummyInstance = Instantiate(originalPlayerDummyPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // 프리팹을 비워둔 경우: 새로운 빈 오브젝트 생성
                dummyInstance = new GameObject("OriginalPlayer_Vehicle");
                dummyInstance.transform.position = spawnPos;
                dummyInstance.transform.rotation = Quaternion.identity; // 앞을 똑바로 바라보도록 회전 세팅
                
                SpriteRenderer dummySr = dummyInstance.AddComponent<SpriteRenderer>();
                SpriteRenderer playerSr = player.GetComponentInChildren<SpriteRenderer>();
                
                // 지정된 앞모습 스프라이트가 있으면 쓰고, 없으면 현재 플레이어 스프라이트를 씀
                if (originalPlayerFrontSprite != null)
                {
                    dummySr.sprite = originalPlayerFrontSprite;
                }
                else if (playerSr != null)
                {
                    dummySr.sprite = playerSr.sprite;
                }

                if (playerSr != null)
                {
                    dummySr.color = playerSr.color;
                    dummySr.sortingLayerID = playerSr.sortingLayerID;
                    dummySr.sortingOrder = playerSr.sortingOrder;
                }
            }

            // ⚠️ 플레이어와 충돌해서 밀려나지 않도록 물리 및 충돌체 완벽 차단 및 고정
            if (dummyInstance != null)
            {
                // 1) 리지드바디가 있다면 물리 반응 차단 및 Static 고정
                Rigidbody2D dummyRb = dummyInstance.GetComponent<Rigidbody2D>();
                if (dummyRb == null)
                {
                    dummyRb = dummyInstance.AddComponent<Rigidbody2D>();
                }
                if (dummyRb != null)
                {
                    dummyRb.bodyType = RigidbodyType2D.Static;
                    dummyRb.simulated = false;
                }

                // 2) 콜라이더가 있다면 모두 끔 (밀려남 방지)
                Collider2D[] dummyColliders = dummyInstance.GetComponentsInChildren<Collider2D>();
                foreach (var col in dummyColliders)
                {
                    if (col != null)
                    {
                        col.enabled = false;
                    }
                }

                // 3) 플레이어 제어 및 사격 스크립트 무력화 (더미가 스스로 총을 쏘거나 이동하는 것을 완전히 방지)
                MonoBehaviour[] scripts = dummyInstance.GetComponentsInChildren<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    if (script == null) continue;
                    
                    string scriptName = script.GetType().Name;
                    if (scriptName.Contains("PlayerShooting") || 
                        scriptName.Contains("PlayerMoving") || 
                        scriptName.Contains("PlayerClickAttack") || 
                        scriptName.Contains("Player"))
                    {
                        script.enabled = false;
                    }
                }
            }
            Debug.Log("🤖 [Paralman Transition] 기존 주인공 기체를 제자리에 물리적으로 고정하고 사격을 차단하여 앞모습으로 스폰 완료.");
        }
        else
        {
            Debug.LogError("⚠️ [Paralman Transition] 플레이어 레퍼런스(player)를 찾을 수 없어 기존 기체 더미 소환을 생략했습니다.");
        }

        // 5. 플레이어를 패럴만으로 변신 (스프라이트/애니메이터 교체)
        ChangePlayerToParalman();

        // 세팅 완료 후 0.2초 대기
        yield return new WaitForSecondsRealtime(0.2f);

        // 🌟 [추가] 페이드인 직전 UI를 다시 켭니다.
        SetGameUIsActive(true);

        // 6. 페이드 인 (화면 밝아짐)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackImg.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - (elapsed / fadeDuration)));
            yield return null;
        }

        // 7. 조작 해제 및 캔버스 소멸
        FreezePlayer(false);
        Destroy(transitionCanvas);
        Debug.Log("✨ [Paralman Transition] 패럴만 탑승/조종 연출 완료!");
    }

    /// <summary>
    /// 더미봇 격파 성공 시 플레이어 대사를 출력하고 2.5초 뒤 허브 씬으로 복귀하는 클리어 시퀀스입니다.
    /// </summary>
    public IEnumerator TutorialClearRoutine()
    {
        // 1. 플레이어 머리 위에 말풍선 띄우기
        ShowPlayerSpeech("드디어 끝인가....");

        // 2. 대사를 읽을 시간 2.5초 대기
        yield return new WaitForSecondsRealtime(2.5f);

        // 3. 허브 씬으로 페이드 전환하며 이동
        LoadNextScene("Hub_Scene");
    }

    /// <summary>
    /// 인스펙터에 미리 세팅해 둔 스프라이트와 애니메이터로 플레이어를 패럴만으로 변경합니다.
    /// 매개변수가 없어 유니티 UnityEvent(트리거)에서 직접 호출하기 적합합니다.
    /// </summary>
    public void ChangePlayerToParalman()
    {
        if (paralmanSprite != null && paralmanAnimator != null)
        {
            ChangePlayerToParalman(paralmanSprite, paralmanAnimator);
        }
        else
        {
            Debug.LogError("⚠️ [TutorialController] 패럴만 Sprite 또는 AnimatorController가 인스펙터에 등록되지 않았습니다!");
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

            // 3. 공격 데미지를 15로 변경 (패럴만 전용 스펙)
            PlayerClickAttack clickAttack = playerObj.GetComponent<PlayerClickAttack>();
            if (clickAttack == null)
            {
                clickAttack = playerObj.GetComponentInChildren<PlayerClickAttack>();
            }
            if (clickAttack != null)
            {
                clickAttack.attackDamage = 15;
                Debug.Log("👤 [데미지 변경] 패럴만 사격 공격 데미지 -> 15");
            }

            // 4. 패럴만 변신 상태 플래그 활성화
            isParalman = true;
        }
    }

    /// <summary>
    /// 플레이어 UI와 보스 UI의 캔버스 오브젝트를 한번에 켜고 끕니다.
    /// </summary>
    public void SetGameUIsActive(bool active)
    {
        if (playerCanvas != null)
        {
            playerCanvas.SetActive(active);
            Debug.Log($"🖥️ [TutorialController] 플레이어 UI 상태 변경: {active}");
        }
        if (bossCanvas != null)
        {
            bossCanvas.SetActive(active);
            Debug.Log($"🖥️ [TutorialController] 보스 UI 상태 변경: {active}");
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

    /// <summary>
    /// 지정된 게임 오브젝트의 활성화(SetActive) 상태를 껐다 켰다 토글합니다.
    /// 유니티 인스펙터의 UnityEvent에서 트리거용으로 등록하여 사용하기 편리합니다.
    /// </summary>
    /// <param name="target">토글할 대상 게임 오브젝트</param>
    public void ToggleGameObject(GameObject target)
    {
        if (target != null)
        {
            bool isCurrentlyActive = target.activeSelf;
            target.SetActive(!isCurrentlyActive);
            Debug.Log($"🔌 [TutorialController] 오브젝트 '{target.name}'의 상태를 토글했습니다: {isCurrentlyActive} -> {!isCurrentlyActive}");
        }
        else
        {
            Debug.LogWarning("⚠️ [TutorialController] 토글하려는 대상 GameObject가 null입니다.");
        }
    }
}

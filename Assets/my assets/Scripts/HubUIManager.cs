using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class HologramMission
{
    [Tooltip("미션 고유 ID (식별자)")]
    public string missionId;
    [Tooltip("미션 카드에 표시될 한글 제목")]
    public string title;
    [TextArea(3, 5)]
    [Tooltip("미션 상세 설명 지문")]
    public string description;
    [Tooltip("보스 일러스트 이미지 스프라이트")]
    public Sprite bossIllustration;
    
    [Header("=== 미션 상태 플래그 ===")]
    public bool isAccepted = false;
    public bool isCompleted = false;
}

/// <summary>
/// 허브(마을) 씬의 UI와 상호작용을 총괄하는 매니저입니다.
/// 단순화된 지도-브리핑-차고 패널 흐름을 관리하며, 보상 및 칩/골드 재화 로직이 제거되었습니다.
/// </summary>
public class HubUIManager : MonoBehaviour
{
    public static HubUIManager instance; // 원격 완료 호출을 위한 싱글톤

    [Header("=== UI 패널 ===")]
    public GameObject garagePanel;       // 차고 (출격/스테이지 선택)
    public GameObject hologramPanel;     // 홀로그램 (스토리/통신)

    [Header("=== 월드맵 출격 시스템 ===")]
    [Tooltip("월드맵 메인 패널")]
    public GameObject worldMapPanel;
    [Tooltip("월드맵 상의 거점 노드 버튼들 (0: Tutorial, 1: Stage 1, 2: Stage 2, 3: Stage 3)")]
    public Button[] stageNodes;
    [Tooltip("각 거점의 잠금(Lock) 표시 오브젝트들")]
    public GameObject[] stageNodeLocks;
    [Tooltip("각 거점의 완료(Check) 표시 오브젝트들")]
    public GameObject[] stageNodeChecks;

    [Header("=== 월드맵 상세 브리핑창 ===")]
    [Tooltip("월드맵 브리핑 패널 오브젝트")]
    public GameObject briefingPanel;
    [Tooltip("브리핑 창의 메인 제목 Text (TMP)")]
    public TextMeshProUGUI briefingTitleText;
    [Tooltip("브리핑 창의 상세 설명 Text (TMP)")]
    public TextMeshProUGUI briefingDescText;
    [Tooltip("브리핑 창의 보스 일러스트 Image")]
    public Image briefingBossImage;
    [Tooltip("월드맵 행동 버튼 (추적)")]
    public Button worldMapActionButton;
    [Tooltip("행동 버튼의 텍스트 컴포넌트 (TMP)")]
    public TextMeshProUGUI worldMapActionButtonText;
    [Tooltip("월드맵 행동 포기 버튼")]
    public Button abandonButton;
    [Tooltip("포기 버튼의 텍스트 컴포넌트 (TMP)")]
    public TextMeshProUGUI abandonButtonText;
    
    [Header("=== 우측 미션 HUD ===")]
    [Tooltip("우측 화면에 띄울 미션 알림판 패널")]
    public GameObject hubMissionPanel;   
    [Tooltip("미션 목표 설명이 표시될 Text (Legacy / TMP 모두 지원)")]
    public GameObject hubMissionText;          

    [Header("=== 고도화된 홀로그램 미션 보드 ===")]
    [Tooltip("유저가 인스펙터 창에서 마음껏 등록하고 디자인하는 미션 목록")]
    public List<HologramMission> hologramMissions = new List<HologramMission>()
    {
        new HologramMission { missionId = "Stage1", title = "바이러스 코어 파괴 공작 (Stage 1)", description = "감염 단계 1구역으로 진입하여 날아오는 극심한 탄막을 피하고, 바이러스 코어 보스를 완벽하게 무력화시키게나." },
        new HologramMission { missionId = "Stage2", title = "암흑 데이터 센터 돌파 (Stage 2)", description = "감염 단계 2구역의 메인 프레임을 점거하고 있는 보스 2(타이탄 크러셔)를 격퇴하고 데이터 복구를 완료하게나." },
        new HologramMission { missionId = "Stage3", title = "네트워크 중앙 심부 침투 (Stage 3)", description = "감염 최고조 상태인 3구역 중심부에 진입하여, 모든 방어 장치를 돌파하고 네트워크 바이러스 메인 컴퓨터를 정화시켜 주게나." },
        new HologramMission { missionId = "Stage4", title = "보조 시스템 가동 공작 (Stage 4)", description = "서브 네트 지연 상태의 전력 흐름을 해킹하여 서브 컨트롤 타워 전원을 가동하게나." },
        new HologramMission { missionId = "Stage5", title = "외곽 망 백업 장치 복구 (Stage 5)", description = "바이러스에 감염된 데이터 허브 통신망을 우회해 백업 서버와의 통신을 정상화시키게나." },
        new HologramMission { missionId = "Stage6", title = "중앙 통제 모듈 정화 (Stage 6)", description = "바이러스 침투의 종착지인 마스터 메인프레임 제어 노드를 해킹해 완벽한 정화를 수행하게나." }
    };

    [Header("=== 플레이어 이동 제어 ===")]
    public HubPlayerMovement playerMovement;

    [Header("=== Sound Settings (사운드 설정) ===")]
    [Tooltip("버튼 클릭 시 재생할 효과음")]
    public AudioClip buttonClickSFX;

    // 미션 수락 여부 플래그
    [HideInInspector] public bool hasAcceptedMission = false;

    [HideInInspector] public int currentSelectedStageIndex = -1;

    // 서브 매니저 인스턴스
    public HubMissionManager missionManager { get; private set; }
    public HubWorldMapManager worldMapManager { get; private set; }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : transform.position);
        }
    }

    private void Awake()
    {
        instance = this; // 싱글톤 인스턴스화

        // 인스펙터에서 미션 목록이 비어있거나 데이터가 비어있을(ID가 빈칸) 경우 기본값으로 복원하되,
        // 이미 인스펙터에 등록되어 있는 스프라이트(bossIllustration) 등의 데이터는 보존합니다.
        if (hologramMissions == null || hologramMissions.Count == 0)
        {
            // 리스트 자체가 완전히 비어있을 때만 새로 만듭니다.
            hologramMissions = new List<HologramMission>()
            {
                new HologramMission { missionId = "Stage1", title = "바이러스 코어 파괴 공작 (Stage 1)", description = "감염 단계 1구역으로 진입하여 날아오는 극심한 탄막을 피하고, 바이러스 코어 보스를 완벽하게 무력화시키게나." },
                new HologramMission { missionId = "Stage2", title = "암흑 데이터 센터 돌파 (Stage 2)", description = "감염 단계 2구역의 메인 프레임을 점거하고 있는 보스 2(타이탄 크러셔)를 격퇴하고 데이터 복구를 완료하게나." },
                new HologramMission { missionId = "Stage3", title = "네트워크 중앙 심부 침투 (Stage 3)", description = "감염 최고조 상태인 3구역 중심부에 진입하여, 모든 방어 장치를 돌파하고 네트워크 바이러스 메인 컴퓨터를 정화시켜 주게나." },
                new HologramMission { missionId = "Stage4", title = "보조 시스템 가동 공작 (Stage 4)", description = "서브 네트 지연 상태의 전력 흐름을 해킹하여 서브 컨트롤 타워 전원을 가동하게나." },
                new HologramMission { missionId = "Stage5", title = "외곽 망 백업 장치 복구 (Stage 5)", description = "바이러스에 감염된 데이터 허브 통신망을 우회해 백업 서버와의 통신을 정상화시키게나." },
                new HologramMission { missionId = "Stage6", title = "중앙 통제 모듈 정화 (Stage 6)", description = "바이러스 침투의 종착지인 마스터 메인프레임 제어 노드를 해킹해 완벽한 정화를 수행하게나." }
            };
            Debug.Log("⚠️ [HubUIManager] 인스펙터의 hologramMissions가 완전히 비어있어 기본 미션 정보로 복원했습니다!");
        }
        else
        {
            // 인스펙터에 슬롯은 존재하지만 데이터(텍스트)가 빈칸인 경우, 스프라이트(bossIllustration)는 둔 채 텍스트만 채워넣습니다.
            string[] defaultIds = { "Stage1", "Stage2", "Stage3", "Stage4", "Stage5", "Stage6" };
            string[] defaultTitles = {
                "바이러스 코어 파괴 공작 (Stage 1)",
                "암흑 데이터 센터 돌파 (Stage 2)",
                "네트워크 중앙 심부 침투 (Stage 3)",
                "보조 시스템 가동 공작 (Stage 4)",
                "외곽 망 백업 장치 복구 (Stage 5)",
                "중앙 통제 모듈 정화 (Stage 6)"
            };
            string[] defaultDescs = {
                "감염 단계 1구역으로 진입하여 날아오는 극심한 탄막을 피하고, 바이러스 코어 보스를 완벽하게 무력화시키게나.",
                "감염 단계 2구역의 메인 프레임을 점거하고 있는 보스 2(타이탄 크러셔)를 격퇴하고 데이터 복구를 완료하게나.",
                "감염 최고조 상태인 3구역 중심부에 진입하여, 모든 방어 장치를 돌파하고 네트워크 바이러스 메인 컴퓨터를 정화시켜 주게나.",
                "서브 네트 지연 상태의 전력 흐름을 해킹하여 서브 컨트롤 타워 전원을 가동하게나.",
                "바이러스에 감염된 데이터 허브 통신망을 우회해 백업 서버와의 통신을 정상화시키게나.",
                "바이러스 침투의 종착지인 마스터 메인프레임 제어 노드를 해킹해 완벽한 정화를 수행하게나."
            };

            for (int i = 0; i < hologramMissions.Count; i++)
            {
                if (i >= defaultIds.Length) break;
                
                if (string.IsNullOrEmpty(hologramMissions[i].missionId))
                {
                    hologramMissions[i].missionId = defaultIds[i];
                    hologramMissions[i].title = defaultTitles[i];
                    hologramMissions[i].description = defaultDescs[i];
                    Debug.Log($"⚠️ [HubUIManager] {i+1}번째 미션의 텍스트가 비어있어 기본 데이터로 자동 매핑했습니다 (스프라이트 유지).");
                }
            }
        }

        missionManager = new HubMissionManager(this);
        worldMapManager = new HubWorldMapManager(this);

#if UNITY_STANDALONE
        Screen.SetResolution(1920, 1200, FullScreenMode.Windowed);
#endif
    }

    private void Start()
    {
        ResolvePlayerMovement();
        CloseAllPanels();
        
        LoadMissionStates();

        hasAcceptedMission = false;
        HologramMission activeMission = null;
        if (hologramMissions != null)
        {
            foreach (var mission in hologramMissions)
            {
                if (mission.isAccepted)
                {
                    hasAcceptedMission = true;
                    activeMission = mission;
                    break;
                }
            }
        }

        if (hubMissionPanel != null)
        {
            hubMissionPanel.SetActive(hasAcceptedMission);
        }

        if (hasAcceptedMission && activeMission != null && hubMissionText != null)
        {
            SetText(hubMissionText, $"<color=#FFFF00>[진행 중인 임무]</color>\n<color=#FFFFFF>[ ]</color> {activeMission.title}");
        }

        if (FindObjectOfType<HubSceneInitializer>() == null && FindObjectOfType<HubInteractionPoint>() == null)
        {
            gameObject.AddComponent<HubSceneInitializer>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllPanels();
            SetPlayerControl(true);
        }
    }

    // ========== [ 패널 열기 및 기타 기본 제어 ] ==========

    public void OpenStageSelect()
    {
        PlaySFX(buttonClickSFX);
        CloseAllPanels();

        if (garagePanel != null)
        {
            garagePanel.SetActive(true);
        }
        SetPlayerControl(false);

        HologramMission trackedMission = null;
        if (hologramMissions != null)
        {
            foreach (var mission in hologramMissions)
            {
                if (mission.isAccepted && !mission.isCompleted)
                {
                    trackedMission = mission;
                    break;
                }
            }
        }

        if (trackedMission != null)
        {
            Debug.Log($"🚗 [차고] 차고 패널 오픈. 현재 추적 중인 스테이지: {trackedMission.title}");
        }
        else
        {
            Debug.LogWarning("⚠️ [차고] 추적 중인 스테이지가 없습니다. 홀로그램 지도에서 스테이지를 먼저 추적해 주세요!");
        }
    }

    /// <summary>
    /// 차고 패널의 '출발' 버튼을 클릭했을 때 호출되며, 현재 추적 중인 스테이지로 이동합니다.
    /// </summary>
    public void StartTrackedStage()
    {
        PlaySFX(buttonClickSFX);

        HologramMission trackedMission = null;
        if (hologramMissions != null)
        {
            foreach (var mission in hologramMissions)
            {
                if (mission.isAccepted)
                {
                    trackedMission = mission;
                    break;
                }
            }
        }

        if (trackedMission != null)
        {
            Debug.Log($"🚀 [차고 출발] 추적 중인 스테이지 '{trackedMission.title}' (ID: {trackedMission.missionId})로 출격합니다!");
            CloseAllPanels();

            if (trackedMission.missionId == "Tutorial") StartTutorial();
            else if (trackedMission.missionId == "Stage1") StartStageOne();
            else if (trackedMission.missionId == "Stage2") StartStageTwo();
            else if (trackedMission.missionId == "Stage3") StartStageThree();
            else if (trackedMission.missionId == "Stage4") StartStageThree();
            else if (trackedMission.missionId == "Stage5") StartStageThree();
            else if (trackedMission.missionId == "Stage6") StartStageThree();
        }
        else
        {
            Debug.LogWarning("⚠️ [차고 출발] 추적 중인 스테이지가 없습니다! 홀로그램 지도에서 스테이지를 먼저 선택해 주세요.");
        }
    }

    public void OpenHologram()
    {
        PlaySFX(buttonClickSFX);
        CloseAllPanels();
        
        // 홀로그램 지도를 엽니다.
        if (hologramPanel != null) hologramPanel.SetActive(true);
        SetPlayerControl(false);

        // 지도 상의 거점 노드 UI들을 갱신하고 연동합니다.
        OpenWorldMap();
    }

    public void OpenShop()
    {
        Debug.Log("🔧 [작업대] 현재 작업대(업그레이드) 시스템은 비활성화 상태입니다.");
    }

    public void CloseAllPanels()
    {
        PlaySFX(buttonClickSFX);
        if (garagePanel != null) garagePanel.SetActive(false);
        if (hologramPanel != null) hologramPanel.SetActive(false);
        if (worldMapPanel != null) worldMapPanel.SetActive(false);
        if (briefingPanel != null) briefingPanel.SetActive(false);
        SetPlayerControl(true);
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu_Sc");
    }

    public void SetPlayerControl(bool canMove)
    {
        ResolvePlayerMovement();
        if (playerMovement != null)
        {
            playerMovement.canMove = canMove;
        }
    }

    private void ResolvePlayerMovement()
    {
        if (playerMovement != null) return;
        playerMovement = FindObjectOfType<HubPlayerMovement>();
    }

    public void TriggerFadeTransition(string sceneName)
    {
        GameObject transitionCanvas = new GameObject("HubFadeTransitionCanvas");
        DontDestroyOnLoad(transitionCanvas);
        var runner = transitionCanvas.AddComponent<HubTransitionRunner>();
        runner.StartCoroutine(runner.Run(sceneName));
    }

    public void SetText(GameObject obj, string value)
    {
        if (obj == null) return;
        var legacy = obj.GetComponent<Text>();
        if (legacy != null)
        {
            legacy.text = value;
            return;
        }
        foreach (var comp in obj.GetComponents<Component>())
        {
            if (comp == null) continue;
            var prop = comp.GetType().GetProperty("text");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(comp, value, null);
                return;
            }
        }
    }

    // ========== [ 차고 - 출격 ] ==========

    public void StartMission()
    {
        PlaySFX(buttonClickSFX);
        Debug.Log("🏍️ 출격! 오토바이 이동 씬으로 전환합니다.");
        SceneManager.LoadScene("Ride_Scene");
    }

    public void StartTutorial()
    {
        PlaySFX(buttonClickSFX);
        Debug.Log("📖 튜토리얼 시작 (페이드 아웃)!");
        TriggerFadeTransition("1st_scene");
    }

    public void StartStageOne()
    {
        PlaySFX(buttonClickSFX);
        Debug.Log("🚀 1스테이지로 출격 (페이드 아웃)!");
        TriggerFadeTransition("game_Scene");
    }

    public void StartStageTwo()
    {
        PlaySFX(buttonClickSFX);
        Debug.Log("🚀 2스테이지로 출격 (페이드 아웃)!");
        TriggerFadeTransition("Stage2_Scene");
    }

    public void StartStageThree()
    {
        PlaySFX(buttonClickSFX);
        Debug.Log("🚀 3스테이지로 출격 (페이드 아웃)!");
        TriggerFadeTransition("Stage3_Scene");
    }

    // ========== [ 위임용 프록시 함수들 (Delegate Proxies) ] ==========

    // 1. 미션 매니저 (HubMissionManager) 위임
    public void CompleteHoloMission(string missionId) => missionManager.CompleteHoloMission(missionId);
    public void LoadMissionStates() => missionManager?.LoadMissionStates();
    public void SaveMissionStates() => missionManager?.SaveMissionStates();
    public void SetHubMissionText(string newMissionDescription) => missionManager.SetHubMissionText(newMissionDescription);
    public void TrackSelectedStage() => missionManager.TrackSelectedStage();
    public void AbandonSelectedStage() => missionManager.AbandonSelectedStage();

    // 2. 월드맵 매니저 (HubWorldMapManager) 위임
    public void OpenWorldMap() => worldMapManager.OpenWorldMap();
    public bool IsStageUnlocked(int stageIndex) => worldMapManager.IsStageUnlocked(stageIndex);
    public void SelectStageNode(int stageIndex) => worldMapManager.SelectStageNode(stageIndex);
    public void RefreshWorldMapNodes() => worldMapManager.RefreshWorldMapNodes();
    public void CloseBriefingAndOpenWorldMap() => worldMapManager.CloseBriefingAndOpenWorldMap();
}

// 씬 전환 도중 파괴되지 않고 화면을 부드럽게 페이드 아웃/인 해주는 도우미 컴포넌트
public class HubTransitionRunner : MonoBehaviour
{
    public System.Collections.IEnumerator Run(string sceneName)
    {
        Time.timeScale = 1f;

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject blackImageObj = new GameObject("BlackImage");
        blackImageObj.transform.SetParent(transform, false);
        UnityEngine.UI.Image blackImg = blackImageObj.AddComponent<UnityEngine.UI.Image>();
        blackImg.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rect = blackImg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.one;

        // 1.0초 동안 페이드 아웃 (화면이 서서히 검어짐)
        float timer = 0f;
        float fadeDuration = 1.0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            if (blackImg != null) blackImg.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        if (blackImg != null) blackImg.color = new Color(0f, 0f, 0f, 1f);

        // 비동기로 다음 씬 로드
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 새로운 씬에 자체 페이드 효과 담당자(GameTransitionManager)가 있는지 체크
        GameTransitionManager newSceneManager = FindObjectOfType<GameTransitionManager>();
        if (newSceneManager != null)
        {
            // 새 씬 매니저가 직접 페이드인 연출을 진행하도록 맡기고 소멸
            Destroy(gameObject);
            yield break;
        }

        // 자체 페이드 효과가 없는 씬일 경우 직접 1초간 페이드인 진행
        timer = 0f;
        while (timer < fadeDuration)
        {
            if (blackImg == null) break;
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            blackImg.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}

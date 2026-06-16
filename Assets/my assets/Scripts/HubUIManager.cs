using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public enum RewardType
{
    Chips,
    Gold
}

[System.Serializable]
public class RewardItem
{
    [Tooltip("보상 종류 (Chips / Gold)")]
    public RewardType type;
    [Tooltip("보상 수량")]
    public int amount;
}

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
    [Tooltip("미션 완료 시 지급할 업그레이드 칩 개수")]
    public int rewardChips = 10;
    
    [Header("=== ZZZ 스타일 추가 데이터 ===")]
    [Tooltip("의뢰 카테고리 (예: 메인 챕터, 작전, 이벤트, 도시)")]
    public string category = "작전";
    [TextArea(2, 4)]
    [Tooltip("미션 단계별 상세 목표 (체크리스트용)")]
    public string subGoal = "목표를 달성하세요.";
    [Tooltip("의뢰 상세 패널용 고해상도 대표 이미지")]
    public Sprite missionBanner;
    
    [Header("=== 미션 상태 플래그 ===")]
    public bool isAccepted = false;
    public bool isCompleted = false;
    public bool isRewardClaimed = false;

    [Header("=== 다중 보상 테이블 ===")]
    [Tooltip("미션 완료 시 지급할 여러 재화 목록 (비어 있으면 기존 rewardChips 지급)")]
    public List<RewardItem> rewards = new List<RewardItem>();
}

/// <summary>
/// 허브(마을) 씬의 UI와 상호작용을 총괄하는 매니저입니다.
/// 차고(출격), 홀로그램(통신), 작업실(업그레이드) 3개의 패널을 관리합니다.
/// ZZZ 스타일의 미션 보드를 동적으로 생성 및 제어할 수 있도록 개편되었습니다.
/// </summary>
public class HubUIManager : MonoBehaviour
{
    public static HubUIManager instance; // 원격 완료 호출을 위한 싱글톤

    [Header("=== UI 패널 ===")]
    public GameObject garagePanel;       // 차고 (출격/스테이지 선택)
    public GameObject hologramPanel;     // 홀로그램 (스토리/통신)
    public GameObject workshopPanel;     // 작업실 (업그레이드)

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
    [Tooltip("브리핑 창의 메인 제목 Text (TMP)")]
    public TextMeshProUGUI briefingTitleText;
    [Tooltip("브리핑 창의 상세 설명 Text (TMP)")]
    public TextMeshProUGUI briefingDescText;
    [Tooltip("브리핑 창의 보상 표시 Text (TMP)")]
    public TextMeshProUGUI briefingRewardText;
    [Tooltip("월드맵 행동 버튼 (수락 / 출격 / 보상수령 / 완료됨)")]
    public Button worldMapActionButton;
    [Tooltip("행동 버튼의 텍스트 컴포넌트 (TMP)")]
    public TextMeshProUGUI worldMapActionButtonText;
    
    [Header("=== 우측 미션 HUD ===")]
    [Tooltip("우측 화면에 띄울 미션 알림판 패널")]
    public GameObject hubMissionPanel;   
    [Tooltip("미션 목표 설명이 표시될 Text (Legacy / TMP 모두 지원)")]
    public GameObject hubMissionText;          

    [Header("=== 홀로그램 NPC 대화창 (기본형 레거시) ===")]
    [Tooltip("홀로그램 NPC 대화 내용 텍스트 (Legacy / TMP 모두 지원)")]
    public GameObject hologramDialogueText;   
    [Tooltip("미션 수락 버튼 오브젝트")]
    public GameObject acceptMissionButton; 

    [Header("=== 고도화된 홀로그램 미션 보드 ===")]
    [Tooltip("유저가 인스펙터 창에서 마음껏 등록하고 디자인하는 미션 목록")]
    public List<HologramMission> hologramMissions = new List<HologramMission>()
    {
        new HologramMission { missionId = "Tutorial", title = "신입 파일럿 조작 훈련", description = "훈련용 구역으로 차고를 통해 출격하여, 더미 봇의 방어망을 해킹하고 조작법을 완전히 마스터하게나.", rewardChips = 5, category = "이벤트", subGoal = "이아스의 새로운 「정보」 듣기" },
        new HologramMission { missionId = "Stage1", title = "바이러스 코어 파괴 공작", description = "감염 단계 1구역으로 진입하여 날아오는 극심한 탄막을 피하고, 바이러스 코어 보스를 완벽하게 무력화시키게나.", rewardChips = 15, category = "메인 챕터", subGoal = "제3장 위험한 건물에서의 수색" },
        new HologramMission { missionId = "Stage2", title = "암흑 데이터 센터 돌파", description = "감염 단계 2구역의 메인 프레임을 점거하고 있는 보스 2(타이탄 크러셔)를 격퇴하고 데이터 복구를 완료하게나.", rewardChips = 25, category = "메인 챕터", subGoal = "Stage 2 보스 격퇴 및 시스템 복구" },
        new HologramMission { missionId = "Stage3", title = "네트워크 중앙 심부 침투", description = "감염 최고조 상태인 3구역 중심부에 진입하여, 모든 방어 장치를 돌파하고 네트워크 바이러스 메인 컴퓨터를 정화시켜 주게나.", rewardChips = 30, category = "메인 챕터", subGoal = "Stage 3 최종 보스 격퇴 및 시스템 정화" }
    };

    [Header("=== 미션 보드 전용 UI 슬롯 (기본 레거시 호환) ===")]
    [Tooltip("미션 카드 제목 Text (Legacy / TMP)")]
    public GameObject holoMissionTitleText;
    [Tooltip("미션 보상 칩 개수 표시 Text (Legacy / TMP)")]
    public GameObject holoMissionRewardText;
    [Tooltip("수락 버튼 내부의 글자 텍스트 오브젝트 (비어있으면 수락버튼 자체 변경)")]
    public GameObject holoAcceptButtonText;
    [Tooltip("이전 미션 카드 보기 버튼")]
    public GameObject prevMissionButton;
    [Tooltip("다음 미션 카드 보기 버튼")]
    public GameObject nextMissionButton;
    [Tooltip("미션 카드 대표 이미지 배너")]
    public Image holoMissionBannerImage;

    [Header("=== ZZZ 스타일 미션 보드 전용 UI 슬롯 (추가) ===")]
    [Tooltip("의뢰 상세 뷰 대표 이미지 배너")]
    public Image zzzMissionBannerImage;
    [Tooltip("의뢰 상세 뷰 서브 목표 단계 Text (Legacy / TMP)")]
    public GameObject zzzSubGoalText;
    [Tooltip("의뢰 상세 뷰 본문 설명 Text (Legacy / TMP)")]
    public GameObject zzzDescriptionText;
    [Tooltip("의뢰 상세 뷰 메인 제목 Text (Legacy / TMP)")]
    public GameObject zzzMissionTitleText;
    [Tooltip("의뢰 상세 뷰 보상 칩 수량 Text (Legacy / TMP)")]
    public GameObject zzzRewardChipsText;
    [Tooltip("통합 추적/수락/수령 버튼")]
    public GameObject zzzTrackButton;
    [Tooltip("통합 버튼 내부의 글자 텍스트 오브젝트 (Legacy / TMP)")]
    public GameObject zzzTrackButtonText;
    [Tooltip("우측 미션 리스트 스크롤 뷰 Content 트랜스폼")]
    public Transform zzzMissionListContainer;
    [Tooltip("우측 미션 리스트에 생성할 프리팹")]
    public GameObject zzzMissionListItemPrefab;

    [Header("=== 상단 HUD ===")]
    [Tooltip("화면 상단에 칩 보유량을 표시할 Text (Legacy / TMP 모두 지원)")]
    public GameObject chipCountText;
    [Tooltip("화면 상단에 골드 보유량을 표시할 Text (Legacy / TMP 모두 지원)")]
    public GameObject goldCountText;

    [Header("=== 작업실(업그레이드) UI (Legacy / TMP 모두 지원) ===")]
    public GameObject speedLevelText;
    public GameObject energyLevelText;
    public GameObject regenLevelText;
    public GameObject dashCostLevelText;
    public GameObject hpLevelText;

    [Header("=== 업그레이드 비용 설정 ===")]
    [Tooltip("레벨당 필요 칩 수 (레벨 * 이 값)")]
    public int chipCostPerLevel = 3;

    [Header("=== 플레이어 이동 제어 ===")]
    public HubPlayerMovement playerMovement;

    [Header("=== Sound Settings (사운드 설정) ===")]
    [Tooltip("버튼 클릭 시 재생할 효과음")]
    public AudioClip buttonClickSFX;

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : transform.position);
        }
    }

    // 미션 수락 여부 플래그
    private bool hasAcceptedMission = false;
    private int currentHoloMissionIndex = 0;

    // ZZZ 스타일 상태 변수
    private HologramMission currentSelectedMission = null;


    private void Awake()
    {
        instance = this; // 싱글톤 인스턴스화

#if UNITY_STANDALONE
        Screen.SetResolution(1920, 1200, FullScreenMode.Windowed);
#endif
    }

    private void Start()
    {
        // 🔧 [스마트 인스펙터 자동 보정 시스템]
        // 1. 만약 Accept Mission Button 슬롯이 비어있고 Holo Accept Button Text에 Button 본체가 들어갔다면 교정
        if (acceptMissionButton == null && holoAcceptButtonText != null)
        {
            if (holoAcceptButtonText.GetComponent<Button>() != null)
            {
                acceptMissionButton = holoAcceptButtonText;
                Debug.Log("🔧 [자동 보정] Accept Mission Button 슬롯이 비어있어 Holo Accept Button Text 오브젝트로 자동 할당했습니다.");
            }
        }

        // 2. 만약 Holo Accept Button Text 슬롯에 자식이 아닌 Button 본체가 할당되어 있다면 자식의 Text 오브젝트를 찾아서 교정
        if (holoAcceptButtonText != null)
        {
            if (holoAcceptButtonText.GetComponent<Button>() != null)
            {
                var childText = holoAcceptButtonText.GetComponentInChildren<Text>();
                if (childText != null)
                {
                    holoAcceptButtonText = childText.gameObject;
                    Debug.Log("🔧 [자동 보정] Holo Accept Button Text가 Button 본체로 지정되어 있어, 자식의 Text 오브젝트로 자동 변경했습니다.");
                }
                else
                {
                    // TMPro 또는 기타 텍스트 컴포넌트가 자식에 있는지 검색 및 보정
                    foreach (Transform child in holoAcceptButtonText.transform)
                    {
                        foreach (var comp in child.GetComponents<Component>())
                        {
                            if (comp == null) continue;
                            var prop = comp.GetType().GetProperty("text");
                            if (prop != null)
                            {
                                holoAcceptButtonText = child.gameObject;
                                Debug.Log($"🔧 [자동 보정] 자식 오브젝트 '{child.name}'에서 텍스트 속성을 발견하여 연동했습니다.");
                                break;
                            }
                        }
                    }
                }
            }
        }

        ResolvePlayerMovement();
        CloseAllPanels();
        
        // 시작 시 저장된 모든 미션 진행 상태를 로컬에서 복구합니다.
        LoadMissionStates();

        // 수락되었으나 보상까지 완전히 수령하지 않은 미션이 있다면 우측 HUD 미션 보드 활성화
        hasAcceptedMission = false;
        HologramMission activeMission = null;
        if (hologramMissions != null)
        {
            foreach (var mission in hologramMissions)
            {
                if (mission.isAccepted && !mission.isRewardClaimed)
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
            if (activeMission.isCompleted)
            {
                SetText(hubMissionText, $"<color=#50FF50>[V] {activeMission.title} (보상 대기)</color>");
            }
            else
            {
                SetText(hubMissionText, $"<color=#FFFF00>[진행 중인 임무]</color>\n<color=#FFFFFF>[ ]</color> {activeMission.title}");
            }
        }

        // 씬 내에 상호작용 포인트가 전혀 없는 상태일 때,
        // 자동으로 HubSceneInitializer 컴포넌트를 장착하여 기본 포인트들을 자동 생성하도록 합니다.
        if (FindObjectOfType<HubSceneInitializer>() == null && FindObjectOfType<HubInteractionPoint>() == null)
        {
            gameObject.AddComponent<HubSceneInitializer>();
        }
    }

    private void Update()
    {
        // ESC로 열린 패널 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllPanels();
            SetPlayerControl(true);
        }

        // 칩 보유량 실시간 표시 (실시간 연동 복원)
        if (chipCountText != null && PlayerDataManager.instance != null)
        {
            SetText(chipCountText, "CHIP: " + PlayerDataManager.instance.upgradeChips);
        }

        // 골드 보유량 실시간 표시 (전시용 99,999 고정)
        if (goldCountText != null)
        {
            SetText(goldCountText, "GOLD: 99,999");
        }

        // 작업실(강화 상점)이 열려있고 R 키를 누르면 업그레이드 전체 초기화 및 칩 환불!
        if (workshopPanel != null && workshopPanel.activeSelf && Input.GetKeyDown(KeyCode.R))
        {
            ResetUpgradesAndRefund();
        }
    }

    // ========== [ 패널 열기 ] ==========

    public void OpenStageSelect()
    {
        PlaySFX(buttonClickSFX);
        CloseAllPanels();
        if (garagePanel != null) garagePanel.SetActive(true);
        SetPlayerControl(false);
    }

    public void OpenHologram()
    {
        PlaySFX(buttonClickSFX);
        CloseAllPanels();
        if (hologramPanel != null) hologramPanel.SetActive(true);
        SetPlayerControl(false);
        
        // ZZZ 스타일 목록이 세팅되어 있는 경우 동적 목록 생성 진행
        if (zzzMissionListContainer != null && zzzMissionListItemPrefab != null)
        {
            PopulateMissionList();
        }
        else
        {
            currentHoloMissionIndex = 0; // 항상 첫 번째 미션부터 연출 시작
            DisplayHoloMission();
        }
    }

    /// <summary>
    /// ZZZ 스타일: 모든 미션 리스트를 우측 스크롤 뷰에 동적으로 생성합니다.
    /// </summary>
    public void PopulateMissionList()
    {
        if (zzzMissionListContainer == null || zzzMissionListItemPrefab == null) return;

        // 1. 기존 리스트 오브젝트 전부 파괴
        foreach (Transform child in zzzMissionListContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 미션 목록을 동적으로 인스턴스화
        HologramMission firstMission = null;
        for (int i = 0; i < hologramMissions.Count; i++)
        {
            HologramMission mission = hologramMissions[i];
            if (firstMission == null) firstMission = mission;

            GameObject itemObj = Instantiate(zzzMissionListItemPrefab, zzzMissionListContainer);
            
            // 리스트 아이템 UI 바인딩
            SetupListItemUI(itemObj, mission);

            // 클릭 이벤트 연동
            var btn = itemObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => {
                    SelectZZZMission(mission);
                });
            }
        }

        // 3. 첫 번째 미션을 기본 선택 처리
        if (firstMission != null)
        {
            SelectZZZMission(firstMission);
        }
        else
        {
            // 미션이 없으면 좌측 뷰 비우기
            ClearZZZDetailView();
        }
    }

    private void SetupListItemUI(GameObject itemObj, HologramMission mission)
    {
        // 1. 카테고리 텍스트 설정 (예: "CategoryText" 오브젝트 탐색)
        Transform catTrans = itemObj.transform.Find("CategoryText");
        if (catTrans != null)
        {
            SetText(catTrans.gameObject, $"[{mission.category}]");
        }

        // 2. 제목 텍스트 설정 (예: "TitleText" 오브젝트 탐색)
        Transform titleTrans = itemObj.transform.Find("TitleText");
        if (titleTrans != null)
        {
            SetText(titleTrans.gameObject, mission.title);
        }

        // 3. 상태 표시 텍스트 설정 (예: "StatusText" 또는 "StateText" 오브젝트 탐색)
        Transform statusTrans = itemObj.transform.Find("StatusText") ?? itemObj.transform.Find("StateText");
        if (statusTrans != null)
        {
            string statusStr = "";
            if (!mission.isAccepted && !mission.isCompleted) statusStr = "<color=#888888>[수락 대기]</color>";
            else if (mission.isAccepted && !mission.isCompleted) statusStr = "<color=#FFFF00>[진행중]</color>";
            else if (mission.isCompleted && !mission.isRewardClaimed) statusStr = "<color=#00FF00>[완료]</color>";
            else if (mission.isRewardClaimed) statusStr = "<color=#555555>[완료됨]</color>";
            
            SetText(statusTrans.gameObject, statusStr);
        }

        // 4. 경고/느낌표 아이콘 색상 및 가시성 제어 (예: "AlertIcon" 오브젝트 탐색)
        Transform alertTrans = itemObj.transform.Find("AlertIcon");
        if (alertTrans != null)
        {
            var img = alertTrans.GetComponent<Image>();
            if (img != null)
            {
                // 카테고리별 컬러 하이라이트 매칭
                if (mission.category == "메인 챕터")
                    img.color = new Color(1f, 0.2f, 0.2f, 1f); // 빨간색
                else if (mission.category == "이벤트")
                    img.color = new Color(0.2f, 1f, 0.2f, 1f); // 연두색
                else if (mission.category == "작전")
                    img.color = new Color(0.2f, 0.6f, 1f, 1f); // 하늘색
                else
                    img.color = new Color(0.8f, 0.8f, 0.8f, 1f); // 회색
            }
        }

        // 5. 하이라이트 배경 끄기 (선택 전)
        Transform highlightTrans = itemObj.transform.Find("HighlightBg");
        if (highlightTrans != null)
        {
            highlightTrans.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ZZZ 스타일: 좌측 상세 패널을 선택된 미션의 데이터로 실시간 갱신하고 우측 아이템에 하이라이트 연출을 활성화합니다.
    /// </summary>
    public void SelectZZZMission(HologramMission mission)
    {
        currentSelectedMission = mission;
        
        // 동기화: 레거시 인덱스도 동시 갱신하여 레거시 버튼 이벤트와 완벽 호환 보장
        int idx = hologramMissions.FindIndex(m => m.missionId == mission.missionId);
        if (idx != -1)
        {
            currentHoloMissionIndex = idx;
        }
        
        // 1. 좌측 상세 정보 채우기
        if (zzzMissionTitleText != null) SetText(zzzMissionTitleText, mission.title);
        if (zzzSubGoalText != null) SetText(zzzSubGoalText, mission.subGoal);
        if (zzzDescriptionText != null) SetText(zzzDescriptionText, mission.description);
        if (zzzRewardChipsText != null) SetText(zzzRewardChipsText, GetRewardText(mission));

        if (zzzMissionBannerImage != null)
        {
            if (mission.missionBanner != null)
            {
                zzzMissionBannerImage.sprite = mission.missionBanner;
                zzzMissionBannerImage.gameObject.SetActive(true);
            }
            else
            {
                zzzMissionBannerImage.gameObject.SetActive(false);
            }
        }

        // 2. 통합 버튼 상태 분기 처리
        RefreshZZZTrackButtonState();

        // 3. 우측 미션 리스트 내 하이라이트 상태 실시간 제어
        if (zzzMissionListContainer != null)
        {
            foreach (Transform child in zzzMissionListContainer)
            {
                Transform titleTrans = child.Find("TitleText");
                Transform highlightTrans = child.Find("HighlightBg");
                
                if (titleTrans != null && highlightTrans != null)
                {
                    // 제목 텍스트로 현재 선택된 미션 판별
                    var textComp = titleTrans.GetComponent<Text>();
                    string itemTitle = textComp != null ? textComp.text : "";
                    
                    if (string.IsNullOrEmpty(itemTitle))
                    {
                        // 리플렉션 헬퍼를 사용해 가져오기 시도
                        foreach (var comp in titleTrans.GetComponents<Component>())
                        {
                            if (comp == null) continue;
                            var prop = comp.GetType().GetProperty("text");
                            if (prop != null)
                            {
                                itemTitle = (string)prop.GetValue(comp, null);
                                break;
                            }
                        }
                    }

                    bool isSelected = (itemTitle == mission.title);
                    highlightTrans.gameObject.SetActive(isSelected);
                }
            }
        }
    }

    /// <summary>
    /// ZZZ 스타일: 선택된 미션의 상태에 따라 통합 버튼의 텍스트와 상호작용성을 갱신합니다.
    /// </summary>
    public void RefreshZZZTrackButtonState()
    {
        if (zzzTrackButton == null || currentSelectedMission == null) return;

        var btn = zzzTrackButton.GetComponent<Button>();
        GameObject labelObj = zzzTrackButtonText != null ? zzzTrackButtonText : zzzTrackButton;

        if (!currentSelectedMission.isAccepted && !currentSelectedMission.isCompleted)
        {
            SetText(labelObj, "수락");
            if (btn != null) btn.interactable = true;
        }
        else if (currentSelectedMission.isAccepted && !currentSelectedMission.isCompleted)
        {
            SetText(labelObj, "진행중");
            if (btn != null) btn.interactable = false;
        }
        else if (currentSelectedMission.isCompleted && !currentSelectedMission.isRewardClaimed)
        {
            SetText(labelObj, "완료 (보상 받기)");
            if (btn != null) btn.interactable = true;
        }
        else if (currentSelectedMission.isRewardClaimed)
        {
            SetText(labelObj, "완료됨");
            if (btn != null) btn.interactable = false;
        }
    }

    /// <summary>
    /// ZZZ 스타일: 통합 추적 버튼 클릭 시, 미션 상태에 맞춰 임무를 수락하거나 완료 보상 칩을 지급하고 영구 저장합니다.
    /// </summary>
    public void ExecuteZZZMissionAction()
    {
        if (currentSelectedMission == null) return;

        // 1. 임무 수락 시
        if (!currentSelectedMission.isAccepted && !currentSelectedMission.isCompleted)
        {
            currentSelectedMission.isAccepted = true;
            hasAcceptedMission = true;

            // 우측 화면 미션 HUD 활성화 및 텍스트 갱신
            if (hubMissionPanel != null)
            {
                hubMissionPanel.SetActive(true);
            }
            if (hubMissionText != null)
            {
                SetText(hubMissionText, $"<color=#FFFF00>[진행 중인 임무]</color>\n<color=#FFFFFF>[ ]</color> {currentSelectedMission.title}");
            }

            Debug.Log($"🎯 [ZZZ 임무 수락] {currentSelectedMission.title} (보상: 칩 {currentSelectedMission.rewardChips}개)");
            SaveMissionStates();
            RefreshZZZTrackButtonState();
            
            // 리스트 아이템 UI 갱신을 위해 재배치 호출
            if (zzzMissionListContainer != null && zzzMissionListItemPrefab != null)
            {
                PopulateMissionList();
            }
        }
        // 2. 보상 수령 시
        else if (currentSelectedMission.isCompleted && !currentSelectedMission.isRewardClaimed)
        {
            // 보상 지급 (다중 보상 지원, PlayerDataManager 연동 및 자동 저장)
            if (PlayerDataManager.instance != null)
            {
                currentSelectedMission.isRewardClaimed = true;

                if (currentSelectedMission.rewards != null && currentSelectedMission.rewards.Count > 0)
                {
                    foreach (var reward in currentSelectedMission.rewards)
                    {
                        if (reward.type == RewardType.Chips)
                            PlayerDataManager.instance.AddChips(reward.amount);
                        else if (reward.type == RewardType.Gold)
                            PlayerDataManager.instance.AddGold(reward.amount);
                    }
                }
                else
                {
                    PlayerDataManager.instance.AddChips(currentSelectedMission.rewardChips);
                }

                // 우측 화면 미션 HUD 갱신
                if (hubMissionText != null)
                {
                    SetText(hubMissionText, $"<color=#50FF50>[V] {currentSelectedMission.title} (수령 완료)</color>");
                }

                Debug.Log($"🎉 [ZZZ 보상 지급] {currentSelectedMission.title} 클리어 보상으로 칩 {currentSelectedMission.rewardChips}개 획득!");
                SaveMissionStates();
                RefreshZZZTrackButtonState();

                // 리스트 아이템 UI 갱신을 위해 재배치 호출
                if (zzzMissionListContainer != null && zzzMissionListItemPrefab != null)
                {
                    PopulateMissionList();
                }
            }
            else
            {
                Debug.LogError("[시스템] 에러: PlayerDataManager.instance가 존재하지 않습니다! 보상을 지급받으려면 MainMenu_Sc 씬에서부터 시작해주세요.");
            }
        }
    }

    private void ClearZZZDetailView()
    {
        currentSelectedMission = null;
        if (zzzMissionTitleText != null) SetText(zzzMissionTitleText, "새로운 의뢰 없음");
        if (zzzSubGoalText != null) SetText(zzzSubGoalText, "-");
        if (zzzDescriptionText != null) SetText(zzzDescriptionText, "현재 사용 가능한 새로운 퀘스트 및 의뢰가 없습니다.");
        if (zzzRewardChipsText != null) SetText(zzzRewardChipsText, "-");
        if (zzzMissionBannerImage != null) zzzMissionBannerImage.gameObject.SetActive(false);
        if (zzzTrackButton != null)
        {
            var btn = zzzTrackButton.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
            GameObject labelObj = zzzTrackButtonText != null ? zzzTrackButtonText : zzzTrackButton;
            SetText(labelObj, "EMPTY");
        }
    }

    /// <summary>
    /// 현재 선택된 미션 카드의 정보를 UI에 렌더링하고 상태에 맞춰 버튼 처리를 진행합니다. (레거시 카드 형식 유지)
    /// </summary>
    public void DisplayHoloMission()
    {
        if (hologramMissions == null || hologramMissions.Count == 0)
        {
            // 예외 방지: 만약 미션 목록이 비어있다면 기본 대화 출력
            if (hologramDialogueText != null)
            {
                SetText(hologramDialogueText, "미션이 존재하지 않습니다.");
            }
            if (acceptMissionButton != null) acceptMissionButton.SetActive(false);
            return;
        }

        // 인덱스 범위 초과 방지
        currentHoloMissionIndex = Mathf.Clamp(currentHoloMissionIndex, 0, hologramMissions.Count - 1);
        HologramMission mission = hologramMissions[currentHoloMissionIndex];

        // 1. 미션 기본 정보 텍스트 세팅
        if (holoMissionTitleText != null)
        {
            SetText(holoMissionTitleText, mission.title);
        }
        else
        {
            // 만약 전용 타이틀 슬롯이 없다면 대화창 윗부분에 병합 표시
            if (hologramDialogueText != null)
            {
                SetText(hologramDialogueText, $"<b><color=#00FFFF>[ {mission.title} ]</color></b>\n\n{mission.description}");
            }
        }

        // 전용 슬롯이 있을 때만 미션 본문과 보상 칩을 따로 업데이트
        if (holoMissionTitleText != null && hologramDialogueText != null)
        {
            SetText(hologramDialogueText, mission.description);
        }
        
        if (holoMissionRewardText != null)
        {
            SetText(holoMissionRewardText, $"보상: <color=#FFD700>{GetRewardText(mission)}</color>");
        }

        if (holoMissionBannerImage != null)
        {
            if (mission.missionBanner != null)
            {
                holoMissionBannerImage.sprite = mission.missionBanner;
                holoMissionBannerImage.gameObject.SetActive(true);
            }
            else
            {
                holoMissionBannerImage.gameObject.SetActive(false);
            }
        }

        // 2. 미션 상태별 버튼 가시성 및 기능 분기
        if (acceptMissionButton != null)
        {
            acceptMissionButton.SetActive(true);
            var btn = acceptMissionButton.GetComponent<Button>();
            GameObject labelObj = holoAcceptButtonText != null ? holoAcceptButtonText : acceptMissionButton;

            if (!mission.isAccepted && !mission.isCompleted)
            {
                SetText(labelObj, "수락");
                if (btn != null) btn.interactable = true;
            }
            else if (mission.isAccepted && !mission.isCompleted)
            {
                SetText(labelObj, "진행중");
                if (btn != null) btn.interactable = false;
            }
            else if (mission.isCompleted && !mission.isRewardClaimed)
            {
                SetText(labelObj, "완료 (보상 받기)");
                if (btn != null) btn.interactable = true;
            }
            else if (mission.isRewardClaimed)
            {
                SetText(labelObj, "완료됨");
                if (btn != null) btn.interactable = false;
            }
        }

        // 3. 미션 넘겨보기 (이전/다음) 버튼 활성화/비활성화 제어
        if (prevMissionButton != null)
        {
            var btn = prevMissionButton.GetComponent<Button>();
            if (btn != null) btn.interactable = (currentHoloMissionIndex > 0);
        }
        
        if (nextMissionButton != null)
        {
            var btn = nextMissionButton.GetComponent<Button>();
            if (btn != null) btn.interactable = (currentHoloMissionIndex < hologramMissions.Count - 1);
        }
    }

    // ========== [ 미션 넘겨보기 클릭 함수 ] ==========
    public void ShowNextHoloMission()
    {
        PlaySFX(buttonClickSFX);
        if (currentHoloMissionIndex < hologramMissions.Count - 1)
        {
            currentHoloMissionIndex++;
            DisplayHoloMission();
        }
    }

    public void ShowPrevHoloMission()
    {
        PlaySFX(buttonClickSFX);
        if (currentHoloMissionIndex > 0)
        {
            currentHoloMissionIndex--;
            DisplayHoloMission();
        }
    }

    // ========== [ 홀로그램 - 미션 수락 및 보상 수령 통합 처리 ] ==========
    public void AcceptMission()
    {
        if (hologramMissions == null || hologramMissions.Count == 0)
        {
            Debug.LogError("[시스템] hologramMissions 목록이 비어있거나 Null입니다!");
            return;
        }

        // ZZZ 스타일이 활성화되어 선택된 미션이 있다면 그것을 사용하고, 없다면 인덱스 기반 레거시 미션을 사용합니다.
        HologramMission mission = currentSelectedMission != null ? currentSelectedMission : hologramMissions[currentHoloMissionIndex];
        Debug.Log($"[시스템] AcceptMission() 호출됨 - 대상 미션: ID={mission.missionId}, Title={mission.title}, isAccepted={mission.isAccepted}, isCompleted={mission.isCompleted}, isRewardClaimed={mission.isRewardClaimed}");

        // 1. 임무 수락 시
        if (!mission.isAccepted && !mission.isCompleted)
        {
            mission.isAccepted = true;
            hasAcceptedMission = true;

            // 우측 미션 패널 활성화 및 텍스트 설정
            if (hubMissionPanel != null)
            {
                hubMissionPanel.SetActive(true);
                Debug.Log("[시스템] 우측 hubMissionPanel 활성화 완료!");
            }
            else
            {
                Debug.LogWarning("[시스템] 경고: 우측 hubMissionPanel 슬롯이 인스펙터에 연결되어 있지 않습니다!");
            }

            if (hubMissionText != null)
            {
                SetText(hubMissionText, $"<color=#FFFF00>[진행 중인 임무]</color>\n<color=#FFFFFF>[ ]</color> {mission.title}");
            }
            else
            {
                Debug.LogWarning("[시스템] 경고: 우측 hubMissionText 슬롯이 인스펙터에 연결되어 있지 않습니다!");
            }

            Debug.Log($"🎯 [임무 수락 완료] {mission.title} (보상: 칩 {mission.rewardChips}개)");
            SaveMissionStates();
            DisplayHoloMission();
            CloseAllPanels(); // 임무 수락 후 패널 자동 종료
        }
        // 2. 임무 완료 후 보상 수령 시
        else if (mission.isCompleted && !mission.isRewardClaimed)
        {
            // 보상 지급! (다중 보상 지원, PlayerDataManager 연동 및 자동 세이브)
            if (PlayerDataManager.instance != null)
            {
                mission.isRewardClaimed = true;

                if (mission.rewards != null && mission.rewards.Count > 0)
                {
                    foreach (var reward in mission.rewards)
                    {
                        if (reward.type == RewardType.Chips)
                            PlayerDataManager.instance.AddChips(reward.amount);
                        else if (reward.type == RewardType.Gold)
                            PlayerDataManager.instance.AddGold(reward.amount);
                    }
                }
                else
                {
                    PlayerDataManager.instance.AddChips(mission.rewardChips);
                }

                // 우측 화면 미션 완료 텍스트 갱신
                if (hubMissionText != null)
                {
                    SetText(hubMissionText, $"<color=#50FF50>[V] {mission.title} (수령 완료)</color>");
                }

                Debug.Log($"🎉 [보상 지급] {mission.title} 성공 보상으로 칩 {mission.rewardChips}개를 지급하였습니다!");
                SaveMissionStates();
                DisplayHoloMission();
                CloseAllPanels(); // 보상 수령 후 패널 자동 종료
            }
            else
            {
                Debug.LogError("[시스템] 에러: PlayerDataManager.instance가 존재하지 않습니다! 씬 내에 PlayerDataManager 프리팹을 반드시 배치해주시거나, MainMenu_Sc 씬에서부터 게임을 시작해주세요. 보상이 지급되지 않았습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"[시스템] 경고: 현재 미션 상태로는 수락/수령 조건에 해당하지 않습니다. 패널을 강제 종료합니다.");
            CloseAllPanels(); // 어떤 상태든 버튼이 눌렸다면 창을 강제로 닫음으로써 홀드 현상 방지
        }
    }

    /// <summary>
    /// 외부 게임/훈련 씬에서 미션 클리어 성공 시 이 함수를 호출하여 미션을 완료 상태로 전환합니다!
    /// (예: CompleteHoloMission("Tutorial") 또는 CompleteHoloMission("Stage1"))
    /// </summary>
    public void CompleteHoloMission(string missionId)
    {
        if (hologramMissions == null) return;
        foreach (var mission in hologramMissions)
        {
            if (mission.missionId == missionId)
            {
                mission.isCompleted = true;
                SaveMissionStates(); // 즉시 저장
                Debug.Log($"✅ [임무 성공 달성] {mission.title} 미션 클리어! 기지에서 보상을 수령할 수 있습니다.");
                
                // 만약 현재 허브 씬에 머무는 상태라면 즉시 UI 갱신
                if (hologramPanel != null && hologramPanel.activeSelf)
                {
                    if (zzzMissionListContainer != null && currentSelectedMission != null)
                    {
                        if (currentSelectedMission.missionId == missionId)
                        {
                            RefreshZZZTrackButtonState();
                        }
                    }
                    else
                    {
                        DisplayHoloMission();
                    }
                }
                break;
            }
        }
    }

    // ========== [ 미션 상태 저장 및 로드 로직 ] ==========

    public void LoadMissionStates()
    {
        if (hologramMissions == null) return;
        foreach (var mission in hologramMissions)
        {
            mission.isAccepted = PlayerPrefs.GetInt($"Mission_{mission.missionId}_Accepted", 0) == 1;
            mission.isCompleted = PlayerPrefs.GetInt($"Mission_{mission.missionId}_Completed", 0) == 1;
            mission.isRewardClaimed = PlayerPrefs.GetInt($"Mission_{mission.missionId}_RewardClaimed", 0) == 1;
        }

        // 튜토리얼 완료 상태 연동 (PlayerDataManager 및 PlayerPrefs 양방향 안전 장치)
        if (PlayerDataManager.instance != null && PlayerDataManager.instance.tutorialCompleted)
        {
            var tutMission = hologramMissions.Find(m => m.missionId == "Tutorial");
            if (tutMission != null)
            {
                tutMission.isAccepted = true;
                tutMission.isCompleted = true;
            }
        }
        else if (PlayerPrefs.GetInt("Mission_Tutorial_Completed", 0) == 1)
        {
            var tutMission = hologramMissions.Find(m => m.missionId == "Tutorial");
            if (tutMission != null)
            {
                tutMission.isAccepted = true;
                tutMission.isCompleted = true;
            }
        }

        // 스테이지 1, 2, 3 클리어 완료 연동 이중화
        if (PlayerPrefs.GetInt("Mission_Stage1_Completed", 0) == 1)
        {
            var s1Mission = hologramMissions.Find(m => m.missionId == "Stage1");
            if (s1Mission != null)
            {
                s1Mission.isAccepted = true;
                s1Mission.isCompleted = true;
            }
        }
        if (PlayerPrefs.GetInt("Mission_Stage2_Completed", 0) == 1)
        {
            var s2Mission = hologramMissions.Find(m => m.missionId == "Stage2");
            if (s2Mission != null)
            {
                s2Mission.isAccepted = true;
                s2Mission.isCompleted = true;
            }
        }
        if (PlayerPrefs.GetInt("Mission_Stage3_Completed", 0) == 1)
        {
            var s3Mission = hologramMissions.Find(m => m.missionId == "Stage3");
            if (s3Mission != null)
            {
                s3Mission.isAccepted = true;
                s3Mission.isCompleted = true;
            }
        }
    }

    public void SaveMissionStates()
    {
        if (hologramMissions == null) return;
        foreach (var mission in hologramMissions)
        {
            PlayerPrefs.SetInt($"Mission_{mission.missionId}_Accepted", mission.isAccepted ? 1 : 0);
            PlayerPrefs.SetInt($"Mission_{mission.missionId}_Completed", mission.isCompleted ? 1 : 0);
            PlayerPrefs.SetInt($"Mission_{mission.missionId}_RewardClaimed", mission.isRewardClaimed ? 1 : 0);
        }
        PlayerPrefs.Save();
        Debug.Log("💾 [미션 세이브 완료] 진행 상황이 성공적으로 저장되었습니다.");
    }

    /// <summary>
    /// 동적으로 새로운 임무 텍스트를 설정하고 우측 미션 패널을 활성화합니다.
    /// </summary>
    public void SetHubMissionText(string newMissionDescription)
    {
        hasAcceptedMission = true;

        if (hubMissionPanel != null)
        {
            hubMissionPanel.SetActive(true);
        }

        if (hubMissionText != null)
        {
            SetText(hubMissionText, $"<color=#FFFF00>[진행 중인 임무]</color>\n<color=#FFFFFF>[ ]</color> {newMissionDescription}");
        }

        Debug.Log($"🎯 [커스텀 미션 등록] {newMissionDescription}");
    }

    public void OpenShop()
    {
        PlaySFX(buttonClickSFX);
        CloseAllPanels();
        if (workshopPanel != null) workshopPanel.SetActive(true);
        SetPlayerControl(false);
        RefreshUpgradeUI();
    }


    public void CloseAllPanels()
    {
        PlaySFX(buttonClickSFX);
        if (garagePanel != null) garagePanel.SetActive(false);
        if (hologramPanel != null) hologramPanel.SetActive(false);
        if (workshopPanel != null) workshopPanel.SetActive(false);
        if (worldMapPanel != null) worldMapPanel.SetActive(false);
        SetPlayerControl(true);
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

    // ========== [ 작업실 - 업그레이드 ] ==========

    public void UpgradeSpeed()
    {
        PlaySFX(buttonClickSFX);
        TryUpgrade(ref PlayerDataManager.instance.speedLevel, "이동속도");
    }

    public void UpgradeMaxEnergy()
    {
        PlaySFX(buttonClickSFX);
        TryUpgrade(ref PlayerDataManager.instance.maxEnergyLevel, "최대 에너지");
    }

    public void UpgradeEnergyRegen()
    {
        PlaySFX(buttonClickSFX);
        TryUpgrade(ref PlayerDataManager.instance.energyRegenLevel, "에너지 재생력");
    }

    public void UpgradeDashCost()
    {
        PlaySFX(buttonClickSFX);
        TryUpgrade(ref PlayerDataManager.instance.dashCostLevel, "대쉬 소모 감소");
    }

    public void UpgradeMaxHp()
    {
        PlaySFX(buttonClickSFX);
        TryUpgrade(ref PlayerDataManager.instance.maxHpLevel, "최대 체력");
    }

    private void TryUpgrade(ref int level, string statName)
    {
        if (PlayerDataManager.instance == null) return;

        if (level >= 3)
        {
            Debug.LogWarning($"⚠️ [{statName}] 이미 최대 레벨(Lv.3)에 도달하여 더 이상 강화할 수 없습니다.");
            return;
        }

        int cost = (level + 1) * chipCostPerLevel;

        if (PlayerDataManager.instance.SpendChips(cost))
        {
            level++;
            PlayerDataManager.instance.SaveData(); // 업그레이드 완료 후 즉시 세이브 기록 갱신 및 자동 저장!
            Debug.Log($"⬆️ [{statName}] Lv.{level} 업그레이드 완료! (소모: {cost} 칩)");
            RefreshUpgradeUI();
        }
    }

    /// <summary>
    /// 모든 업그레이드를 초기화하고 소비한 칩을 100% 환불합니다. (UI 버튼 및 단축키 연동 가능)
    /// </summary>
    public void ResetUpgradesAndRefund()
    {
        if (PlayerDataManager.instance == null) return;

        int refundedChips = 0;
        
        // 각 스탯별 누적 소비 칩 환불 계산: level * (level + 1) / 2 * chipCostPerLevel
        refundedChips += (PlayerDataManager.instance.speedLevel * (PlayerDataManager.instance.speedLevel + 1) / 2) * chipCostPerLevel;
        refundedChips += (PlayerDataManager.instance.maxEnergyLevel * (PlayerDataManager.instance.maxEnergyLevel + 1) / 2) * chipCostPerLevel;
        refundedChips += (PlayerDataManager.instance.energyRegenLevel * (PlayerDataManager.instance.energyRegenLevel + 1) / 2) * chipCostPerLevel;
        refundedChips += (PlayerDataManager.instance.dashCostLevel * (PlayerDataManager.instance.dashCostLevel + 1) / 2) * chipCostPerLevel;
        refundedChips += (PlayerDataManager.instance.maxHpLevel * (PlayerDataManager.instance.maxHpLevel + 1) / 2) * chipCostPerLevel;

        // 업그레이드 레벨 리셋
        PlayerDataManager.instance.speedLevel = 0;
        PlayerDataManager.instance.maxEnergyLevel = 0;
        PlayerDataManager.instance.energyRegenLevel = 0;
        PlayerDataManager.instance.dashCostLevel = 0;
        PlayerDataManager.instance.maxHpLevel = 0;

        // 칩 복구 및 데이터 저장
        PlayerDataManager.instance.AddChips(refundedChips);
        PlayerDataManager.instance.SaveData();

        Debug.Log($"🔄 [업그레이드 초기화] 강화 레벨이 전부 초기화되었으며, 칩 {refundedChips}개가 100% 환불되었습니다.");

        // UI 즉시 갱신
        RefreshUpgradeUI();
    }

    private void RefreshUpgradeUI()
    {
        if (PlayerDataManager.instance == null) return;

        var data = PlayerDataManager.instance;
        SetStatText(speedLevelText, "이동속도", data.speedLevel);
        SetStatText(energyLevelText, "최대 에너지", data.maxEnergyLevel);
        SetStatText(regenLevelText, "에너지 재생", data.energyRegenLevel);
        SetStatText(dashCostLevelText, "대쉬 효율", data.dashCostLevel);
        SetStatText(hpLevelText, "최대 체력", data.maxHpLevel);
    }

    private void SetStatText(GameObject textObj, string name, int level)
    {
        if (textObj == null) return;
        if (level >= 3)
        {
            SetText(textObj, $"{name}  Lv.{level}  [최대 레벨 (MAX)]");
        }
        else
        {
            int nextCost = (level + 1) * chipCostPerLevel;
            SetText(textObj, $"{name}  Lv.{level}  [강화: {nextCost} 칩]");
        }
    }

    // ========== [ 기타 ] ==========

    public void QuitGame()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu_Sc");
    }

    private void SetPlayerControl(bool canMove)
    {
        ResolvePlayerMovement();

        if (playerMovement != null)
        {
            playerMovement.canMove = canMove;
        }
    }

    private void ResolvePlayerMovement()
    {
        if (playerMovement != null)
        {
            return;
        }

        playerMovement = FindObjectOfType<HubPlayerMovement>();
    }

    /// <summary>
    /// 미션의 보상 목록을 문자열로 가공하여 반환합니다. (하위 호환성 보장 및 폰트 호환성 개선)
    /// </summary>
    public string GetRewardText(HologramMission mission)
    {
        if (mission == null) return "";

        // 다중 보상 테이블이 비어있는 경우 ➔ 기존 rewardChips 기반의 칩 단일 보상으로 반환
        if (mission.rewards == null || mission.rewards.Count == 0)
        {
            return $"{mission.rewardChips} CHIPS";
        }

        // 다중 보상 테이블이 있는 경우 ➔ 각 보상을 결합하여 반환
        List<string> rewardStrings = new List<string>();
        foreach (var r in mission.rewards)
        {
            if (r.type == RewardType.Chips)
            {
                rewardStrings.Add($"{r.amount} CHIPS");
            }
            else if (r.type == RewardType.Gold)
            {
                rewardStrings.Add($"{r.amount} GOLD");
            }
        }
        return string.Join(", ", rewardStrings);
    }

    // ── 텍스트 컴포넌트 자동 판별 및 값 설정 헬퍼 함수 ──
    private void SetText(GameObject obj, string value)
    {
        if (obj == null) return;
        
        // 1. 구버전 Legacy Text인 경우
        var legacy = obj.GetComponent<Text>();
        if (legacy != null)
        {
            legacy.text = value;
            return;
        }
        
        // 2. TextMeshPro 또는 타 컴포넌트인 경우 리플렉션으로 자동 갱신
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

    // =========================================================================
    // [ 월드맵 출격 시스템 구현 영역 ]
    // =========================================================================

    private int currentSelectedStageIndex = -1;

    /// <summary>
    /// 차고나 터미널에서 상호작용 시 월드맵 패널을 엽니다.
    /// </summary>
    public void OpenWorldMap()
    {
        PlaySFX(buttonClickSFX);
        CloseAllPanels();
        
        if (worldMapPanel != null)
        {
            worldMapPanel.SetActive(true);
        }
        
        SetPlayerControl(false);
        LoadMissionStates(); // 로컬 세이브/미션 상태 연동
        RefreshWorldMapNodes(); // 월드맵 상의 노드 UI 업데이트
        
        // 기본 선택 스테이지 지정
        SelectDefaultStage();
    }

    private void SelectDefaultStage()
    {
        if (hologramMissions == null || hologramMissions.Count == 0) return;

        // 잠금 해제되어 있으며 아직 최종 보상을 수령하지 않은 첫 번째 미션 노드를 디폴트로 선택합니다.
        for (int i = 0; i < hologramMissions.Count; i++)
        {
            if (IsStageUnlocked(i) && !hologramMissions[i].isRewardClaimed)
            {
                SelectStageNode(i);
                return;
            }
        }
        
        // 만약 모든 보상을 수령했다면 가장 마지막 노드 선택
        SelectStageNode(hologramMissions.Count - 1);
    }

    /// <summary>
    /// 특정 인덱스의 스테이지가 해제(Unlock)되었는지 반환합니다.
    /// </summary>
    public bool IsStageUnlocked(int stageIndex)
    {
        if (stageIndex <= 0) return true; // 첫 번째(Tutorial)는 언제나 해제 상태
        if (hologramMissions == null || stageIndex >= hologramMissions.Count) return false;

        // 이전 스테이지의 보상까지 최종 수령(isRewardClaimed)했을 때 다음 스테이지가 잠금 해제됩니다.
        return hologramMissions[stageIndex - 1].isRewardClaimed;
    }

    /// <summary>
    /// 지도상의 스테이지 거점 마커 노드를 클릭했을 때 호출됩니다.
    /// </summary>
    public void SelectStageNode(int stageIndex)
    {
        if (hologramMissions == null || stageIndex < 0 || stageIndex >= hologramMissions.Count) return;
        
        PlaySFX(buttonClickSFX);
        currentSelectedStageIndex = stageIndex;
        HologramMission mission = hologramMissions[stageIndex];

        // 1. 브리핑 텍스트 갱신
        if (briefingTitleText != null) briefingTitleText.text = mission.title;
        if (briefingDescText != null) briefingDescText.text = mission.description;
        if (briefingRewardText != null) briefingRewardText.text = $"보상: <color=#FFD700>{GetRewardText(mission)}</color>";

        // 2. 전체 노드 상태 새로고침
        RefreshWorldMapNodes();
    }

    /// <summary>
    /// 전체 거점 마크(잠금, 완료 여부) 및 브리핑창 우측 행동 버튼의 상태를 실시간으로 새로고침합니다.
    /// </summary>
    public void RefreshWorldMapNodes()
    {
        if (hologramMissions == null || hologramMissions.Count == 0) return;

        // 1. 지도 위 노드들(버튼 상호작용, 잠금 열쇠, 완료 체크 마크) 제어
        for (int i = 0; i < stageNodes.Length; i++)
        {
            if (i >= hologramMissions.Count) break;
            if (stageNodes[i] == null) continue;

            HologramMission mission = hologramMissions[i];
            bool unlocked = IsStageUnlocked(i);

            // 잠긴 노드는 클릭 비활성화
            stageNodes[i].interactable = unlocked;

            // 자식 오브젝트의 잠금 자물쇠 아이콘 토글
            if (stageNodeLocks != null && i < stageNodeLocks.Length && stageNodeLocks[i] != null)
            {
                stageNodeLocks[i].SetActive(!unlocked);
            }

            // 자식 오브젝트의 최종 클리어 녹색 체크마크 토글 (보상 수령 완료 시 완료 체크 마크 활성화)
            if (stageNodeChecks != null && i < stageNodeChecks.Length && stageNodeChecks[i] != null)
            {
                stageNodeChecks[i].SetActive(mission.isRewardClaimed);
            }
        }

        // 2. 브리핑 상세창 우측 하단의 통합 작동 버튼 상태 제어
        if (currentSelectedStageIndex != -1 && worldMapActionButton != null)
        {
            HologramMission selectedMission = hologramMissions[currentSelectedStageIndex];
            bool isUnlocked = IsStageUnlocked(currentSelectedStageIndex);

            worldMapActionButton.gameObject.SetActive(true);

            if (!isUnlocked)
            {
                SetActionButtonState("잠김", false);
            }
            else if (!selectedMission.isAccepted && !selectedMission.isCompleted)
            {
                SetActionButtonState("의뢰 수락", true);
            }
            else if (selectedMission.isAccepted && !selectedMission.isCompleted)
            {
                SetActionButtonState("출격하기", true);
            }
            else if (selectedMission.isCompleted && !selectedMission.isRewardClaimed)
            {
                SetActionButtonState("보상 수령", true);
            }
            else if (selectedMission.isRewardClaimed)
            {
                SetActionButtonState("작전 완료", false);
            }
        }
        else if (worldMapActionButton != null)
        {
            worldMapActionButton.gameObject.SetActive(false);
        }
    }

    private void SetActionButtonState(string label, bool interactable)
    {
        if (worldMapActionButton != null)
        {
            worldMapActionButton.interactable = interactable;
        }
        
        if (worldMapActionButtonText != null)
        {
            worldMapActionButtonText.text = label;
        }
    }

    /// <summary>
    /// 월드맵 브리핑 상세창의 실행 버튼 클릭 시 호출됩니다. (의뢰 수락 ➔ 출격 ➔ 보상 수령 순환)
    /// </summary>
    public void ExecuteWorldMapAction()
    {
        if (currentSelectedStageIndex == -1 || hologramMissions == null) return;
        if (currentSelectedStageIndex >= hologramMissions.Count) return;

        PlaySFX(buttonClickSFX);
        HologramMission mission = hologramMissions[currentSelectedStageIndex];

        // ① 미션 수락
        if (!mission.isAccepted && !mission.isCompleted)
        {
            mission.isAccepted = true;
            hasAcceptedMission = true;

            // 우측 HUD 보드 활성화
            if (hubMissionPanel != null) hubMissionPanel.SetActive(true);
            if (hubMissionText != null)
            {
                SetText(hubMissionText, $"<color=#FFFF00>[진행 중인 임무]</color>\n<color=#FFFFFF>[ ]</color> {mission.title}");
            }

            Debug.Log($"🎯 [월드맵 임무 수락] {mission.title}");
            SaveMissionStates();
            RefreshWorldMapNodes();
        }
        // ② 전투 씬 출격
        else if (mission.isAccepted && !mission.isCompleted)
        {
            Debug.Log($"🏍️ [월드맵 출격 실행] {mission.title} (ID: {mission.missionId})");
            CloseAllPanels();

            if (mission.missionId == "Tutorial")
            {
                StartTutorial();
            }
            else if (mission.missionId == "Stage1")
            {
                StartStageOne();
            }
            else if (mission.missionId == "Stage2")
            {
                StartStageTwo();
            }
            else if (mission.missionId == "Stage3")
            {
                StartStageThree();
            }
        }
        // ③ 보상 수령
        else if (mission.isCompleted && !mission.isRewardClaimed)
        {
            if (PlayerDataManager.instance != null)
            {
                mission.isRewardClaimed = true;

                // 보상 처리
                if (mission.rewards != null && mission.rewards.Count > 0)
                {
                    foreach (var reward in mission.rewards)
                    {
                        if (reward.type == RewardType.Chips)
                            PlayerDataManager.instance.AddChips(reward.amount);
                        else if (reward.type == RewardType.Gold)
                            PlayerDataManager.instance.AddGold(reward.amount);
                    }
                }
                else
                {
                    PlayerDataManager.instance.AddChips(mission.rewardChips);
                }

                // 우측 HUD 완료 처리
                if (hubMissionText != null)
                {
                    SetText(hubMissionText, $"<color=#50FF50>[V] {mission.title} (수령 완료)</color>");
                }

                Debug.Log($"🎉 [월드맵 보상 지급] {mission.title} 클리어 보상 칩 {mission.rewardChips}개 지급 완료!");
                SaveMissionStates();
                RefreshWorldMapNodes();
            }
            else
            {
                Debug.LogError("⚠️ [시스템 에러] PlayerDataManager가 씬에 존재하지 않아 보상을 줄 수 없습니다!");
            }
        }
    }

    // 지정한 씬으로 페이드아웃 연출을 거쳐 로드합니다.
    public void TriggerFadeTransition(string sceneName)
    {
        GameObject transitionCanvas = new GameObject("HubFadeTransitionCanvas");
        DontDestroyOnLoad(transitionCanvas);
        
        var runner = transitionCanvas.AddComponent<HubTransitionRunner>();
        runner.StartCoroutine(runner.Run(sceneName));
    }
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

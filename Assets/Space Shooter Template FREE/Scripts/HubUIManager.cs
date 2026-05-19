using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 허브(마을) 씬의 UI와 상호작용을 총괄하는 매니저입니다.
/// 차고(출격), 홀로그램(통신), 작업실(업그레이드) 3개의 패널을 관리합니다.
/// </summary>
public class HubUIManager : MonoBehaviour
{
    [Header("=== UI 패널 ===")]
    public GameObject garagePanel;       // 차고 (출격/스테이지 선택)
    public GameObject hologramPanel;     // 홀로그램 (스토리/통신)
    public GameObject workshopPanel;     // 작업실 (업그레이드)
    
    [Header("=== 우측 미션 HUD ===")]
    [Tooltip("우측 화면에 띄울 미션 알림판 패널")]
    public GameObject hubMissionPanel;   
    [Tooltip("미션 목표 설명이 표시될 Text (Legacy / TMP 모두 지원)")]
    public GameObject hubMissionText;          

    [Header("=== 홀로그램 NPC 대화창 ===")]
    [Tooltip("홀로그램 NPC 대화 내용 텍스트 (Legacy / TMP 모두 지원)")]
    public GameObject hologramDialogueText;   
    [Tooltip("미션 수락 버튼 오브젝트")]
    public GameObject acceptMissionButton; 

    [Header("=== 상단 HUD ===")]
    [Tooltip("화면 상단에 칩 보유량을 표시할 Text (Legacy / TMP 모두 지원)")]
    public GameObject chipCountText;

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

    // 미션 수락 여부 플래그
    private bool hasAcceptedMission = false;

    private void Awake()
    {
#if UNITY_STANDALONE
        Screen.SetResolution(1920, 1200, FullScreenMode.Windowed);
#endif
    }

    private void Start()
    {
        CloseAllPanels();
        
        // 시작 시 미션을 수락하지 않았다면 우측 미션 알림판을 꺼둡니다.
        if (hubMissionPanel != null)
        {
            hubMissionPanel.SetActive(hasAcceptedMission);
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

        // 칩 보유량 실시간 표시
        if (chipCountText != null && PlayerDataManager.instance != null)
        {
            SetText(chipCountText, "CHIP: " + PlayerDataManager.instance.upgradeChips);
        }
    }

    // ========== [ 패널 열기 ] ==========

    public void OpenStageSelect()
    {
        CloseAllPanels();
        if (garagePanel != null) garagePanel.SetActive(true);
        SetPlayerControl(false);
    }

    public void OpenHologram()
    {
        CloseAllPanels();
        if (hologramPanel != null) hologramPanel.SetActive(true);
        SetPlayerControl(false);
        
        // 미션 수락 여부에 따른 대화 지문 분기 및 수락 버튼 상태 설정
        if (hologramDialogueText != null)
        {
            if (!hasAcceptedMission)
            {
                SetText(hologramDialogueText, "반갑군, 비행사여.\n금지된 구역의 에너지가 폭주하고 있네.\n바이러스 감염 구역에서 '업그레이드 칩'을 찾아 기체를 강화하고 세이프존을 확보해 주게나.");
                if (acceptMissionButton != null) acceptMissionButton.SetActive(true);
            }
            else
            {
                SetText(hologramDialogueText, "감염 구역의 바이러스 코어(보스)를 격퇴하고 오게나. 인류의 운명이 그대에게 달렸네.");
                if (acceptMissionButton != null) acceptMissionButton.SetActive(false);
            }
        }
    }

    // ========== [ 홀로그램 - 미션 수락 ] ==========
    public void AcceptMission()
    {
        hasAcceptedMission = true;
        
        // 1. 대화창 지문 변경
        if (hologramDialogueText != null)
        {
            SetText(hologramDialogueText, "고맙네! 감염 구역의 바이러스 코어(보스)를 격퇴하고 오게나. 인류의 운명이 그대에게 달렸네.");
        }
        
        // 2. 수락 버튼 숨기기
        if (acceptMissionButton != null)
        {
            acceptMissionButton.SetActive(false);
        }
        
        // 3. 우측 미션 패널 활성화 및 텍스트 갱신
        if (hubMissionPanel != null)
        {
            hubMissionPanel.SetActive(true);
        }
        
        if (hubMissionText != null)
        {
            SetText(hubMissionText, "<color=#FFFF00>[진행 중인 임무]</color>\n<color=#FFFFFF>[ ]</color> 차고에서 1스테이지 출격하기");
        }
        
        Debug.Log("🎯 [미션 수락] 바이러스 코어 격퇴 미션을 받았습니다.");
    }

    public void OpenShop()
    {
        CloseAllPanels();
        if (workshopPanel != null) workshopPanel.SetActive(true);
        SetPlayerControl(false);
        RefreshUpgradeUI();
    }

    public void CloseAllPanels()
    {
        if (garagePanel != null) garagePanel.SetActive(false);
        if (hologramPanel != null) hologramPanel.SetActive(false);
        if (workshopPanel != null) workshopPanel.SetActive(false);
    }

    // ========== [ 차고 - 출격 ] ==========

    public void StartMission()
    {
        Debug.Log("🏍️ 출격! 오토바이 이동 씬으로 전환합니다.");
        SceneManager.LoadScene("Ride_Scene");
    }

    public void StartTutorial()
    {
        Debug.Log("📖 튜토리얼 시작!");
        SceneManager.LoadScene("Tutorial_scene");
    }

    public void StartStageOne()
    {
        Debug.Log("🚀 1스테이지로 출격!");
        SceneManager.LoadScene("game_Scene");
    }

    // ========== [ 작업실 - 업그레이드 ] ==========

    public void UpgradeSpeed()
    {
        TryUpgrade(ref PlayerDataManager.instance.speedLevel, "이동속도");
    }

    public void UpgradeMaxEnergy()
    {
        TryUpgrade(ref PlayerDataManager.instance.maxEnergyLevel, "최대 에너지");
    }

    public void UpgradeEnergyRegen()
    {
        TryUpgrade(ref PlayerDataManager.instance.energyRegenLevel, "에너지 재생력");
    }

    public void UpgradeDashCost()
    {
        TryUpgrade(ref PlayerDataManager.instance.dashCostLevel, "대쉬 소모 감소");
    }

    public void UpgradeMaxHp()
    {
        TryUpgrade(ref PlayerDataManager.instance.maxHpLevel, "최대 체력");
    }

    private void TryUpgrade(ref int level, string statName)
    {
        if (PlayerDataManager.instance == null) return;

        int cost = (level + 1) * chipCostPerLevel;

        if (PlayerDataManager.instance.SpendChips(cost))
        {
            level++;
            Debug.Log($"⬆️ [{statName}] Lv.{level} 업그레이드 완료! (소모: {cost} 칩)");
            RefreshUpgradeUI();
        }
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
        int nextCost = (level + 1) * chipCostPerLevel;
        SetText(textObj, $"{name}  Lv.{level}  [강화: {nextCost} 칩]");
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
        if (playerMovement != null)
        {
            playerMovement.canMove = canMove;
        }
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
}

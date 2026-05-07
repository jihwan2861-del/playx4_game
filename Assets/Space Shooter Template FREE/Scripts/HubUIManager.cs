using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 마을(허브) 씬에서 터미널 메뉴 UI를 관리하는 매니저입니다.
/// </summary>
public class HubUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;      // 최상단 메뉴 (임무출격, 기체정비 등)
    public GameObject stageSelectPanel;   // 스테이지 선택 창
    public GameObject shopPanel;          // 상점/업그레이드 창
    public GameObject settingsPanel;      // 설정(볼륨) 창

    [Header("Player & NPC Reference")]
    public PlayerVillageMoving playerMovement;   // 메뉴 켜졌을 때 플레이어 못 움직이게 제어
    public Transform missionNPC;          // 임무 NPC의 위치
    public float autoWalkSpeed = 5f;      // NPC로 걸어가는 속도

    private void Awake()
    {
        // PC 빌드(exe 파일) 실행 시 해상도를 강제로 1920 x 1200 창모드로 고정합니다.
#if UNITY_STANDALONE
        Screen.SetResolution(1920, 1200, FullScreenMode.Windowed);
#endif
    }

    private void Start()
    {
        // 처음 시작할 때 메인 메뉴만 켜지게 세팅
        OpenMainMenu();
    }

    // ========== [ 메뉴 열기 / 닫기 ] ==========

    public void OpenMainMenu()
    {
        CloseAllPanels();
        mainMenuPanel.SetActive(true);
        SetPlayerControl(false); // 메뉴 열려있으면 이동 불가
    }

    // 임무 시작 버튼을 눌렀을 때 (NPC로 걸어가기 시작)
    public void OnClickStartMission()
    {
        CloseAllPanels(); // 일단 메뉴 다 끄고
        
        if (playerMovement == null)
        {
            Debug.LogWarning("⚠️ [에러] HubManager의 'Player Movement' 빈칸이 비어있습니다! 씬의 Player를 넣어주세요.");
            return;
        }

        if (missionNPC == null)
        {
            Debug.LogWarning("⚠️ [에러] HubManager의 'Mission NPC' 빈칸이 비어있습니다! 목적지 오브젝트를 넣어주세요.");
            return;
        }

        // 정상적으로 다 들어있다면 걸어가기 시작
        playerMovement.WalkToTarget(missionNPC, OpenStageSelect);
    }

    public void OpenStageSelect()
    {
        CloseAllPanels();
        stageSelectPanel.SetActive(true);
    }

    public void OpenShop()
    {
        CloseAllPanels();
        shopPanel.SetActive(true);
    }

    public void OpenSettings()
    {
        CloseAllPanels();
        settingsPanel.SetActive(true);
    }

    // "돌아가기" 버튼을 누르거나 메뉴를 완전히 닫을 때
    public void CloseMenuAndReturnToVillage()
    {
        CloseAllPanels();
        SetPlayerControl(true); // 이제 다시 캐릭터를 움직일 수 있음
        Debug.Log("캐릭터가 원래 자리(마을 자유 이동 모드)로 돌아갔습니다!");
    }

    private void CloseAllPanels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (stageSelectPanel) stageSelectPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    // ========== [ 실제 기능 작동 ] ==========

    // 스테이지 1 (Game_Scene) 진입 버튼
    public void StartStageOne()
    {
        Debug.Log("🚀 1스테이지로 출격합니다!");
        // 씬 이름이 "Game_Scene"이라고 가정
        SceneManager.LoadScene("game_Scene");
    }

    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다.");
        Application.Quit();
    }

    // 플레이어 이동 가능 여부 설정
    private void SetPlayerControl(bool canMove)
    {
        if (playerMovement != null)
        {
            playerMovement.controlIsActive = canMove;
        }
    }
}

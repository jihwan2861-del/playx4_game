using UnityEngine;

/// <summary>
/// 허브 씬의 의뢰 상태(수락, 완료) 관리 및 세이브/로드를 독립적으로 처리하는 매니저 클래스입니다.
/// </summary>
public class HubMissionManager
{
    private HubUIManager ui;

    public HubMissionManager(HubUIManager uiManager)
    {
        ui = uiManager;
    }

    public void CompleteHoloMission(string missionId)
    {
        if (ui.hologramMissions == null) return;
        foreach (var mission in ui.hologramMissions)
        {
            if (mission.missionId == missionId)
            {
                mission.isCompleted = true;
                
                // 완료 시 자동으로 추적 해제
                mission.isAccepted = false;
                
                SaveMissionStates(); 
                Debug.Log($"✅ [임무 성공 달성] {mission.title} 미션 클리어!");
                
                // HUD 끄기
                ui.hasAcceptedMission = false;
                if (ui.hubMissionPanel != null) ui.hubMissionPanel.SetActive(false);

                if (ui.hologramPanel != null && ui.hologramPanel.activeSelf)
                {
                    ui.RefreshWorldMapNodes();
                }
                break;
            }
        }
    }

    public void LoadMissionStates()
    {
        if (ui.hologramMissions == null) return;
        foreach (var mission in ui.hologramMissions)
        {
            mission.isAccepted = PlayerPrefs.GetInt($"Mission_{mission.missionId}_Accepted", 0) == 1;
            mission.isCompleted = PlayerPrefs.GetInt($"Mission_{mission.missionId}_Completed", 0) == 1;
        }

        // 스테이지 1~6 클리어 완료 연동
        for (int i = 1; i <= 6; i++)
        {
            if (PlayerPrefs.GetInt($"Mission_Stage{i}_Completed", 0) == 1)
            {
                var sMission = ui.hologramMissions.Find(m => m.missionId == $"Stage{i}");
                if (sMission != null)
                {
                    sMission.isCompleted = true;
                }
            }
        }
    }

    public void SaveMissionStates()
    {
        if (ui.hologramMissions == null) return;
        foreach (var mission in ui.hologramMissions)
        {
            PlayerPrefs.SetInt($"Mission_{mission.missionId}_Accepted", mission.isAccepted ? 1 : 0);
            PlayerPrefs.SetInt($"Mission_{mission.missionId}_Completed", mission.isCompleted ? 1 : 0);
        }
        PlayerPrefs.Save();
        Debug.Log("💾 [미션 세이브 완료] 진행 상황이 성공적으로 저장되었습니다.");
    }

    public void SetHubMissionText(string newMissionDescription)
    {
        ui.hasAcceptedMission = true;

        if (ui.hubMissionPanel != null)
        {
            ui.hubMissionPanel.SetActive(true);
        }

        if (ui.hubMissionText != null)
        {
            ui.SetText(ui.hubMissionText, $"<color=#FFFF00>[진행 중인 임무]</color>\n<color=#FFFFFF>[ ]</color> {newMissionDescription}");
        }

        Debug.Log($"🎯 [커스텀 미션 등록] {newMissionDescription}");
    }

    /// <summary>
    /// 브리핑 창에서 '추적' 버튼을 눌렀을 때 호출됩니다.
    /// </summary>
    public void TrackSelectedStage()
    {
        if (ui.currentSelectedStageIndex == -1 || ui.hologramMissions == null) return;
        if (ui.currentSelectedStageIndex >= ui.hologramMissions.Count) return;

        ui.PlaySFX(ui.buttonClickSFX);
        HologramMission targetMission = ui.hologramMissions[ui.currentSelectedStageIndex];

        // 다른 모든 스테이지의 수락 상태를 해제 (단 하나의 스테이지도 단독 추적)
        foreach (var mission in ui.hologramMissions)
        {
            if (mission != targetMission)
            {
                mission.isAccepted = false;
            }
        }

        targetMission.isAccepted = true;
        ui.hasAcceptedMission = true;

        if (ui.hubMissionPanel != null) ui.hubMissionPanel.SetActive(true);
        if (ui.hubMissionText != null)
        {
            ui.SetText(ui.hubMissionText, $"<color=#FFFF00>[추적 중인 스테이지]</color>\n<color=#FFFFFF>▶</color> {targetMission.title}");
        }

        Debug.Log($"🎯 [스테이지 추적 시작] {targetMission.title}");
        SaveMissionStates();

        // 나레이션 UI를 통해 추적 시작 문구를 부드럽게 띄웁니다.
        if (NarrationUI.instance != null)
        {
            NarrationUI.instance.Show($"<color=#FFFF00>{targetMission.title}</color>를 추적합니다.", () => {
                NarrationUI.instance.Close(3.0f); // 타이핑이 끝나고 3초 뒤에 페이드아웃
            });
        }
        
        ui.CloseAllPanels();
    }

    /// <summary>
    /// 브리핑 창에서 '포기' 버튼을 눌렀을 때 호출됩니다.
    /// </summary>
    public void AbandonSelectedStage()
    {
        if (ui.currentSelectedStageIndex == -1 || ui.hologramMissions == null) return;
        if (ui.currentSelectedStageIndex >= ui.hologramMissions.Count) return;

        ui.PlaySFX(ui.buttonClickSFX);
        HologramMission targetMission = ui.hologramMissions[ui.currentSelectedStageIndex];

        targetMission.isAccepted = false;
        
        bool anyTracked = false;
        foreach (var mission in ui.hologramMissions)
        {
            if (mission.isAccepted)
            {
                anyTracked = true;
                break;
            }
        }

        ui.hasAcceptedMission = anyTracked;

        if (ui.hubMissionPanel != null) ui.hubMissionPanel.SetActive(anyTracked);
        if (!anyTracked && ui.hubMissionText != null)
        {
            ui.SetText(ui.hubMissionText, "");
        }

        Debug.Log($"🛑 [스테이지 추적 포기] {targetMission.title}");
        SaveMissionStates();
        
        // 포기 시 브리핑창을 닫고 월드맵 패널을 다시 활성화하여 다른 스테이지를 고를 수 있게 합니다.
        if (ui.briefingPanel != null) ui.briefingPanel.SetActive(false);
        if (ui.worldMapPanel != null) ui.worldMapPanel.SetActive(true);

        ui.RefreshWorldMapNodes();
    }
}

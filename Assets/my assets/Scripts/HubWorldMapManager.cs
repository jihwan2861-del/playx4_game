using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 월드맵 스테이지 노드의 활성화/잠금 처리 및 브리핑창 정보 제어 매니저 클래스입니다.
/// </summary>
public class HubWorldMapManager
{
    private HubUIManager ui;

    public HubWorldMapManager(HubUIManager uiManager)
    {
        ui = uiManager;
    }

    /// <summary>
    /// 차고나 터미널에서 상호작용 시 월드맵 패널을 엽니다.
    /// </summary>
    public void OpenWorldMap()
    {
        ui.PlaySFX(ui.buttonClickSFX);
        ui.CloseAllPanels();
        
        ui.currentSelectedStageIndex = -1; // 처음 열었을 때는 선택된 스테이지가 없도록 인덱스 초기화
        
        if (ui.worldMapPanel != null)
        {
            ui.worldMapPanel.SetActive(true);
        }
        
        ui.SetPlayerControl(false);
        ui.LoadMissionStates(); // 로컬 세이브/미션 상태 연동
        RefreshWorldMapNodes(); // 월드맵 상의 노드 UI 업데이트
    }

    private void SelectDefaultStage()
    {
        if (ui.hologramMissions == null || ui.hologramMissions.Count == 0) return;

        // 잠금 해제되어 있으며 아직 완료하지 않은 첫 번째 미션 노드를 디폴트로 선택합니다.
        for (int i = 0; i < ui.hologramMissions.Count; i++)
        {
            if (IsStageUnlocked(i) && !ui.hologramMissions[i].isCompleted)
            {
                SelectStageNode(i);
                return;
            }
        }
        
        // 만약 모든 스테이지를 완료했다면 가장 마지막 노드 선택
        SelectStageNode(ui.hologramMissions.Count - 1);
    }

    /// <summary>
    /// 특정 인덱스의 스테이지가 해제(Unlock)되었는지 반환합니다.
    /// </summary>
    public bool IsStageUnlocked(int stageIndex)
    {
        if (ui.hologramMissions == null || stageIndex < 0 || stageIndex >= ui.hologramMissions.Count) return false;

        HologramMission mission = ui.hologramMissions[stageIndex];

        // 인덱스가 0이거나 미션 ID가 "Stage1"인 경우(첫 번째 스테이지)는 언제나 해제 상태
        if (stageIndex == 0 || mission.missionId == "Stage1") return true;

        // 이전 스테이지를 클리어(isCompleted)했을 때 다음 스테이지가 잠금 해제됩니다.
        return ui.hologramMissions[stageIndex - 1].isCompleted;
    }

    /// <summary>
    /// 지도상의 스테이지 거점 마커 노드를 클릭했을 때 호출됩니다.
    /// </summary>
    public void SelectStageNode(int stageIndex)
    {
        Debug.Log($"🎯 [월드맵 매니저] SelectStageNode({stageIndex}) 호출됨!");

        if (ui.hologramMissions == null)
        {
            Debug.LogError("⚠️ [월드맵 매니저] ui.hologramMissions가 null입니다!");
            return;
        }

        if (stageIndex < 0 || stageIndex >= ui.hologramMissions.Count)
        {
            Debug.LogError($"⚠️ [월드맵 매니저] 인덱스 범위 초과! stageIndex: {stageIndex}, missions count: {ui.hologramMissions.Count}");
            return;
        }
        
        ui.PlaySFX(ui.buttonClickSFX);
        ui.currentSelectedStageIndex = stageIndex;
        HologramMission mission = ui.hologramMissions[stageIndex];

        // 1. 브리핑 텍스트 갱신 및 패널 활성화
        if (ui.briefingPanel != null)
        {
            ui.briefingPanel.SetActive(true);
            Debug.Log("🎯 [월드맵 매니저] briefingPanel을 활성화(true)했습니다!");
        }
        else
        {
            Debug.LogError("⚠️ [월드맵 매니저] ui.briefingPanel이 할당되어 있지 않습니다!");
        }

        // 월드맵 패널 비활성화 (브리핑창과 겹치지 않도록 처리)
        if (ui.worldMapPanel != null)
        {
            ui.worldMapPanel.SetActive(false);
            Debug.Log("🎯 [월드맵 매니저] worldMapPanel을 비활성화(false)했습니다!");
        }

        if (ui.briefingTitleText != null) ui.briefingTitleText.text = mission.title;
        if (ui.briefingDescText != null) ui.briefingDescText.text = mission.description;

        // 보스 일러스트 할당
        if (ui.briefingBossImage != null)
        {
            if (mission.bossIllustration != null)
            {
                ui.briefingBossImage.gameObject.SetActive(true);
                ui.briefingBossImage.sprite = mission.bossIllustration;
            }
            else
            {
                ui.briefingBossImage.gameObject.SetActive(false); // 일러스트가 없을 경우 비활성화
            }
        }

        // 2. 전체 노드 상태 새로고침
        RefreshWorldMapNodes();
    }

    /// <summary>
    /// 브리핑 창을 닫고 월드맵 패널을 다시 활성화합니다.
    /// </summary>
    public void CloseBriefingAndOpenWorldMap()
    {
        ui.PlaySFX(ui.buttonClickSFX);
        if (ui.briefingPanel != null) ui.briefingPanel.SetActive(false);
        if (ui.worldMapPanel != null) ui.worldMapPanel.SetActive(true);
        RefreshWorldMapNodes();
    }

    /// <summary>
    /// 전체 거점 마크(잠금, 완료 여부) 및 브리핑창 우측 하단 버튼들의 상태를 실시간으로 새로고침합니다.
    /// </summary>
    public void RefreshWorldMapNodes()
    {
        if (ui.hologramMissions == null || ui.hologramMissions.Count == 0) return;

        // 1. 지도 위 노드들(버튼 상호작용, 잠금 열쇠, 완료 체크 마크) 제어
        for (int i = 0; i < ui.stageNodes.Length; i++)
        {
            if (i >= ui.hologramMissions.Count) break;
            if (ui.stageNodes[i] == null) continue;

            HologramMission mission = ui.hologramMissions[i];
            bool unlocked = IsStageUnlocked(i);

            // 잠긴 노드는 클릭 비활성화
            ui.stageNodes[i].interactable = unlocked;

            // 자식 오브젝트의 잠금 자물쇠 아이콘 토글
            if (ui.stageNodeLocks != null && i < ui.stageNodeLocks.Length && ui.stageNodeLocks[i] != null)
            {
                ui.stageNodeLocks[i].SetActive(!unlocked);
            }

            // 자식 오브젝트의 최종 클리어 녹색 체크마크 토글 (클리어 완료 시 체크마크 활성화)
            if (ui.stageNodeChecks != null && i < ui.stageNodeChecks.Length && ui.stageNodeChecks[i] != null)
            {
                ui.stageNodeChecks[i].SetActive(mission.isCompleted);
            }
        }

        // 2. 브리핑 상세창 우측 하단의 작동 버튼 상태 제어
        if (ui.currentSelectedStageIndex != -1)
        {
            HologramMission selectedMission = ui.hologramMissions[ui.currentSelectedStageIndex];
            bool isUnlocked = IsStageUnlocked(ui.currentSelectedStageIndex);

            // 추적 버튼 및 포기 버튼 활성화/비활성화 처리
            if (ui.worldMapActionButton != null)
            {
                ui.worldMapActionButton.gameObject.SetActive(true);

                if (!isUnlocked)
                {
                    SetActionButtonState("잠김", false);
                }
                else if (selectedMission.isCompleted)
                {
                    if (selectedMission.isAccepted)
                    {
                        SetActionButtonState("추적 중", false);
                    }
                    else
                    {
                        SetActionButtonState("추적", true);
                    }
                }
                else if (selectedMission.isAccepted)
                {
                    SetActionButtonState("추적 중", false);
                }
                else
                {
                    SetActionButtonState("추적", true);
                }
            }

            if (ui.abandonButton != null)
            {
                ui.abandonButton.gameObject.SetActive(true);

                // 현재 스테이지가 잠겨있거나, 혹은 현재 추적 중인 상태가 아니라면 포기할 수 없음
                if (!isUnlocked || !selectedMission.isAccepted)
                {
                    ui.abandonButton.interactable = false;
                }
                else
                {
                    ui.abandonButton.interactable = true;
                }

                if (ui.abandonButtonText != null)
                {
                    ui.abandonButtonText.text = "포기";
                }
            }
        }
        else
        {
            if (ui.worldMapActionButton != null) ui.worldMapActionButton.gameObject.SetActive(false);
            if (ui.abandonButton != null) ui.abandonButton.gameObject.SetActive(false);
        }
    }

    private void SetActionButtonState(string label, bool interactable)
    {
        if (ui.worldMapActionButton != null)
        {
            ui.worldMapActionButton.interactable = interactable;
        }
        
        if (ui.worldMapActionButtonText != null)
        {
            ui.worldMapActionButtonText.text = label;
        }
    }
}

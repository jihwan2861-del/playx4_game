using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어 데이터 매니저 (골드, 칩, 업그레이드 스탯, 스테이지 정보 등 로컬 저장 및 로드)
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager instance;

    [Header("재화")]
    public int upgradeChips = 0; // 업그레이드 칩 (핵심 재화)
    public int gold = 99999;     // 골드 (전시용 99,999 고정)

    [Header("기체 업그레이드 레벨")]
    public int speedLevel = 0;        // 이동속도 강화
    public int maxEnergyLevel = 0;    // 최대 에너지 강화
    public int energyRegenLevel = 0;  // 에너지 재생력 강화
    public int dashCostLevel = 0;     // 대쉬 에너지 소모 감소
    public int maxHpLevel = 0;        // 최대 체력 강화

    [Header("게임 진행 플래그")]
    public bool tutorialCompleted = false; // 튜토리얼 완료 여부
    public int currentStage = 1;           // 현재 스테이지

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 1. [Backspace] 게임 초기화 (모든 데이터 완전 삭제)
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            ResetData();
            Debug.Log("🧹 [데이터 전체 포맷] 로컬 저장소가 완벽하게 공장 초기화되었습니다.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // 2. [F10] 치트키 - VIP 모드 (모든 재화 999/99999 및 레벨 3 설정)
        if (Input.GetKeyDown(KeyCode.F10))
        {
            upgradeChips = 999;
            gold = 99999;
            speedLevel = 3;
            maxEnergyLevel = 3;
            energyRegenLevel = 3;
            dashCostLevel = 3;
            maxHpLevel = 3;
            
            SaveData();
            Debug.Log("🎁 [치트] F10 치트 발동! 999 칩, 99999 골드 및 모든 스탯 레벨 3 적용.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // 3. [ ] ] 다음 스테이지 이동 치트키
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            string currentScene = SceneManager.GetActiveScene().name;
            string nextScene = "Hub_Scene";
            if (currentScene == "MainMenu_Sc") nextScene = "Hub_Scene";
            else if (currentScene == "Hub_Scene") nextScene = "Tutorial_scene";
            else if (currentScene == "Tutorial_scene")
            {
                tutorialCompleted = true;
                PlayerPrefs.SetInt("Save_TutorialCompleted", 1);
                PlayerPrefs.SetInt("Mission_Tutorial_Completed", 1);
                PlayerPrefs.Save();
                nextScene = "Ride_Scene";
            }
            else if (currentScene == "Ride_Scene") nextScene = "game_Scene";
            else if (currentScene == "game_Scene")
            {
                PlayerPrefs.SetInt("Mission_Stage1_Completed", 1);
                PlayerPrefs.Save();
                nextScene = "Stage2_Scene";
            }
            else if (currentScene == "Stage2_Scene")
            {
                PlayerPrefs.SetInt("Mission_Stage2_Completed", 1);
                PlayerPrefs.Save();
                nextScene = "Stage3_Scene";
            }
            else if (currentScene == "Stage3_Scene")
            {
                PlayerPrefs.SetInt("Mission_Stage3_Completed", 1);
                PlayerPrefs.Save();
                nextScene = "Hub_Scene";
            }

            Time.timeScale = 1f;
            Debug.Log("⏭️ [치트] ']' 키 입력 - 씬 이동: " + currentScene + " -> " + nextScene);
            SceneManager.LoadScene(nextScene);
        }

        // 4. [ [ ] 허브 씬으로 강제 이동 치트키
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            Time.timeScale = 1f;
            Debug.Log("🏠 [치트] '[' 키 입력 - 즉시 허브 씬으로 복귀합니다.");
            SceneManager.LoadScene("Hub_Scene");
        }
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt("Save_UpgradeChips", upgradeChips);
        PlayerPrefs.SetInt("Save_Gold", gold);
        PlayerPrefs.SetInt("Save_SpeedLevel", speedLevel);
        PlayerPrefs.SetInt("Save_MaxEnergyLevel", maxEnergyLevel);
        PlayerPrefs.SetInt("Save_EnergyRegenLevel", energyRegenLevel);
        PlayerPrefs.SetInt("Save_DashCostLevel", dashCostLevel);
        PlayerPrefs.SetInt("Save_MaxHpLevel", maxHpLevel);
        PlayerPrefs.SetInt("Save_TutorialCompleted", tutorialCompleted ? 1 : 0);
        PlayerPrefs.SetInt("Save_CurrentStage", currentStage);
        
        PlayerPrefs.Save();
        Debug.Log("💾 [데이터 저장] 스탯 및 진행 정보가 저장되었습니다.");
    }

    public void LoadData()
    {
        upgradeChips = PlayerPrefs.GetInt("Save_UpgradeChips", 0);
        gold = PlayerPrefs.GetInt("Save_Gold", 99999);
        speedLevel = PlayerPrefs.GetInt("Save_SpeedLevel", 0);
        maxEnergyLevel = PlayerPrefs.GetInt("Save_MaxEnergyLevel", 0);
        energyRegenLevel = PlayerPrefs.GetInt("Save_EnergyRegenLevel", 0);
        dashCostLevel = PlayerPrefs.GetInt("Save_DashCostLevel", 0);
        maxHpLevel = PlayerPrefs.GetInt("Save_MaxHpLevel", 0);
        tutorialCompleted = PlayerPrefs.GetInt("Save_TutorialCompleted", 0) == 1;
        currentStage = PlayerPrefs.GetInt("Save_CurrentStage", 1);
        
        Debug.Log("📂 [데이터 로드] 칩: " + upgradeChips + " | 골드: " + gold + " | 스피드Lv: " + speedLevel + " | 에너지Lv: " + maxEnergyLevel + " | HP Lv: " + maxHpLevel);
    }

    public void ResetData()
    {
        PlayerPrefs.DeleteAll(); // 모든 로컬 데이터 깨끗하게 포맷하여 초기화 오염 제거
        upgradeChips = 0;
        gold = 99999;
        speedLevel = 0;
        maxEnergyLevel = 0;
        energyRegenLevel = 0;
        dashCostLevel = 0;
        maxHpLevel = 0;
        tutorialCompleted = false;
        currentStage = 1;

        PlayerPrefs.Save();
        Debug.Log("🧹 [로컬 저장소 초기화] 모든 PlayerPrefs 데이터와 메모리 데이터가 공장 초기화되었습니다.");
    }

    public void AddChips(int amount)
    {
        upgradeChips += amount;
        Debug.Log("💎 [칩 획득] +" + amount + " -> 총 칩: " + upgradeChips);
        SaveData();
    }

    public bool SpendChips(int amount)
    {
        if (upgradeChips >= amount)
        {
            upgradeChips -= amount;
            Debug.Log("💸 [칩 소비] -" + amount + " -> 남은 칩: " + upgradeChips);
            SaveData();
            return true;
        }
        else
        {
            Debug.LogWarning("⚠️ 칩이 부족합니다!");
            return false;
        }
    }

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log("🪙 [골드 획득] +" + amount + " -> 총 골드: " + gold);
        SaveData();
    }

    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            Debug.Log("💸 [골드 소비] -" + amount + " -> 남은 골드: " + gold);
            SaveData();
            return true;
        }
        else
        {
            Debug.LogWarning("⚠️ 골드가 부족합니다!");
            return false;
        }
    }
}

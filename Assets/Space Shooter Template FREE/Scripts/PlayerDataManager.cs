using UnityEngine;

/// <summary>
/// 플레이어의 업그레이드 칩(재화) 및 스탯 데이터를 씬 전환 시에도 유지하는 데이터 매니저입니다.
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager instance;

    [Header("재화")]
    public int upgradeChips = 0; // 업그레이드 칩 (핵심 재화)

    [Header("기체 업그레이드 레벨")]
    public int speedLevel = 0;        // 이동속도 강화
    public int maxEnergyLevel = 0;    // 최대 에너지 강화
    public int energyRegenLevel = 0;  // 에너지 재생력 강화
    public int dashCostLevel = 0;     // 대쉬 소모 감소
    public int maxHpLevel = 0;        // 최대 체력 강화

    [Header("게임 진행 플래그")]
    public bool tutorialCompleted = false; // 튜토리얼 완료 여부
    public int currentStage = 1;           // 현재 스테이지

    void Awake()
    {
        // 씬이 바뀌어도 파괴되지 않고 데이터를 유지하는 싱글톤 패턴
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 스테이지 클리어 후 칩을 획득합니다.
    /// </summary>
    public void AddChips(int amount)
    {
        upgradeChips += amount;
        Debug.Log($"💎 [칩 획득] +{amount} → 총 보유: {upgradeChips}");
    }

    /// <summary>
    /// 업그레이드 시 칩을 소모합니다.
    /// </summary>
    public bool SpendChips(int amount)
    {
        if (upgradeChips >= amount)
        {
            upgradeChips -= amount;
            Debug.Log($"🔧 [칩 소모] -{amount} → 남은 칩: {upgradeChips}");
            return true;
        }
        else
        {
            Debug.Log("❌ 칩이 부족합니다!");
            return false;
        }
    }
}

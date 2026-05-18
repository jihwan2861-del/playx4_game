using UnityEngine;

/// <summary>
/// 플레이어의 재화(부품) 및 업그레이드 데이터를 씬 전환 시에도 유지하는 데이터 매니저입니다.
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager instance;

    [Header("플레이어 데이터")]
    public int salvagedParts = 0; // 수거한 부품 (재화)
    public int engineLevel = 1;   // 기체 엔진 레벨 (예시)
    public int shieldLevel = 1;   // 방어막 레벨 (예시)

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
    /// 전투 결과창 등에서 부품을 추가할 때 사용합니다.
    /// </summary>
    public void AddParts(int amount)
    {
        salvagedParts += amount;
        Debug.Log($"🔧 부품 획득! 현재 총 부품: {salvagedParts}");
    }

    /// <summary>
    /// 업그레이드 시 부품을 소모할 때 사용합니다.
    /// </summary>
    /// <returns>소모 성공 여부</returns>
    public bool SpendParts(int amount)
    {
        if (salvagedParts >= amount)
        {
            salvagedParts -= amount;
            Debug.Log($"💰 부품 소모: {amount} / 남은 부품: {salvagedParts}");
            return true;
        }
        else
        {
            Debug.Log("❌ 부품이 부족합니다!");
            return false;
        }
    }
}

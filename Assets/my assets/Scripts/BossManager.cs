using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스의 체력(HP)과 UI 게이지 갱신 및 격퇴 처리를 전담 관리하는 싱글톤 매니저 클래스입니다.
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager instance;

    [Header("=== 보스 스탯 ===")]
    [Tooltip("보스의 최대 체력")]
    public float maxHp = 500f;
    [HideInInspector]
    public float currentHp;
    [Tooltip("보스 이름 (HODGE, MASS GAP 등)")]
    public string bossName = "HODGE";

    [Header("=== UI 컴포넌트 연결 ===")]
    [Tooltip("보스 출현 시 활성화할 UI 패널")]
    public GameObject uiPanel;
    [Tooltip("보스 체력 게이지 이미지 (Image Type: Filled)")]
    public Image healthBarFill;
    [Tooltip("체력 퍼센트 숫자를 표시할 텍스트 오브젝트 (TMP/Text 모두 호환)")]
    public GameObject healthPercentageTextObj;
    [Tooltip("보스 이름을 표시할 텍스트 오브젝트 (TMP/Text 모두 호환)")]
    public GameObject bossNameTextObj;

    [HideInInspector]
    public bool isHacking = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentHp = maxHp;
    }

    private void Start()
    {
        UpdateBossUI();
    }

    private void Update()
    {
        // 해킹 중일 때 초당 2.5의 지속적인 데미지를 가합니다.
        if (currentHp > 0 && isHacking)
        {
            TakeDamage(Time.deltaTime * 2.5f);
        }
    }

    /// <summary>
    /// 보스가 데미지를 입었을 때 호출되어 체력을 깎고 UI를 갱신합니다.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (currentHp <= 0) return;

        currentHp -= damage;
        if (currentHp <= 0)
        {
            currentHp = 0;
            OnBossDefeated();
        }
        UpdateBossUI();
    }

    /// <summary>
    /// 체력바(Fill Amount) 및 텍스트 문구들을 실시간 동기화합니다.
    /// </summary>
    private void UpdateBossUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(currentHp > 0);
        }

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHp / maxHp);
        }

        int percent = Mathf.CeilToInt((currentHp / maxHp) * 100);
        SetText(healthPercentageTextObj, $"{percent}%");
        
        // 씬 명칭에 따른 이름 자동 보완 (선택사항)
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (activeSceneName == "game_Scene") bossName = "HODGE";
        else if (activeSceneName == "Stage2_Scene") bossName = "MASS GAP";
        
        SetText(bossNameTextObj, bossName);
    }

    /// <summary>
    /// 보스 사망 처리 및 스테이지 클리어 이벤트를 전개합니다.
    /// </summary>
    private void OnBossDefeated()
    {
        Debug.Log("🏁 [보스 퇴치 성공] 보스를 처단하여 스테이지를 클리어했습니다!");

        if (LevelController.instance != null)
        {
            LevelController.instance.TriggerVictory();
        }
    }

    /// <summary>
    /// TextMeshPro와 일반 Text 컴포넌트 모두 호환되는 만능 텍스트 입력 유틸 함수입니다.
    /// </summary>
    private void SetText(GameObject obj, string val)
    {
        if (obj == null) return;
        
        // 1. 일반 Legacy Text
        var txt = obj.GetComponent<Text>();
        if (txt != null)
        {
            txt.text = val;
            return;
        }

        // 2. TextMeshPro (TMP) 등의 리플렉션 대응
        foreach (var comp in obj.GetComponents<Component>())
        {
            if (comp == null) continue;
            var prop = comp.GetType().GetProperty("text");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(comp, val, null);
                return;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 에디터 인스펙터에서 직접 체력 대상 오브젝트와 UI 컴포넌트들을 드래그 앤 드롭하여 연결할 수 있는 게임 매니저 스크립트입니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("=== 체력 보유 오브젝트 ===")]
    [Tooltip("체력(Enemy 컴포넌트)을 지닌 캐릭터/보스 오브젝트를 드래그 앤 드롭 하세요.")]
    public GameObject targetObjectWithHealth;

    [Header("=== 체력 시각화 UI 컴포넌트 ===")]
    [Tooltip("보스가 살아있을 때 표시할 UI 패널")]
    public GameObject uiPanel;
    
    [Tooltip("체력 게이지 이미지 (Type: Filled)")]
    public Image healthBarFill;
    
    [Tooltip("체력 게이지 슬라이더 (Image 방식 대신 Slider를 쓸 때 사용)")]
    public Slider healthSlider;

    [Tooltip("체력 백분율 수치를 표시할 텍스트 오브젝트 (TMP/일반 텍스트 모두 지원)")]
    public GameObject healthPercentageTextObj;

    [Tooltip("보스 이름을 표시할 텍스트 오브젝트")]
    public GameObject bossNameTextObj;

    [Tooltip("표시할 보스 이름")]
    public string bossName = "HODGE";

    private Enemy targetEnemy;
    private float maxHealth;
    private bool isDefeated = false;

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
    }

    private void Start()
    {
        if (targetObjectWithHealth != null)
        {
            targetEnemy = targetObjectWithHealth.GetComponent<Enemy>();
            if (targetEnemy != null)
            {
                maxHealth = targetEnemy.health;
            }
            else
            {
                Debug.LogWarning("⚠️ [GameManager] 대상 오브젝트에 'Enemy' 스크립트가 없습니다. 인스펙터를 확인해 주세요!");
            }
        }

        UpdateUI();
    }

    private void Update()
    {
        if (targetObjectWithHealth != null && targetEnemy == null)
        {
            // 지연 캐싱 시도 (시작할 때 컴포넌트가 늦게 붙는 경우 대비)
            targetEnemy = targetObjectWithHealth.GetComponent<Enemy>();
            if (targetEnemy != null)
            {
                maxHealth = targetEnemy.health;
            }
        }

        if (targetEnemy != null)
        {
            UpdateUI();

            // 체력이 0 이하가 되면 격퇴 처리
            if (targetEnemy.health <= 0 && !isDefeated)
            {
                isDefeated = true;
                OnDefeated();
            }
        }
        else if (targetObjectWithHealth == null && !isDefeated && maxHealth > 0)
        {
            // 대상 오브젝트가 씬에서 파괴(Destroy)되었을 때 클리어 처리
            isDefeated = true;
            OnDefeated();
        }
    }

    private void UpdateUI()
    {
        // 체력이 남아 있을 때만 UI 활성화
        if (uiPanel != null)
        {
            uiPanel.SetActive(targetEnemy != null && targetEnemy.health > 0);
        }

        if (targetEnemy == null) return;

        float currentHp = Mathf.Max(0, targetEnemy.health);

        // 1. Image Filled 방식 UI 업데이트
        if (healthBarFill != null && maxHealth > 0)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHp / maxHealth);
        }

        // 2. Slider 방식 UI 업데이트
        if (healthSlider != null && maxHealth > 0)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHp;
        }

        // 3. 체력 퍼센트 텍스트(%) 업데이트
        if (healthPercentageTextObj != null && maxHealth > 0)
        {
            int percent = Mathf.CeilToInt((currentHp / maxHealth) * 100);
            SetText(healthPercentageTextObj, $"{percent}%");
        }

        // 4. 이름 텍스트 업데이트
        if (bossNameTextObj != null)
        {
            SetText(bossNameTextObj, bossName);
        }
    }

    private void OnDefeated()
    {
        Debug.Log($"🏁 [GameManager] 대상 오브젝트({bossName}) 격퇴 성공!");
        
        if (LevelController.instance != null)
        {
            LevelController.instance.TriggerVictory();
        }
    }

    /// <summary>
    /// TextMeshPro와 일반 Text 컴포넌트 모두 호환되는 만능 텍스트 입력 유틸 함수
    /// </summary>
    private void SetText(GameObject obj, string val)
    {
        if (obj == null) return;
        
        var txt = obj.GetComponent<Text>();
        if (txt != null)
        {
            txt.text = val;
            return;
        }

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

using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("체력바 (HP Bar) - Image Filled 방식")]
    [Tooltip("HP_Bar_Fill (Image Type: Filled) 오브젝트를 넣으세요")]
    public Image fillImage;
    public Color highHpColor = new Color(0.2f, 0.95f, 0.1f);
    public Color lowHpColor = new Color(1f, 0.2f, 0.1f);

    [Header("에너지 바 (Energy Bar) - Image Filled 방식")]
    [Tooltip("Energy_Bar_Fill (Image Type: Filled) 오브젝트를 넣으세요")]
    public Image energyFillImage;
    public Color highEnergyColor = new Color(0.6f, 1f, 0.05f);
    public Color lowEnergyColor = new Color(1f, 0.85f, 0f);

    [Header("스킬 사용 예고 눈금 (Tick)")]
    [Tooltip("직접 만든 Tick_1 이미지를 여기에 넣으세요")]
    public RectTransform costPreviewTick;
    [Tooltip("에너지 바의 배경(Energy_Bar_BG)의 RectTransform을 넣으세요 (너비 계산용)")]
    public RectTransform energyBarRect;

    [Header("Victory UI")]
    public GameObject victoryTextObj;

    // 하위 호환용
    [HideInInspector] public Slider healthSlider;
    [HideInInspector] public Text hpText;
    [HideInInspector] public Slider energySlider;
    [HideInInspector] public Text energyText;

    public static PlayerUI instance;

    private void Awake()
    {
        instance = this;
    }

    public void ShowVictory()
    {
        if (victoryTextObj != null)
        {
            victoryTextObj.SetActive(true);
            Text t = victoryTextObj.GetComponent<Text>();
            if (t != null) t.text = "MISSION COMPLETE!\nSAFE ZONE REACHED";
        }
    }

    void Update()
    {
        // ============================================
        // 1. HP 바
        // ============================================
        if (Player.instance != null)
        {
            float hpPercent = (float)Player.instance.health / Player.instance.maxHealth;

            if (fillImage != null)
            {
                fillImage.fillAmount = hpPercent;
                fillImage.color = Color.Lerp(lowHpColor, highHpColor, hpPercent);
            }

            // 하위 호환
            if (healthSlider != null)
            {
                healthSlider.maxValue = Player.instance.maxHealth;
                healthSlider.value = Player.instance.health;
            }
            if (hpText != null) hpText.text = Player.instance.health + " / " + Player.instance.maxHealth;
        }
        else
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = 0;
                fillImage.color = Color.gray;
            }
            if (healthSlider != null) healthSlider.value = 0;
            if (hpText != null) hpText.text = "DESTROYED";
        }

        // ============================================
        // 2. 에너지 바
        // ============================================
        if (PlayerMoving.instance != null)
        {
            float current = PlayerMoving.instance.currentEnergy;
            float max = PlayerMoving.instance.maxEnergy;
            float cost = PlayerMoving.instance.dashEnergyCost;
            float energyPercent = max > 0 ? current / max : 0f;

            if (energyFillImage != null)
            {
                energyFillImage.fillAmount = energyPercent;
                energyFillImage.color = Color.Lerp(lowEnergyColor, highEnergyColor, energyPercent);
            }

            // ============================================
            // 3. 스킬 사용 예고 Tick (핵심!)
            //    "지금 스킬을 쓰면 바가 여기까지 줄어듭니다" 위치를 실시간으로 표시
            // ============================================
            if (costPreviewTick != null && energyBarRect != null)
            {
                if (current >= cost)
                {
                    // Tick 보이기
                    costPreviewTick.gameObject.SetActive(true);

                    // 스킬 사용 후 남을 에너지 비율 계산
                    float afterSkillPercent = Mathf.Clamp01((current - cost) / max);

                    // 에너지 바의 전체 너비에서 해당 비율 위치에 Tick 배치
                    float barWidth = energyBarRect.rect.width - 8f; // 양쪽 여백(4+4) 빼기
                    float tickX = 4f + afterSkillPercent * barWidth; // 왼쪽 여백 + 비율 위치

                    costPreviewTick.anchoredPosition = new Vector2(
                        tickX,
                        costPreviewTick.anchoredPosition.y
                    );
                }
                else
                {
                    // 에너지가 스킬 비용보다 적으면 Tick 숨기기
                    costPreviewTick.gameObject.SetActive(false);
                }
            }

            // 하위 호환
            if (energySlider != null)
            {
                energySlider.maxValue = max;
                energySlider.value = current;
            }
            if (energyText != null) energyText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
        }
    }
}

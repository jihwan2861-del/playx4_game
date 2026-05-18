using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 왼쪽 상단에 보스의 남은 체력(해킹 남은 시간)을 표시하는 UI 스크립트입니다.
/// </summary>
public class BossHealthUI : MonoBehaviour
{
    public GameObject uiPanel;      // 보스 출현 시 켜질 패널
    public Image healthBarFill;     // 체력바 (Image Type: Filled)
    public GameObject healthPercentageTextObj; // 퍼센트 글자가 들어갈 텍스트 오브젝트 (TMP 지원)

    void Start()
    {
        // 시작과 동시에 보스전이므로 UI를 숨기지 않습니다.
    }
    void Update()
    {
        // 1. 플레이어가 죽었으면 무조건 UI 다 끄기
        if (Player.instance == null)
        {
            SetUIActive(false);
            return;
        }

        // 2. 먼저 메인 게임의 진짜 보스를 찾습니다.
        BossPatternController boss = FindObjectOfType<BossPatternController>();
        // 3. 만약 보스가 없다면, 튜토리얼용 봇을 찾습니다.
        TutorialDummy dummy = FindObjectOfType<TutorialDummy>();

        if (boss != null)
        {
            float current = boss.currentSurvivalTimer;
            float max = boss.bossSurvivalTime;
            float ratio = Mathf.Clamp01(current / max);
            int percent = Mathf.CeilToInt(ratio * 100);

            // 체력이 0 이하가 되면 즉시 UI 모두 숨기기
            if (percent <= 0)
            {
                SetUIActive(false);
                return;
            }

            // 보스가 살아있다면 UI 켜기
            SetUIActive(true);

            if (healthBarFill != null) healthBarFill.fillAmount = percent / 100f;
            Color textColor = boss.isHacking ? Color.cyan : Color.white;
            SetTextAndColor(healthPercentageTextObj, $"{percent}%", textColor);
        }
        else if (dummy != null && dummy.isHackingMode)
        {
            float current = dummy.currentHealth;
            float max = dummy.maxHealth;
            float ratio = Mathf.Clamp01(current / max);
            int percent = Mathf.CeilToInt(ratio * 100);

            // 체력이 0 이하가 되면 즉시 UI 모두 숨기기
            if (percent <= 0)
            {
                SetUIActive(false);
                return;
            }

            // 튜토리얼 더미가 해킹 모드라면 똑같이 UI 켜기!
            SetUIActive(true);

            if (healthBarFill != null) healthBarFill.fillAmount = percent / 100f;
            
            // 더미 쪽에서 해킹 중일 때는 체력이 깎이고 있을 테니 초록색이나 민트색으로 표시
            bool isBeingHacked = Vector3.Distance(dummy.transform.position, Player.instance.transform.position) <= dummy.hackingRadius;
            Color textColor = isBeingHacked ? Color.cyan : Color.white;

            SetTextAndColor(healthPercentageTextObj, $"{percent}%", textColor);
        }
        else
        {
            // 보스도 없고 해킹용 더미도 없으면 UI 숨김
            SetUIActive(false);
        }
    }

    // 패널과 글자(숫자) 오브젝트를 한 번에 켜고 끄는 함수 (글자가 패널 밖에 있어도 확실히 꺼줌)
    void SetUIActive(bool isActive)
    {
        if (uiPanel != null && uiPanel.activeSelf != isActive)
        {
            uiPanel.SetActive(isActive);
        }
        if (healthPercentageTextObj != null && healthPercentageTextObj.activeSelf != isActive)
        {
            healthPercentageTextObj.SetActive(isActive);
        }
        if (healthBarFill != null && healthBarFill.gameObject.activeSelf != isActive)
        {
            healthBarFill.gameObject.SetActive(isActive);
        }
    }

    // TextMeshPro와 일반 Text를 모두 지원하는 만능 텍스트 입력 함수
    void SetTextAndColor(GameObject obj, string textValue, Color colorValue)
    {
        if (obj == null) return;
        
        // 1. 일반 Legacy Text
        var legacyText = obj.GetComponent<UnityEngine.UI.Text>();
        if (legacyText != null) 
        {
            legacyText.text = textValue;
            legacyText.color = colorValue;
            return;
        }
        
        // 2. TextMeshPro (TMP) 지원
        Component[] components = obj.GetComponents<Component>();
        foreach(var comp in components)
        {
            if (comp == null) continue;
            if (comp.GetType().Name.Contains("Text"))
            {
                var textProp = comp.GetType().GetProperty("text");
                if (textProp != null && textProp.CanWrite)
                {
                    textProp.SetValue(comp, textValue, null);
                }
                
                var colorProp = comp.GetType().GetProperty("color");
                if (colorProp != null && colorProp.CanWrite)
                {
                    colorProp.SetValue(comp, colorValue, null);
                }
                return;
            }
        }
    }
}

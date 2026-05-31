using UnityEngine;

/// <summary>
/// 특정 지점(2D 트리거 콜라이더 영역)에 플레이어 기체가 도달하면 
/// 지정된 타겟(또는 플레이어 본인) 머리 위에 동적으로 말풍선(SpeechBubble)을 띄우는 
/// 유니티 전용 재사용 가능 스토리 연출 스크립트입니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StoryTriggerZone : MonoBehaviour
{
    [Header("=== 대상 설정 ===")]
    [Tooltip("말풍선이 머리 위에 나타날 게임오브젝트 (비워둘 경우 진입한 플레이어 본인으로 자동 설정)")]
    public GameObject targetObject;

    [Header("=== 대사 및 테마 설정 ===")]
    [TextArea(2, 5)]
    [Tooltip("트리거에 진입했을 때 출력될 말풍선 내용")]
    public string dialogueText = "여기가 첫 번째 구역인가... 조심해야겠군.";
    [Tooltip("말풍선 테마 네온 컬러")]
    public Color themeColor = new Color(0f, 0.8f, 1f, 1f); // Neon Cyan

    [Header("=== 자동 닫기 및 동작 옵션 ===")]
    [Tooltip("말풍선을 띄운 뒤 몇 초 후에 자동으로 수축 소멸하게 만들지 지정 (0 이하이면 나갈 때 소멸하거나 직접 닫아야 함)")]
    public float autoCloseDelay = 3.0f;
    [Tooltip("단 1회만 트리거되게 제한할지 여부 (스토리 연출 필수 권장)")]
    public bool triggerOnlyOnce = true;

    private bool hasTriggered = false;
    private SpeechBubble activeBubble;

    private void Awake()
    {
        // 트리거 충돌이 필수적이므로 콜라이더를 Is Trigger 상태로 강제 조정 및 경고 예방
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log($"🛡️ [{gameObject.name}] Collider2D의 'Is Trigger' 옵션을 자동으로 활성화했습니다.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 작동 한계 조건 검사
        if (hasTriggered && triggerOnlyOnce) return;
        if (!other.CompareTag("Player")) return;

        // 플레이어 머리 위의 정적 말풍선 인스턴스를 직접 호출하여 활성화 및 대사 대입
        if (SpeechBubble.playerBubbleInstance != null)
        {
            activeBubble = SpeechBubble.playerBubbleInstance;
            activeBubble.Show(dialogueText);

            if (autoCloseDelay > 0f)
            {
                activeBubble.Close(autoCloseDelay);
            }
        }
        else
        {
            // 백업: 플레이어 내부에 꺼진 상태로 보관되어 있는 SpeechBubble 컴포넌트 자동 검색
            activeBubble = other.GetComponentInChildren<SpeechBubble>(true);
            if (activeBubble != null)
            {
                activeBubble.Show(dialogueText);
                if (autoCloseDelay > 0f)
                {
                    activeBubble.Close(autoCloseDelay);
                }
            }
        }

        hasTriggered = true;
        Debug.Log($"💬 [{gameObject.name}] 스토리 트리거 작동 완료! 대사: '{dialogueText}'");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // autoCloseDelay가 0 이하(무제한 지속)일 때, 트리거존 밖으로 기체가 빠져나가면 말풍선을 닫아줍니다.
        if (autoCloseDelay <= 0f && other.CompareTag("Player") && activeBubble != null)
        {
            activeBubble.Close(0f);
        }
    }
}

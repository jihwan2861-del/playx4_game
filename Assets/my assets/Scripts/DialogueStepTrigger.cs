using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 플레이어가 트리거(Is Trigger가 켜진 Collider2D) 영역에 진입하는 순간 
/// 지정된 대사(DialoguePlayer.PlayDialogues)나 이벤트를 즉각 발동시키는 범용 밟기 트리거 스크립트입니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DialogueStepTrigger : MonoBehaviour
{
    [Header("=== 트리거 이벤트 ===")]
    [Tooltip("플레이어가 진입했을 때 실행할 이벤트 (예: DialoguePlayer의 PlayDialogues 호출)")]
    public UnityEvent onTriggerEnterEvent;

    [Header("=== 일회성 설정 ===")]
    [Tooltip("체크 시 이 트리거는 평생 딱 한 번만 작동하고 작동 정지됩니다.")]
    public bool triggerOnlyOnce = true;

    private bool isTriggered = false;

    private void Awake()
    {
        // Collider2D 컴포넌트의 Is Trigger 속성이 꺼져있을 경우 오작동을 막기 위해 강제 활성화
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;

        // 플레이어 태그를 가진 오브젝트가 밟았을 때만 이벤트 실행
        if (collision.CompareTag("Player"))
        {
            if (triggerOnlyOnce)
            {
                isTriggered = true;
            }

            Debug.Log($"👣 [DialogueStepTrigger] 플레이어가 {gameObject.name} 트리거를 밟았습니다. 이벤트 발동!");
            onTriggerEnterEvent?.Invoke();

            // 1회 작동 설정 시, 다시는 중복 연출되지 않도록 오브젝트 자체를 안전하게 비활성화
            if (triggerOnlyOnce)
            {
                gameObject.SetActive(false);
            }
        }
    }
}

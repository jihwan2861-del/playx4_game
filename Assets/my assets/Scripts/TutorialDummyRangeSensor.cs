using UnityEngine;

/// <summary>
/// 더미봇의 자식 오브젝트에 부착되어 플레이어 진입을 감지하는 범위 센서 스크립트입니다.
/// </summary>
public class TutorialDummyRangeSensor : MonoBehaviour
{
    private TutorialDummy parentDummy;

    void Start()
    {
        // 부모 오브젝트의 TutorialDummy 컴포넌트를 캐싱합니다.
        parentDummy = GetComponentInParent<TutorialDummy>();
        
        // IsTrigger 검증 예외 처리
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"⚠️ [{gameObject.name}] Collider2D가 Trigger로 설정되어 있지 않아 자동으로 Trigger로 변경했습니다.");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && parentDummy != null)
        {
            parentDummy.SetPlayerInRange(true);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && parentDummy != null)
        {
            parentDummy.SetPlayerInRange(false);
        }
    }
}

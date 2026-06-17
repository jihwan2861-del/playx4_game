using UnityEngine;

/// <summary>
/// 특정 영역(Is Trigger가 켜진 Collider2D) 안에 플레이어가 존재할 때만 
/// 지정한 더미봇(또는 다른 오브젝트)을 활성화(SetActive)하고, 영역을 벗어나면 비활성화하는 감지 영역 스크립트입니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DummyActivationArea : MonoBehaviour
{
    [Header("=== 대상 오브젝트 ===")]
    [Tooltip("영역 내에 플레이어가 들어왔을 때 활성화할 더미봇 오브젝트를 넣어주세요.")]
    public GameObject targetDummy;

    private void Awake()
    {
        // 영역 감지용 콜라이더의 Is Trigger 속성을 강제로 켭니다.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        // 시작할 때는 플레이어가 영역 안에 없으므로 타겟을 꺼둔 채 시작합니다.
        if (targetDummy != null)
        {
            targetDummy.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어가 영역 내로 진입했을 때 타겟 활성화
        if (collision.CompareTag("Player"))
        {
            if (targetDummy != null)
            {
                targetDummy.SetActive(true);
                Debug.Log($"🤖 [DummyActivationArea] 플레이어 진입 - '{targetDummy.name}' 활성화");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어가 영역 밖으로 나갔을 때 타겟 비활성화
        if (collision.CompareTag("Player"))
        {
            if (targetDummy != null)
            {
                targetDummy.SetActive(false);
                Debug.Log($"💤 [DummyActivationArea] 플레이어 이탈 - '{targetDummy.name}' 비활성화");
            }
        }
    }

    private void OnDestroy()
    {
        // 영역 오브젝트 자체가 파괴될 경우 안전하게 더미봇도 정리하거나 끌 수 있도록 예외 처리
        if (targetDummy != null)
        {
            targetDummy.SetActive(false);
        }
    }
}

using UnityEngine;

/// <summary>
/// 플레이어가 밟으면 특정 목적지로 가는 가이드라인(PathGuideLine)을 켜거나 
/// 가이드라인을 지워서 끄는 연출용 밟기 트리거 스크립트입니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GuideTrigger : MonoBehaviour
{
    public enum TriggerAction
    {
        SetTarget,   // 가이드선 켜고 목적지 지정
        ClearTarget  // 가이드선 끄기
    }

    [Header("=== 대상 가이드라인 (선택) ===")]
    [Tooltip("제어할 가이드라인 오브젝트입니다. 비워두면 씬 내의 가이드라인을 자동으로 찾아 작동합니다.")]
    public PathGuideLine guideLine;

    [Header("=== 작동 방식 설정 ===")]
    [Tooltip("SetTarget: 밟았을 때 가이드선이 켜지며 지정된 목적지를 가리킵니다.\nClearTarget: 목적지에 도달했을 때 밟으면 가이드선이 꺼집니다.")]
    public TriggerAction action = TriggerAction.SetTarget;

    [Header("=== 목적지 설정 (SetTarget 모드 전용) ===")]
    [Tooltip("가이드라인이 가리킬 새로운 목적지 오브젝트(Transform)를 넣어주세요.")]
    public Transform destinationTarget;

    [Header("=== 일회성 설정 ===")]
    [Tooltip("체크 시 이 트리거는 평생 딱 한 번만 작동하고 오브젝트가 사라집니다.")]
    public bool triggerOnlyOnce = true;

    private bool isTriggered = false;

    private void Awake()
    {
        // 콜라이더의 Is Trigger 속성을 안전하게 강제 활성화
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;

        // 플레이어 태그를 가진 캐릭터가 밟았을 때만 작동
        if (collision.CompareTag("Player"))
        {
            // 1. 사용할 가이드라인 컴포넌트 결정
            PathGuideLine targetGuide = guideLine;
            
            // 수동 할당이 안 되어 있다면 싱글톤 인스턴스 확인
            if (targetGuide == null)
            {
                targetGuide = PathGuideLine.instance;
            }

            // 2. 만약 가이드라인 오브젝트가 '비활성화(SetActive(false))' 상태로 시작하여 인스턴스를 찾지 못했다면 씬 전체에서 검색
            if (targetGuide == null)
            {
                PathGuideLine[] allGuides = Resources.FindObjectsOfTypeAll<PathGuideLine>();
                if (allGuides != null && allGuides.Length > 0)
                {
                    targetGuide = allGuides[0];
                }
            }

            if (targetGuide != null)
            {
                // 3. 꺼져 있을 수 있는 가이드라인 오브젝트를 강제로 켭니다.
                targetGuide.gameObject.SetActive(true);

                if (action == TriggerAction.SetTarget)
                {
                    if (destinationTarget != null)
                    {
                        targetGuide.SetTarget(destinationTarget);
                        Debug.Log($"👣 [GuideTrigger] 가이드라인이 '{destinationTarget.name}' 방향으로 활성화되었습니다.");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [GuideTrigger] {gameObject.name}에 연결된 목적지(Destination Target)가 비어있습니다!");
                    }
                }
                else if (action == TriggerAction.ClearTarget)
                {
                    targetGuide.ClearTarget();
                    Debug.Log("👣 [GuideTrigger] 가이드라인이 꺼졌습니다.");
                }

                // 일회성 트리거라면 다시 작동하지 않게 처리
                if (triggerOnlyOnce)
                {
                    isTriggered = true;
                    gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("⚠️ [GuideTrigger] 씬 내에서 제어할 PathGuideLine 오브젝트를 찾을 수 없습니다!");
            }
        }
    }
}

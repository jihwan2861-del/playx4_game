using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 패링 성공 횟수를 감지하여 지정한 횟수에 도달하면 인스펙터(UnityEvent)로 이벤트를 발동시키는 
/// 경량 유틸리티 컴포넌트입니다. (에디터 시퀀스 제작용)
/// </summary>
public class ParryCounterTrigger : MonoBehaviour
{
    public static ParryCounterTrigger instance;

    [Header("=== 목표 패링 횟수 ===")]
    [Tooltip("이 횟수만큼 패링에 성공하면 이벤트가 발동합니다.")]
    public int targetParryCount = 2;

    [Header("=== 달성 시 실행할 이벤트 ===")]
    [Tooltip("목표 횟수 도달 시 유니티 인스펙터에서 실행할 액션들을 연결하세요.")]
    public UnityEvent onTargetReached;

    private int currentParryCount = 0;

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// PlayerMoving에서 패링 성공 시 호출됩니다.
    /// </summary>
    public void OnParrySuccess()
    {
        if (!enabled) return;

        currentParryCount++;
        Debug.Log($"🛡️ [ParryCounter] 패링 성공 감지: ({currentParryCount} / {targetParryCount})");

        if (currentParryCount >= targetParryCount)
        {
            Debug.Log("🎉 [ParryCounter] 목표 패링 횟수 도달! 등록된 이벤트를 실행합니다.");
            onTargetReached.Invoke();
            
            // 한 번 실행된 후에는 이벤트를 반복 실행하지 않도록 컴포넌트를 비활성화합니다.
            enabled = false;
        }
    }
}

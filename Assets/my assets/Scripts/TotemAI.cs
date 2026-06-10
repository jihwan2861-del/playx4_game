using System.Collections;
using UnityEngine;

/// <summary>
/// 맵에 배치된 각 토템(원, 네모, 세모)의 발사 및 휴식 상태 주기를 관리하는 공통 AI 두뇌 스크립트입니다.
/// </summary>
public class TotemAI : MonoBehaviour
{
    public enum TotemType { Circle, Square, Triangle }

    [Header("=== 토템 설정 ===")]
    [Tooltip("토템의 타입 분류")]
    public TotemType totemType;
    [Tooltip("탄막을 지속해서 발사할 시간 (초)")]
    public float activeDuration = 4.0f;
    [Tooltip("탄막 발사 중지 후 대기할 시간 (초)")]
    public float restDuration = 2.0f;

    private ITotemPattern activePattern;
    private bool isAiRunning = true;

    private void Start()
    {
        // 동일 오브젝트에 부착된 도형 고유의 탄막 패턴 스크립트 캐싱
        activePattern = GetComponent<ITotemPattern>();
        
        if (activePattern != null)
        {
            StartCoroutine(TotemLoopRoutine());
        }
        else
        {
            Debug.LogError($"🛑 [TotemAI] {gameObject.name}에 ITotemPattern 인터페이스를 구현한 패턴 스크립트가 없습니다!");
        }
    }

    private IEnumerator TotemLoopRoutine()
    {
        // 씬 로드 후 안전하게 1초 대기 후 루프 개시
        yield return new WaitForSeconds(1.0f);

        while (isAiRunning)
        {
            // 1. 탄막 발사 개시
            activePattern.StartPattern();
            yield return new WaitForSeconds(activeDuration);

            // 2. 탄막 발사 정지 및 휴식
            activePattern.StopPattern();
            yield return new WaitForSeconds(restDuration);
        }
    }

    /// <summary>
    /// 보스 페이즈 전환이나 씬 클리어 시 토템의 탄막 사격을 중단하기 위해 외부에서 호출합니다.
    /// </summary>
    public void StopTotemAI()
    {
        isAiRunning = false;
        if (activePattern != null)
        {
            activePattern.StopPattern();
        }
        StopAllCoroutines();
    }
}

/// <summary>
/// 각 토템의 개별 수학 탄막 패턴 컴포넌트들이 필수 구현해야 하는 인터페이스 규격입니다.
/// </summary>
public interface ITotemPattern
{
    void StartPattern();
    void StopPattern();
}

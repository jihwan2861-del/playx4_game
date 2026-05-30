using System.Collections;
using UnityEngine;

/// <summary>
/// 역경직(Hit Stop) 시스템: TimeScale을 순간적으로 0으로 만들어 찰진 타격감을 연출합니다.
/// 씬의 빈 게임오브젝트에 부착하세요. 싱글턴으로 어디서든 호출 가능합니다.
/// </summary>
public class HitStop : MonoBehaviour
{
    public static HitStop instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 역경직을 발동합니다. duration 동안 게임 시간이 멈췄다가 복구됩니다.
    /// </summary>
    /// <param name="duration">역경직 지속 시간 (실제 시간 기준, 초)</param>
    public void Do(float duration)
    {
        // 이미 역경직 중이면 중복 실행하지 않음
        if (Time.timeScale == 0f) return;
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}

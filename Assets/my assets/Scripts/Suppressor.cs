using UnityEngine;

/// <summary>
/// 플레이어가 획득하면 보스 등장 시간을 단축시키는 억제기 아이템입니다.
/// </summary>
public class Suppressor : MonoBehaviour
{
    public float timeReduction = 10f; // 줄여줄 시간 (초)
    public GameObject pickupEffect;   // 획득 시 이펙트 (옵션)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어와 부딪혔을 때
        if (collision.CompareTag("Player"))
        {
            // LevelController의 타이머 단축 함수 호출
            if (LevelController.instance != null)
            {
                LevelController.instance.ReduceBossTimer(timeReduction);
            }

            // 시각적 피드백 (이펙트)
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            // 사운드나 로그 추가 가능
            Debug.Log($"🔋 [억제기 획득!] 보스 등장 시간이 {timeReduction}초 단축되었습니다!");

            // 아이템 파괴
            Destroy(gameObject);
        }
    }
}

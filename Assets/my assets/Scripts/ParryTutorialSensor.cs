using UnityEngine;

/// <summary>
/// 플레이어의 자식 오브젝트에 부착되어 날아오는 적의 총알을 감지하고,
/// 패링 연출 매니저(ParryStop)에 신호를 전달하는 물리 트리거 센서 스크립트입니다.
/// </summary>
public class ParryTutorialSensor : MonoBehaviour
{
    void Start()
    {
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
        // 충돌한 물체가 적의 투사체(Projectile)인지 판별
        Projectile proj = collision.GetComponent<Projectile>();
        if (proj == null)
        {
            proj = collision.GetComponentInParent<Projectile>();
        }

        if (proj != null && proj.enemyBullet && collision.gameObject.activeInHierarchy)
        {
            // ParryStop 연출 매니저가 있을 때만 총알 감지 신호를 보냄
            if (ParryStop.instance != null)
            {
                ParryStop.instance.OnBulletDetected();
            }
        }
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 2의 공전형 돌 투사체 오브젝트입니다.
/// 보스의 궤도를 따라 이동하며, 스스로 자전 회전하고 플레이어 충돌 시 사라지지 않고 지속적으로 피해를 줍니다.
/// </summary>
public class Boss2RockProjectile : MonoBehaviour
{
    [Header("공격 및 자전 설정")]
    [Tooltip("플레이어 충돌 시 입히는 피해량")]
    public int damage = 1;
    [Tooltip("돌 자체의 실시간 자전(스핀) 속도")]
    public float spinSpeed = 250f;

    [Header("이전 포물선 투하 호환성 필드 (직렬화 유지용)")]
    public float flySpeed = 5f;
    public float rotationSpeed = 250f;
    public AnimationCurve heightCurve;
    public GameObject bulletPrefab;
    public int bulletCount = 16;
    public GameObject explosionEffect;

    private void Update()
    {
        // 돌 자체의 Z축 자전 구르기 연출
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 태그 확인 시 피해 연동 (사라지지 않는 돌맹이)
        if (collision.CompareTag("Player"))
        {
            if (Player.instance != null)
            {
                // 플레이어에 데미지 적용 (자체 무적 시간 동안 중복 피해 방지)
                Player.instance.GetDamage(damage, gameObject);
                Debug.Log("💥 [보스2 공전 돌맹이] 플레이어와 충돌하여 데미지를 1 입혔습니다! (충돌 후 파괴 없음)");
            }
        }
    }

    /// <summary>
    /// 이전 스크립트 컴포넌트 호출 오류 방지용 더미 메서드
    /// </summary>
    public void Launch(Vector3 target)
    {
        // 아우렐리온 솔 방식에서는 BossPattern_RockDrop이 직접 위치를 배치/공전 제어하므로 Launch를 무시합니다.
    }
}

using UnityEngine;

/// <summary>
/// 야스오 바람장막처럼 적의 투사체(Projectile)가 닿으면 파괴하는 데드존 스크립트입니다.
/// </summary>
public class Deadzone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 부딪힌 오브젝트가 투사체(총알)인지 확인합니다.
        Projectile p = collision.GetComponent<Projectile>();
        if (p != null && p.enemyBullet)
        {
            Destroy(p.gameObject);
            return; // 파괴했으므로 여기서 종료
        }

        // 2. 수리검 같은 적 본체(Enemy)인지 확인합니다.
        Enemy e = collision.GetComponent<Enemy>();
        if (e != null)
        {
            Destroy(e.gameObject);
            return;
        }

        // 3. 스크립트가 없더라도 'Enemy'나 'Projectile' 태그가 붙어있다면 무조건 파괴합니다.
        if (collision.CompareTag("Enemy") || collision.CompareTag("Projectile"))
        {
            Destroy(collision.gameObject);
        }
    }
}

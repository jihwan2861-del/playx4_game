using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the damage and defines whether the projectile belongs to the ‘Enemy’ or to the ‘Player’, whether the projectile is destroyed in the collision, or not and amount of damage.
/// </summary>

public class Projectile : MonoBehaviour {

    [Tooltip("Damage which a projectile deals to another object. Integer")]
    public int damage;

    [Tooltip("Whether the projectile belongs to the ‘Enemy’ or to the ‘Player’")]
    public bool enemyBullet;

    [Tooltip("Whether the projectile is destroyed in the collision, or not")]
    public bool destroyedByCollision;

    private void OnTriggerEnter2D(Collider2D collision) //when a projectile collides with another object
    {
        if (enemyBullet && collision.tag == "Player") //if anoter object is 'player' or 'enemy sending the command of receiving the damage
        {
            Player.instance.GetDamage(damage, gameObject); 
            if (destroyedByCollision)
                Destruction();
        }
        else if (!enemyBullet && collision.tag == "Enemy")
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.GetDamage(damage);
            }
            
            if (destroyedByCollision)
                Destruction();
        }
    }

    void Destruction() 
    {
        // 오브젝트 풀링을 위해 파괴하는 대신 꺼줍니다(비활성화).
        gameObject.SetActive(false);
    }
}



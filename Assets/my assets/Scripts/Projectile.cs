using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 투사체의 공격 판정을 내리며, 적인지 아군인지 구분하여 데미지를 입히고 소멸 처리하는 스크립트입니다.
/// </summary>
public class Projectile : MonoBehaviour {

    [Tooltip("Damage which a projectile deals to another object. Integer")]
    public int damage;

    [Tooltip("Whether the projectile belongs to the 'Enemy' or to the 'Player'")]
    public bool enemyBullet;

    [Tooltip("Whether the projectile is destroyed in the collision, or not")]
    public bool destroyedByCollision;

    [Header("=== 아군 투사체 명중 피드백 (Hit Feedback) ===")]
    [Tooltip("적에게 명중했을 때 재생할 효과음 클립")]
    public AudioClip hitSound;
    [Range(0f, 1f)]
    [Tooltip("명중 효과음 볼륨")]
    public float hitVolume = 0.5f;

    [Tooltip("명중 시 카메라 흔들림 강도 (0이면 흔들지 않음)")]
    public float cameraShakeMagnitude = 0.05f;
    [Tooltip("명중 시 카메라 흔들림 시간 (초)")]
    public float cameraShakeDuration = 0.08f;

    [Tooltip("명중 시 순간적인 역경직(히트스탑) 멈춤 시간 (초)")]
    public float hitStopDuration = 0.02f;

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
            if (enemy == null)
            {
                enemy = collision.GetComponentInParent<Enemy>();
            }

            if (enemy != null)
            {
                enemy.GetDamage(damage);

                // 🔊 1. 명중 오디오 이펙트 재생 (투사체 위치 기준)
                if (hitSound != null)
                {
                    AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
                }

                // 🎥 2. 카메라 미세 진동 효과
                if (cameraShakeMagnitude > 0f && Camera.main != null)
                {
                    CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                    if (cam != null)
                    {
                        cam.Shake(cameraShakeDuration, cameraShakeMagnitude);
                    }
                }

                // ⏳ 3. 물리적인 묵직함을 위한 찰나의 히트스탑
                if (hitStopDuration > 0f && HitStop.instance != null)
                {
                    HitStop.instance.Do(hitStopDuration);
                }
            }

            // 장애물(BreakableObject) 피격 처리
            BreakableObject breakable = collision.GetComponent<BreakableObject>();
            if (breakable == null)
            {
                breakable = collision.GetComponentInParent<BreakableObject>();
            }
            if (breakable != null)
            {
                breakable.TakeDamage(damage);
            }
            
            if (destroyedByCollision)
                Destruction();
        }
    }

    void Destruction() 
    {
        // 오브젝트 풀링 환원 및 비활성화
        gameObject.SetActive(false);
    }
}

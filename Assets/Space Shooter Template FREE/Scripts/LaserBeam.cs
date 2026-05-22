using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지속적으로 닿아있을 때 플레이어에게 데미지를 주는 레이저 스크립트입니다.
/// </summary>
public class LaserBeam : MonoBehaviour
{
    [Tooltip("레이저가 주는 데미지")]
    public int damage = 1;

    [Tooltip("레이저가 발사되어 유지되는 시간")]
    public float lifeTime = 2f;

    [Tooltip("몇 초 간격으로 데미지가 계속 들어갈지 결정")]
    public float damageTickRate = 0.5f;

    [Header("사운드 설정")]
    [Tooltip("레이저 작동 시 재생할 루핑 효과음")]
    public AudioClip laserSFX;

    private float nextDamageTime;
    private AudioSource audioSource;

    private void Start()
    {
        // lifeTime 이후에 자동으로 파괴됩니다.
        Destroy(gameObject, lifeTime);

        if (laserSFX != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = laserSFX;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0.5f;
            audioSource.spatialBlend = 0f; // 2D Sound
            audioSource.Play();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // OnTriggerStay2D는 콜라이더가 겹쳐있는 동안 계속 호출됩니다.
        // 플레이어이고, 틱 시간이 지났을 때만 데미지를 줍니다.
        if (collision.CompareTag("Player") && Time.time >= nextDamageTime)
        {
            Player.instance.GetDamage(damage, gameObject);
            nextDamageTime = Time.time + damageTickRate;
        }
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// 데미지를 입으면 HP를 깎고, HP가 0이 되면 파괴되는 가장 심플한 적(Enemy) 스크립트입니다.
/// </summary>
public class Enemy : MonoBehaviour 
{
    [Header("체력 설정")]
    [Tooltip("Health points in integer")]
    public int health = 10;

    [Header("이펙트 설정")]
    [Tooltip("VFX prefab generating after destruction")]
    public GameObject destructionVFX;
    [Tooltip("Hit effect prefab")]
    public GameObject hitEffect;

    [Header("피격 머티리얼 설정")]
    [Tooltip("피격 시 순간적으로 교체할 머티리얼 (예: enemy_Hit)")]
    public Material hitMaterial;

    // --- 기존 스크립트 및 프리팹 데이터 호환성을 위해 남겨둔 미사용 더미 변수들 ---
    [HideInInspector] public GameObject Projectile;
    [HideInInspector] public int shotChance;
    [HideInInspector] public float shotTimeMin, shotTimeMax;
    [HideInInspector] public float homingMissileNerf = 3f;
    [HideInInspector] public GameObject overrideVisualPrefab;
    [HideInInspector] public string attackAnimTrigger = "Attack";

    private Coroutine flashCoroutine;

    /// <summary>
    /// 외부(투사체 등)에서 데미지를 줄 때 호출됩니다.
    /// </summary>
    public void GetDamage(int damage) 
    {
        health -= damage;

        // 1. 피격 이펙트 생성 및 0.3초 후 소멸 처리
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity, transform);
            Destroy(effect, 0.3f);
        }

        // 2. 몬스터 본체 피격 연출 (머티리얼 교체 또는 색상 교체)
        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        // 체력이 0 이하가 되면 소멸
        if (health <= 0)
        {
            Destruction();
        }
    }

    /// <summary>
    /// 피격 시 머티리얼을 교체하거나 색상을 변경한 후, 0.3초 뒤에 복구하는 코루틴입니다.
    /// </summary>
    private IEnumerator HitFlashRoutine()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 원래 상태 저장
            Material originalMaterial = sr.material;
            Color originalColor = sr.color;

            if (hitMaterial != null)
            {
                // 사용자가 등록한 피격 전용 머티리얼로 교체 (예: 완전 흰색/빨간색 실루엣)
                sr.material = hitMaterial;
            }
            else
            {
                // 등록된 머티리얼이 없는 경우 폴백으로 기본 스프라이트 색상을 붉게 칠함
                sr.color = new Color(1f, 0.3f, 0.3f, 1f);
            }

            yield return new WaitForSeconds(0.3f); // 0.3초 유지

            // 원래 상태로 완벽히 복구
            if (sr != null)
            {
                sr.material = originalMaterial;
                sr.color = originalColor;
            }
        }
    }

    /// <summary>
    /// 적이 사망했을 때 호출됩니다.
    /// </summary>
    private void Destruction()
    {
        // 사망 이펙트 생성
        if (destructionVFX != null)
        {
            Instantiate(destructionVFX, transform.position, Quaternion.identity);
        }

        // 오브젝트 파괴
        Destroy(gameObject);
    }
}

using System.Collections;
using UnityEngine;

/// <summary>
/// 적 및 보스의 체력(HP)을 관리하며, 피격 시 하얀색 플래시 연출 및 0이 되면 파괴 처리하는 스크립트입니다.
/// </summary>
public class Enemy : MonoBehaviour 
{
    [Header("체력")]
    [Tooltip("Health points in integer")]
    public int health = 10;

    [Header("이펙트")]
    [Tooltip("VFX prefab generating after destruction")]
    public GameObject destructionVFX;
    [Tooltip("Hit effect prefab")]
    public GameObject hitEffect;

    [Header("피격 머티리얼")]
    [Tooltip("피격 시 적용할 머티리얼 (지정하지 않으면 기본 흰색 플래시가 적용됩니다)")]
    public Material hitMaterial;

    // --- 구 스크립트들과의 하위 호환성 유지를 위한 더미 변수 목록 ---
    [HideInInspector] public GameObject Projectile;
    [HideInInspector] public int shotChance;
    [HideInInspector] public float shotTimeMin, shotTimeMax;
    [HideInInspector] public float homingMissileNerf = 3f;
    [HideInInspector] public GameObject overrideVisualPrefab;
    [HideInInspector] public string attackAnimTrigger = "Attack";

    // --- 원본 상태 캐싱용 변수 (버그 원천 차단) ---
    private Material originalMaterial;
    private Color originalColor;
    private bool isOriginalCached = false;
    private Material tempWhiteMaterial; // 흰색 플래시용 임시 머티리얼

    private Coroutine flashCoroutine;

    private void Awake()
    {
        // 텍스처의 형태를 유지한 채 완전한 하얀색으로 칠해주는 유니티 내장 셰이더 생성
        tempWhiteMaterial = new Material(Shader.Find("GUI/Text Shader"));
    }

    private void Start()
    {
        CacheOriginalState();
    }

    private void CacheOriginalState()
    {
        if (isOriginalCached) return;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalMaterial = sr.material;
            originalColor = sr.color;
            isOriginalCached = true;
        }
    }

    /// <summary>
    /// 외부(투사체 등)에서 데미지를 가할 때 호출됩니다.
    /// </summary>
    public void GetDamage(int damage) 
    {
        health -= damage;

        // 1. 피격 파티클 이펙트 스폰
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity, transform);
            Destroy(effect, 0.1f);
        }

        // 2. 피격 컬러 플래시 실행 (0.05초 흰색 번쩍임)
        if (gameObject.activeInHierarchy)
        {
            CacheOriginalState(); // 안전 예외 처리: Start가 돌기 전 첫 타격을 맞았을 경우 대비
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        // 체력이 0 이하가 되면 파괴
        if (health <= 0)
        {
            Destruction();
        }
    }

    /// <summary>
    /// 피격 색상으로 변경 후 0.05초 뒤에 최초 원본 상태로 돌려놓는 연출 코루틴입니다.
    /// </summary>
    private IEnumerator HitFlashRoutine()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 1. 피격 효과 적용 (우선순위: 지정한 hitMaterial -> 없으면 자체 제작한 흰색 셰이더)
            if (hitMaterial != null)
            {
                sr.material = hitMaterial;
            }
            else if (tempWhiteMaterial != null)
            {
                sr.material = tempWhiteMaterial;
                sr.color = Color.white;
            }
            else
            {
                sr.color = Color.white;
            }

            // 2. 단축된 지속 시간 (0.05초 대기)
            yield return new WaitForSeconds(0.05f);

            // 3. 무조건 원래 상태(최초 저장된 원본)로 안전하게 복구
            if (sr != null)
            {
                sr.material = originalMaterial;
                sr.color = originalColor;
            }
        }
    }

    /// <summary>
    /// 파괴 시의 연출 및 오브젝트 삭제를 수행합니다.
    /// </summary>
    private void Destruction()
    {
        if (destructionVFX != null)
        {
            Instantiate(destructionVFX, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}

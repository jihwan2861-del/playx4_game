using System.Collections;
using UnityEngine;

/// <summary>
/// 범용 파괴 가능 오브젝트 컴포넌트입니다.
/// 마우스 클릭 공격(해킹 펄스)이나 패리 반격 미사일로 파괴할 수 있습니다.
/// 스테이지에 배치할 상자, 배럴, 방벽 등 다양한 환경 파괴물에 사용합니다.
/// 
/// 사용법:
/// 1. 파괴하고 싶은 오브젝트에 이 컴포넌트를 추가합니다.
/// 2. 오브젝트의 Tag를 "Enemy"로 설정합니다 (기존 Projectile과 호환).
/// 3. Collider2D를 추가합니다 (Trigger 여부는 자유).
/// 4. health, destructionVFX, dropItem 등을 설정합니다.
/// </summary>
public class BreakableObject : MonoBehaviour
{
    [Header("Health (체력)")]
    [Tooltip("파괴되기까지 필요한 총 데미지")]
    public int health = 3;

    [Header("Visual Effects (시각 효과)")]
    [Tooltip("파괴 시 생성할 이펙트 프리팹 (없으면 절차적으로 생성)")]
    public GameObject destructionVFX;

    [Tooltip("피격 시 생성할 이펙트 프리팹 (없으면 컬러 플래시만)")]
    public GameObject hitEffect;

    [Header("Drop Item (아이템 드롭)")]
    [Tooltip("파괴 시 드롭할 아이템 프리팹 (Bonus 등)")]
    public GameObject dropItem;

    [Tooltip("아이템 드롭 확률 (0~1)")]
    [Range(0f, 1f)]
    public float dropChance = 0.5f;

    [Header("Mission Integration (미션 연동)")]
    [Tooltip("파괴 시 미션 진행에 사용할 키워드")]
    public string missionKeyword = "파괴";

    private Coroutine flashCoroutine;
    private bool isDestroyed = false;

    /// <summary>
    /// 외부에서 데미지를 적용합니다. (HackPulseProjectile, Projectile 등에서 호출)
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        health -= damage;

        // 피격 이펙트
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity, transform);
        }

        // 피격 타격 피드백 (빨갛게 깜빡임)
        if (gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        if (health <= 0)
        {
            Destruction();
        }
    }

    /// <summary>
    /// 피격 시 빨갛게 깜빡이는 타격 피드백 연출입니다.
    /// </summary>
    private IEnumerator HitFlashRoutine()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0) yield break;

        // 원래 색상 저장
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) originalColors[i] = renderers[i].color;
        }

        // 피격용 강렬한 빨간색으로 변경
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = new Color(1f, 0.2f, 0.2f, 1f);
            }
        }

        yield return new WaitForSeconds(0.07f);

        // 원래 색상으로 복원
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = originalColors[i];
            }
        }
    }

    /// <summary>
    /// 오브젝트 파괴 처리입니다.
    /// </summary>
    private void Destruction()
    {
        isDestroyed = true;

        // 1. 파괴 이펙트
        if (destructionVFX != null)
        {
            Instantiate(destructionVFX, transform.position, Quaternion.identity);
        }
        else
        {
            // 프리팹이 없으면 절차적으로 간단한 파괴 이펙트 생성
            StartCoroutine(ProceduralDestructionEffect(transform.position));
        }

        // 2. 히트스톱 (파괴감)
        if (HitStop.instance != null)
        {
            HitStop.instance.Do(0.08f);
        }

        // 3. 카메라 흔들림
        if (Camera.main != null)
        {
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.Shake(0.15f, 0.3f);
            }
        }

        // 4. 미션 진행 연동
        if (MissionPanel.instance != null && !string.IsNullOrEmpty(missionKeyword))
        {
            MissionPanel.instance.AddProgressByKeyword(missionKeyword, 1);
        }

        // 5. 아이템 드롭
        if (dropItem != null && Random.value <= dropChance)
        {
            Instantiate(dropItem, transform.position, Quaternion.identity);
        }

        // 6. 오브젝트 파괴
        Destroy(gameObject);
    }

    /// <summary>
    /// 절차적으로 파괴 이펙트를 생성합니다. (destructionVFX 프리팹이 없을 때 사용)
    /// </summary>
    private IEnumerator ProceduralDestructionEffect(Vector3 position)
    {
        // 파편 4~6개가 사방으로 튀는 효과
        int fragmentCount = Random.Range(4, 7);
        GameObject[] fragments = new GameObject[fragmentCount];
        Vector3[] velocities = new Vector3[fragmentCount];

        // 원본 색상 가져오기
        Color baseColor = Color.white;
        SpriteRenderer mainSr = GetComponent<SpriteRenderer>();
        if (mainSr != null) baseColor = mainSr.color;

        for (int i = 0; i < fragmentCount; i++)
        {
            fragments[i] = new GameObject($"Fragment_{i}");
            fragments[i].transform.position = position;

            SpriteRenderer sr = fragments[i].AddComponent<SpriteRenderer>();
            sr.sprite = PlayerClickAttack.CreateCircleSprite(16);
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 55;
            
            // 원본 색상에서 약간씩 변형된 색상 사용
            float colorVariation = Random.Range(-0.15f, 0.15f);
            sr.color = new Color(
                Mathf.Clamp01(baseColor.r + colorVariation),
                Mathf.Clamp01(baseColor.g + colorVariation),
                Mathf.Clamp01(baseColor.b + colorVariation),
                1f
            );

            float fragScale = Random.Range(0.15f, 0.4f);
            fragments[i].transform.localScale = Vector3.one * fragScale;

            // 랜덤 방향 속도
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speed = Random.Range(3f, 7f);
            velocities[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * speed;
        }

        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < fragmentCount; i++)
            {
                if (fragments[i] != null)
                {
                    // 이동 (감속)
                    fragments[i].transform.position += velocities[i] * Time.unscaledDeltaTime * (1f - t);

                    // 축소 + 페이드
                    float fadeScale = Mathf.Lerp(fragments[i].transform.localScale.x, 0f, t);
                    fragments[i].transform.localScale = Vector3.one * fadeScale;

                    SpriteRenderer sr = fragments[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = Mathf.Lerp(1f, 0f, t);
                        sr.color = c;
                    }

                    // 회전
                    fragments[i].transform.Rotate(0, 0, 360f * Time.unscaledDeltaTime);
                }
            }

            yield return null;
        }

        // 정리
        foreach (var frag in fragments)
        {
            if (frag != null) Destroy(frag);
        }
    }

    /// <summary>
    /// 에디터에서 오브젝트 범위를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}

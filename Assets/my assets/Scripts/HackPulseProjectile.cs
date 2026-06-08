using System.Collections;
using UnityEngine;

/// <summary>
/// 클릭 공격 시 발사되는 해킹 펄스 투사체입니다.
/// 대상(target)을 추적하여 도달하면 데미지를 주고 소멸합니다.
/// 모든 비주얼(스프라이트, 트레일, 히트 이펙트)을 절차적으로 생성하여 외부 에셋 의존성이 없습니다.
/// </summary>
public class HackPulseProjectile : MonoBehaviour
{
    [Header("Target (자동 설정)")]
    [HideInInspector] public Transform target;
    [HideInInspector] public int damage = 5;
    [HideInInspector] public float speed = 25f;
    [HideInInspector] public Color color = new Color(0f, 0.8f, 1f, 1f);
    [HideInInspector] public float scale = 1.2f;

    [Header("Sound")]
    [HideInInspector] public AudioClip hitSFX;
    [HideInInspector] public float hitSFXVolume = 0.6f;

    // 도착 판정 거리
    private float arrivalDistance = 0.4f;
    // 최대 생존 시간 (대상 추적 실패 시 자동 소멸)
    private float maxLifetime = 5f;
    private float spawnTime;

    private SpriteRenderer sr;
    private TrailRenderer trail;

    private void Start()
    {
        spawnTime = Time.time;

        // --- 절차적 비주얼 생성 ---
        SetupVisuals();
        SetupTrail();
    }

    private void Update()
    {
        // 수명 초과 시 자동 소멸
        if (Time.time - spawnTime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 대상이 사라졌으면 (적이 먼저 죽었으면) 소멸
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            // 대상 소실 시 페이드 아웃 후 소멸
            StartCoroutine(FadeAndDestroy());
            enabled = false; // Update 중복 호출 방지
            return;
        }

        // --- 대상 추적 이동 ---
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // 이동 방향으로 회전 (시각적 방향 표시)
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // 스프라이트 맥동 효과 (살아있는 에너지 느낌)
        if (sr != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 25f) * 0.15f;
            float currentScale = scale * pulse;
            // 트레일이 달려 있으므로 sr의 트랜스폼은 건드리지 않고 색상만 맥동
            Color pulseColor = color;
            pulseColor.a = 0.8f + Mathf.Sin(Time.time * 30f) * 0.2f;
            sr.color = pulseColor;
        }

        // --- 도착 판정 ---
        float dist = Vector2.Distance(transform.position, target.position);
        if (dist <= arrivalDistance)
        {
            OnArrival();
        }
    }

    /// <summary>
    /// 대상에 도달했을 때 데미지를 적용하고 히트 이펙트를 생성합니다.
    /// </summary>
    private void OnArrival()
    {
        if (target != null)
        {
            // Enemy 컴포넌트에 데미지 적용
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy == null) enemy = target.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.GetDamage(damage);
            }

            // BreakableObject 컴포넌트에 데미지 적용
            BreakableObject breakable = target.GetComponent<BreakableObject>();
            if (breakable != null)
            {
                breakable.TakeDamage(damage);
            }

            // 히트 이펙트 생성
            SpawnHitEffect(target.position);

            // 히트 사운드
            if (hitSFX != null)
            {
                AudioSource.PlayClipAtPoint(hitSFX, target.position, hitSFXVolume);
            }

            // 약간의 히트스톱 (타격감)
            if (HitStop.instance != null)
            {
                HitStop.instance.Do(0.04f);
            }

            // 약한 카메라 흔들림
            if (Camera.main != null)
            {
                CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
                if (camFollow != null)
                {
                    camFollow.Shake(0.08f, 0.15f);
                }
            }
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 절차적으로 에너지 볼 스프라이트를 생성합니다.
    /// </summary>
    private void SetupVisuals()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = PlayerClickAttack.CreateCircleSprite(64);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 52;
        sr.color = color;
        transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// 투사체 뒤에 잔상 트레일을 추가합니다.
    /// </summary>
    private void SetupTrail()
    {
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.15f;
        trail.startWidth = 0.3f * scale;
        trail.endWidth = 0f;
        trail.sortingLayerName = "Default";
        trail.sortingOrder = 51;

        // 트레일 머티리얼 (기본 스프라이트 머티리얼 사용)
        trail.material = new Material(Shader.Find("Sprites/Default"));

        // 트레일 색상 그라디언트
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(new Color(color.r * 0.5f, color.g * 0.5f, color.b, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;

        // 모서리 둥글게
        trail.numCornerVertices = 4;
        trail.numCapVertices = 4;
        trail.minVertexDistance = 0.05f;
    }

    /// <summary>
    /// 대상 위치에 전기 스파크 형태의 히트 이펙트를 생성합니다.
    /// </summary>
    private void SpawnHitEffect(Vector3 position)
    {
        // 중심 임팩트 플래시
        StartCoroutine(HitEffectRoutine(position));
    }

    private IEnumerator HitEffectRoutine(Vector3 position)
    {
        // 1. 중심 빛나는 원 (임팩트 코어)
        GameObject coreObj = new GameObject("HitCore");
        coreObj.transform.position = position;
        SpriteRenderer coreSr = coreObj.AddComponent<SpriteRenderer>();
        coreSr.sprite = PlayerClickAttack.CreateCircleSprite(48);
        coreSr.sortingLayerName = "Default";
        coreSr.sortingOrder = 56;
        coreSr.color = Color.white; // 초기 흰색 번쩍

        // 2. 외곽 링 이펙트 (쇼크웨이브)
        GameObject ringObj = new GameObject("HitRing");
        ringObj.transform.position = position;
        SpriteRenderer ringSr = ringObj.AddComponent<SpriteRenderer>();
        ringSr.sprite = CreateRingSprite(64);
        ringSr.sortingLayerName = "Default";
        ringSr.sortingOrder = 55;
        ringSr.color = color;

        // 3. 전기 스파크 파티클 (4개의 작은 점이 사방으로 퍼짐)
        GameObject[] sparks = new GameObject[4];
        Vector3[] sparkDirs = {
            new Vector3(1, 0.5f, 0).normalized,
            new Vector3(-0.7f, 0.8f, 0).normalized,
            new Vector3(0.5f, -1f, 0).normalized,
            new Vector3(-1f, -0.3f, 0).normalized
        };

        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i] = new GameObject($"Spark_{i}");
            sparks[i].transform.position = position;
            SpriteRenderer sparkSr = sparks[i].AddComponent<SpriteRenderer>();
            sparkSr.sprite = PlayerClickAttack.CreateCircleSprite(16);
            sparkSr.sortingLayerName = "Default";
            sparkSr.sortingOrder = 57;
            sparkSr.color = new Color(color.r, color.g, color.b, 1f);
            sparks[i].transform.localScale = Vector3.one * 0.3f;
        }

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // 코어: 확장 + 페이드 (흰색 → 시안 → 투명)
            if (coreObj != null)
            {
                float coreScale = Mathf.Lerp(0.5f, 2.0f, t);
                coreObj.transform.localScale = new Vector3(coreScale, coreScale, 1f);
                coreSr.color = Color.Lerp(Color.white, new Color(color.r, color.g, color.b, 0f), t);
            }

            // 링: 빠르게 확장 + 페이드
            if (ringObj != null)
            {
                float ringScale = Mathf.Lerp(0.3f, 3.0f, t);
                ringObj.transform.localScale = new Vector3(ringScale, ringScale, 1f);
                Color rc = ringSr.color;
                rc.a = Mathf.Lerp(0.8f, 0f, t);
                ringSr.color = rc;
            }

            // 스파크: 사방으로 퍼지며 축소 + 페이드
            for (int i = 0; i < sparks.Length; i++)
            {
                if (sparks[i] != null)
                {
                    sparks[i].transform.position = position + sparkDirs[i] * t * 1.5f;
                    float sparkScale = Mathf.Lerp(0.3f, 0.05f, t);
                    sparks[i].transform.localScale = new Vector3(sparkScale, sparkScale, 1f);
                    SpriteRenderer ssr = sparks[i].GetComponent<SpriteRenderer>();
                    if (ssr != null)
                    {
                        Color sc = ssr.color;
                        sc.a = Mathf.Lerp(1f, 0f, t);
                        ssr.color = sc;
                    }
                }
            }

            yield return null;
        }

        // 정리
        Destroy(coreObj);
        Destroy(ringObj);
        foreach (var spark in sparks)
        {
            if (spark != null) Destroy(spark);
        }
    }

    /// <summary>
    /// 절차적으로 링(도넛) 형태의 스프라이트를 생성합니다.
    /// </summary>
    private Sprite CreateRingSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        float center = size / 2f;
        float outerRadius = size / 2f - 2f;
        float innerRadius = outerRadius - 5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= outerRadius && dist >= innerRadius)
                {
                    float t = 1f - Mathf.Abs(dist - (innerRadius + outerRadius) / 2f) / ((outerRadius - innerRadius) / 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, t));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// 대상 소실 시 페이드 아웃 후 소멸합니다.
    /// </summary>
    private IEnumerator FadeAndDestroy()
    {
        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.8f, 0f, t);
                sr.color = c;
            }

            float fadeScale = Mathf.Lerp(scale, scale * 0.3f, t);
            transform.localScale = Vector3.one * fadeScale;

            yield return null;
        }

        Destroy(gameObject);
    }
}

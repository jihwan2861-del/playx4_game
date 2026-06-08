using System.Collections;
using UnityEngine;

/// <summary>
/// 마우스 좌클릭으로 적(Enemy)이나 부숴지는 오브젝트(BreakableObject)를 클릭하면
/// 롤(League of Legends) 스타일로 해킹 펄스 투사체가 대상을 향해 발사되는 공격 시스템입니다.
/// Player 오브젝트에 부착하여 사용합니다.
/// </summary>
public class PlayerClickAttack : MonoBehaviour
{
    [Header("Attack Settings (공격 설정)")]
    [Tooltip("공격 간격 (초). 낮을수록 연사가 빠름")]
    public float attackCooldown = 0.3f;

    [Tooltip("최대 공격 사거리 (유닛)")]
    public float attackRange = 15f;

    [Tooltip("클릭 공격 기본 데미지")]
    public int attackDamage = 5;

    [Tooltip("투사체 이동 속도")]
    public float projectileSpeed = 25f;

    [Header("Visual Settings (비주얼 설정)")]
    [Tooltip("투사체 색상")]
    public Color projectileColor = new Color(0f, 0.8f, 1f, 1f); // 네온 시안

    [Tooltip("투사체 크기 배율")]
    public float projectileScale = 1.2f;

    [Tooltip("발사 시 총구 이펙트 크기")]
    public float muzzleFlashScale = 0.8f;

    [Header("Cursor Feedback (커서 피드백)")]
    [Tooltip("적 위에 호버 시 사용할 커서 텍스처 (비워두면 기본 색상 변경)")]
    public Texture2D attackCursorTexture;

    [Tooltip("공격 불가 상태 (사거리 밖/쿨다운) 커서 텍스처")]
    public Texture2D disabledCursorTexture;

    [Header("Sound Effects (사운드)")]
    [Tooltip("공격 발사 시 사운드")]
    public AudioClip attackFireSFX;
    [Range(0f, 1f)]
    public float attackFireVolume = 0.5f;

    [Tooltip("공격 적중 시 사운드")]
    public AudioClip attackHitSFX;
    [Range(0f, 1f)]
    public float attackHitVolume = 0.6f;

    // 내부 상태
    private float lastAttackTime = -999f;
    private Camera mainCamera;
    private bool cursorOverTarget = false;

    public static PlayerClickAttack instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 플레이어가 조작 불가 상태인지 확인
        if (!CanAttack())
        {
            return;
        }

        // --- 커서 호버 피드백 ---
        UpdateCursorFeedback();

        // --- 마우스 좌클릭 공격 ---
        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }
    }

    /// <summary>
    /// 현재 공격이 가능한 상태인지 확인합니다.
    /// </summary>
    private bool CanAttack()
    {
        // 패리 경직 중
        if (PlayerMoving.instance != null && PlayerMoving.instance.isParryRecovery)
            return false;

        // 넉백 중
        if (PlayerMoving.instance != null && PlayerMoving.instance.isKnockedBack)
            return false;

        return true;
    }

    /// <summary>
    /// 마우스 위치의 대상을 감지하고 커서를 업데이트합니다.
    /// </summary>
    private void UpdateCursorFeedback()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // 마우스 위치에서 공격 가능한 대상이 있는지 확인
        GameObject target = FindTargetAtPosition(mouseWorldPos);

        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.transform.position);

            if (distance <= attackRange && !IsOnCooldown())
            {
                // 공격 가능 — 공격 커서 표시
                if (!cursorOverTarget)
                {
                    cursorOverTarget = true;
                    if (attackCursorTexture != null)
                    {
                        Cursor.SetCursor(attackCursorTexture, new Vector2(attackCursorTexture.width / 2f, attackCursorTexture.height / 2f), CursorMode.Auto);
                    }
                }
            }
            else
            {
                // 사거리 밖 또는 쿨다운 — 비활성 커서
                if (cursorOverTarget)
                {
                    cursorOverTarget = false;
                    if (disabledCursorTexture != null)
                    {
                        Cursor.SetCursor(disabledCursorTexture, new Vector2(disabledCursorTexture.width / 2f, disabledCursorTexture.height / 2f), CursorMode.Auto);
                    }
                    else
                    {
                        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    }
                }
            }
        }
        else
        {
            // 대상 없음 — 기본 커서 복원
            if (cursorOverTarget)
            {
                cursorOverTarget = false;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
    }

    /// <summary>
    /// 클릭 시 공격을 시도합니다.
    /// </summary>
    private void TryAttack()
    {
        // 쿨다운 확인
        if (IsOnCooldown()) return;

        // 마우스 → 월드 좌표 변환
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // 클릭 지점에서 대상 탐색
        GameObject target = FindTargetAtPosition(mouseWorldPos);
        if (target == null) return;

        // 사거리 확인
        float distance = Vector2.Distance(transform.position, target.transform.position);
        if (distance > attackRange) return;

        // 공격 실행!
        PerformAttack(target);
    }

    /// <summary>
    /// 주어진 월드 좌표에서 공격 가능한 대상을 탐색합니다.
    /// Enemy 태그 또는 BreakableObject 컴포넌트가 있는 오브젝트를 반환합니다.
    /// </summary>
    private GameObject FindTargetAtPosition(Vector3 worldPos)
    {
        // 넓은 범위로 감지 (클릭 정밀도 향상을 위해 작은 원형 영역 사용)
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.5f);

        GameObject bestTarget = null;
        float bestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!hit.gameObject.activeInHierarchy) continue;

            bool isValidTarget = false;

            // Enemy 태그 확인
            if (hit.CompareTag("Enemy"))
            {
                isValidTarget = true;
            }

            // BreakableObject 컴포넌트 확인
            if (!isValidTarget)
            {
                BreakableObject breakable = hit.GetComponent<BreakableObject>();
                if (breakable != null)
                {
                    isValidTarget = true;
                }
            }

            if (isValidTarget)
            {
                float dist = Vector2.Distance(worldPos, hit.transform.position);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestTarget = hit.gameObject;
                }
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// 대상을 향해 해킹 펄스 투사체를 발사합니다.
    /// </summary>
    private void PerformAttack(GameObject target)
    {
        lastAttackTime = Time.time;

        // 1. 발사 이펙트 (작은 총구 플래시)
        SpawnMuzzleFlash();

        // 2. 사운드
        if (attackFireSFX != null)
        {
            AudioSource.PlayClipAtPoint(attackFireSFX, transform.position, attackFireVolume);
        }

        // 3. 해킹 펄스 투사체 생성
        SpawnHackPulse(target);

        // 4. 약한 히트스톱 (발사감)
        if (HitStop.instance != null)
        {
            HitStop.instance.Do(0.03f);
        }

        Debug.Log($"⚡ [클릭 공격] 대상: {target.name} 방향으로 해킹 펄스 발사!");
    }

    /// <summary>
    /// 해킹 펄스 투사체를 생성하고 대상을 향해 발사합니다.
    /// </summary>
    private void SpawnHackPulse(GameObject target)
    {
        GameObject pulseObj = new GameObject("HackPulse");
        pulseObj.transform.position = transform.position;

        // HackPulseProjectile 컴포넌트 추가 및 설정
        HackPulseProjectile pulse = pulseObj.AddComponent<HackPulseProjectile>();
        pulse.target = target.transform;
        pulse.damage = attackDamage;
        pulse.speed = projectileSpeed;
        pulse.color = projectileColor;
        pulse.scale = projectileScale;
        pulse.hitSFX = attackHitSFX;
        pulse.hitSFXVolume = attackHitVolume;
    }

    /// <summary>
    /// 플레이어 위치에서 작은 발사 이펙트를 생성합니다.
    /// </summary>
    private void SpawnMuzzleFlash()
    {
        StartCoroutine(MuzzleFlashRoutine());
    }

    private IEnumerator MuzzleFlashRoutine()
    {
        GameObject flashObj = new GameObject("MuzzleFlash");
        flashObj.transform.position = transform.position;

        SpriteRenderer sr = flashObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(32);
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 55;
        sr.color = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0.9f);
        flashObj.transform.localScale = Vector3.one * muzzleFlashScale;

        // 빠르게 확장 후 소멸
        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float scale = muzzleFlashScale * (1f + t * 1.5f);
            flashObj.transform.localScale = new Vector3(scale, scale, 1f);

            Color c = sr.color;
            c.a = Mathf.Lerp(0.9f, 0f, t);
            sr.color = c;

            yield return null;
        }

        Destroy(flashObj);
    }

    private bool IsOnCooldown()
    {
        return (Time.time - lastAttackTime) < attackCooldown;
    }

    /// <summary>
    /// 절차적으로 원형 스프라이트를 생성합니다. (외부 에셋 불필요)
    /// </summary>
    public static Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        float center = size / 2f;
        float maxRadius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= maxRadius)
                {
                    // 중심이 밝고 가장자리가 투명한 부드러운 원
                    float alpha = 1f - (dist / maxRadius);
                    alpha = alpha * alpha; // 제곱으로 더 부드러운 페이드
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
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
    /// 에디터에서 사거리를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

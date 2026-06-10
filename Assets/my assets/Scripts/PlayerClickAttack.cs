using UnityEngine;

/// <summary>
/// 좌클릭으로 적을 타겟 지정하면 자동으로 연사하는 MOBA 스타일 공격 시스템입니다.
/// - 좌클릭 적 위: 타겟 지정, 자동 연사 시작
/// - 좌클릭 다른 적: 타겟 변경
/// - 좌클릭 빈 공간: 타겟 해제, 연사 중단
/// Player 오브젝트에 부착하여 사용합니다.
/// </summary>
public class PlayerClickAttack : MonoBehaviour
{
    [Header("발사 설정")]
    [Tooltip("공격 간격 (초)")]
    public float attackCooldown = 0.3f;

    [Tooltip("투사체 프리팹 (여기에 넣으세요! 비워두면 PlayerShooting의 것을 사용)")]
    public GameObject projectilePrefab;

    [Tooltip("투사체 속도")]
    public float projectileSpeed = 20f;

    [Tooltip("투사체 데미지")]
    public int attackDamage = 5;

    [Header("타겟팅 설정")]
    [Tooltip("타겟 감지 반경 (클릭 정밀도)")]
    public float clickDetectRadius = 0.5f;

    [Header("타겟 표시 설정")]
    [Tooltip("타겟 지정 시 적 밑에 표시할 이펙트 프리팹 (비워두면 기본 원 표시)")]
    public GameObject targetIndicatorPrefab;

    // 현재 타겟
    [HideInInspector] public GameObject currentTarget;
    private GameObject currentIndicator;
    private float lastAttackTime = -999f;
    private Camera mainCamera;

    [Header("=== 효과음 설정 (Sound Effects) ===")]
    [Tooltip("발사 시 재생할 효과음")]
    public AudioClip shootSound;
    [Tooltip("소리 재생용 오디오 소스 (비워두면 자동으로 찾거나 생성합니다)")]
    public AudioSource audioSource;
    [Range(0f, 1f)]
    [Tooltip("발사 효과음 볼륨")]
    public float shootVolume = 0.5f;

    public static PlayerClickAttack instance;

    private void Awake()
    {
        if (instance == null) instance = this;

        // AudioSource 자동 캐싱 및 기본 세팅
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 패리 경직 / 넉백 중이면 공격 불가
        if (PlayerMoving.instance != null &&
            (PlayerMoving.instance.isParryRecovery || PlayerMoving.instance.isKnockedBack))
            return;

        // 좌클릭 — 타겟 지정 / 변경 / 해제
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            GameObject clicked = FindEnemyAtPosition(mouseWorld);

            if (clicked != null)
            {
                if (currentTarget == clicked)
                {
                    SetTarget(null);
                }
                else
                {
                    SetTarget(clicked);
                }
            }
            else
            {
                SetTarget(null);
            }
        }

        // 타겟이 죽었으면 해제
        if (currentTarget != null && !currentTarget.activeInHierarchy)
        {
            SetTarget(null);
        }

        // 타겟의 스케일에 영향받지 않기 위해 부모 관계를 맺지 않고, 매 프레임 위치만 동기화합니다.
        if (currentIndicator != null && currentTarget != null)
        {
            currentIndicator.transform.position = currentTarget.transform.position;
        }

        // 타겟이 있으면 자동 연사
        if (currentTarget != null)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                Shoot(currentTarget.transform.position);
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;
        return worldPos;
    }

    /// <summary>
    /// 타겟을 지정/해제하고 인디케이터를 관리합니다.
    /// </summary>
    private void SetTarget(GameObject newTarget)
    {
        // 기존 인디케이터 제거
        if (currentIndicator != null)
            Destroy(currentIndicator);

        currentTarget = newTarget;

        // 새 타겟이 있으면 인디케이터 생성
        if (currentTarget != null)
        {
            if (targetIndicatorPrefab != null)
            {
                // Inspector에 넣은 프리팹 사용 (부모 관계를 맺지 않고 월드 상에 생성)
                currentIndicator = Instantiate(targetIndicatorPrefab, currentTarget.transform.position, Quaternion.identity);
            }
            else
            {
                // 기본: 빨간 원 표시 (부모 관계를 맺지 않고 월드 상에 생성)
                currentIndicator = new GameObject("TargetIndicator");
                currentIndicator.transform.position = currentTarget.transform.position;

                SpriteRenderer sr = currentIndicator.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(64);
                sr.color = new Color(1f, 0.2f, 0.2f, 0.4f);
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -1;
                currentIndicator.transform.localScale = Vector3.one * 2f;
            }
        }
    }

    /// <summary>
    /// 클릭 위치에서 Enemy 태그를 가진 오브젝트를 찾습니다.
    /// </summary>
    private GameObject FindEnemyAtPosition(Vector3 worldPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, clickDetectRadius);
        GameObject best = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit == null || !hit.gameObject.activeInHierarchy) continue;
            if (!hit.CompareTag("Enemy")) continue;

            float dist = Vector2.Distance(worldPos, hit.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = hit.gameObject;
            }
        }
        return best;
    }

    private void Shoot(Vector3 targetPos)
    {
        // 🔊 투사체 발사 효과음 재생
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound, shootVolume);
        }

        Vector2 direction = ((Vector2)targetPos - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        GameObject prefab = projectilePrefab;
        if (prefab == null && PlayerShooting.instance != null)
            prefab = PlayerShooting.instance.projectileObject;
        if (prefab == null)
        {
            Debug.LogWarning("⚠️ [PlayerClickAttack] 투사체 프리팹이 비어있습니다! Inspector에서 Projectile Prefab 칸에 총알 프리팹을 넣어주세요.");
            return;
        }

        GameObject bullet = Instantiate(prefab, transform.position, Quaternion.Euler(0, 0, angle));

        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.enemyBullet = false;
            proj.damage = attackDamage;
            proj.destroyedByCollision = true;
        }

        DirectMoving dm = bullet.GetComponent<DirectMoving>();
        if (dm != null)
        {
            dm.speed = projectileSpeed;
            dm.isHoming = false;
            dm.homingTargetEnemy = false;
            dm.aimAtPlayerOnStart = false;
            dm.visualAngleOffset = 0f;
        }

        bullet.tag = "Projectile";

        // 타겟 지점에 도달하면 소멸
        float distance = Vector2.Distance(transform.position, targetPos);
        Destroy(bullet, distance / projectileSpeed);
    }

    /// <summary>
    /// 절차적으로 원형 스프라이트를 생성합니다.
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
                    float alpha = 1f - (dist / maxRadius);
                    alpha = alpha * alpha;
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
}

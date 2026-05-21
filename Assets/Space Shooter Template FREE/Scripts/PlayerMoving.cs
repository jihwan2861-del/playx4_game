using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어의 이동 속도, 경계 구역 제한, 넉백, 에너지 시스템 및 패링(Parry) 기능을 총괄하는 스크립트입니다.
/// 기존의 대쉬와 저스트회피는 패링 메커니즘으로 전면 교체되었습니다.
/// </summary>

[System.Serializable]
public class Borders
{
    [Tooltip("viewport 경계로부터의 여백")]
    public float minXOffset = 1.5f, maxXOffset = 1.5f, minYOffset = 1.5f, maxYOffset = 1.5f;
    [HideInInspector] public float minX, maxX, minY, maxY;
}

public class PlayerMoving : MonoBehaviour {

    [Tooltip("플레이어 이동 가능 영역 경계")]
    public Borders borders;
    Camera mainCamera;
    bool controlIsActive = true; 

    [Header("Movement Settings")]
    public float baseSpeed = 9f;

    [Header("Parry Settings (패링 설정)")]
    [Tooltip("패링 판정 유효 시간 (초)")]
    public float parryDuration = 0.25f;
    [Tooltip("패링 실패(허공 사용) 시의 경직 리커버리 시간 (초)")]
    public float parryWhiffRecoveryDuration = 0.4f;
    [Tooltip("패링 재사용 대기 시간 (초)")]
    public float parryCooldownDuration = 0.6f;
    [Tooltip("패링 성공 시 주어지는 무적 시간 (초)")]
    public float parrySuccessInvincibility = 0.8f;
    [Tooltip("패링 1회당 소모 에너지")]
    public float parryEnergyCost = 20f;
    [Tooltip("패링 판정 범위 (파란 원 크기) - Inspector에서 자유롭게 조절하세요!")]
    public float parryRadius = 1.5f;
    [Tooltip("패링 성공 시 돌려받는 에너지량")]
    public float parryEnergyRefund = 40f;

    [HideInInspector] public bool isParryActive = false;
    [HideInInspector] public bool isParryCooldown = false;
    [HideInInspector] public bool isParryRecovery = false; // 패링 실패(whiff) 시 경직 상태

    [Header("Knockback Settings")]
    public float knockbackForce = 15f;
    public float knockbackDuration = 0.2f;
    [Tooltip("넉백 후 경직 시간")]
    public float stunDuration = 0.3f;
    [HideInInspector] public bool isKnockedBack = false;

    [Header("Energy System (에너지 시스템)")]
    [Tooltip("최대 에너지량")]
    public float maxEnergy = 100f;
    [Tooltip("현재 에너지량")]
    public float currentEnergy = 100f;
    [Tooltip("아이템 획득 시 회복하는 에너지")]
    public float itemEnergyRestore = 20f;

    [Header("에너지 자동 재생")]
    [Tooltip("초당 자동으로 회복되는 에너지")]
    public float baseEnergyRegen = 2f;
    [Tooltip("에너지 재생력 보너스")]
    public float energyRegenBonus = 0f;

    [Header("UI Objects")]
    public GameObject dashTextObj;
    public GameObject warningTextObj;

    public static PlayerMoving instance;

    private Rigidbody2D rb;
    private SpriteRenderer shieldSr;

    private void Awake()
    {
        if (instance == null)
            instance = this;
            
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        
        // 데이터 매니저 업그레이드 수치 적용
        if (PlayerDataManager.instance != null)
        {
            baseSpeed = 9f + PlayerDataManager.instance.speedLevel * 1.2f;
            maxEnergy = 100f + PlayerDataManager.instance.maxEnergyLevel * 10f;
            baseEnergyRegen = 2f + PlayerDataManager.instance.energyRegenLevel * 0.5f;
            // 기존 대쉬 소모 감소 업그레이드를 패링 소모 감소로 계승 적용!
            parryEnergyCost = Mathf.Max(10f, 20f - PlayerDataManager.instance.dashCostLevel * 1.5f);
            currentEnergy = maxEnergy;
            
            Debug.Log($"🔧 [스탯 강화 적용 완료] 속도: {baseSpeed}, 최대에너지: {maxEnergy}, 에너지회복/초: {baseEnergyRegen}, 패링에너지소모: {parryEnergyCost}");
        }
        
        ResizeBorders();
        
        if (dashTextObj != null)
        {
            Canvas canvas = dashTextObj.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(640, 920);
                    scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
                }
            }
            
            RectTransform rt = dashTextObj.GetComponent<RectTransform>();
            if (rt != null) rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
        }

        UpdateEnergyUI();
        if (warningTextObj != null) 
            warningTextObj.SetActive(false);
    }

    private void Update()
    {
        if (controlIsActive)
        {
            ResizeBorders();

            // 넉백 처리
            if (isKnockedBack)
            {
                Vector2 kbClamped = new Vector2(
                    Mathf.Clamp(rb.position.x, borders.minX, borders.maxX),
                    Mathf.Clamp(rb.position.y, borders.minY, borders.maxY)
                );
                if (rb.position != kbClamped) rb.position = kbClamped;
                return;
            }

            // 패링 실패로 인한 이동 경직(Whiff Freeze) 처리
            if (isParryRecovery)
            {
                if (rb != null) rb.velocity = Vector2.zero;
                return;
            }

            // 에너지 자동 재생
            if (currentEnergy < maxEnergy && !isParryActive)
            {
                float totalRegen = baseEnergyRegen + energyRegenBonus;
                currentEnergy = Mathf.Min(currentEnergy + totalRegen * Time.deltaTime, maxEnergy);
                UpdateEnergyUI();
            }

            // 패링 발동 입력 받기 (스페이스바 또는 마우스 우클릭)
            bool parryPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1);

            if (parryPressed && !isParryActive && !isParryCooldown && !isParryRecovery)
            {
                if (currentEnergy >= parryEnergyCost)
                {
                    PerformParry();
                }
                else if (warningTextObj != null && !warningTextObj.activeSelf)
                {
                    StartCoroutine(ShowWarningUI());
                }
            }

            // 패링 활성화 중 보호막 실시간 연출 (맥동 및 회전) + 범위 내 총알 자동 패링 감지
            if (isParryActive)
            {
                if (shieldSr != null)
                {
                    float pulse = 1.0f + Mathf.Sin(Time.time * 30f) * 0.1f;
                    float visualScale = parryRadius * pulse;
                    shieldSr.transform.localScale = new Vector3(visualScale, visualScale, 1f);
                    shieldSr.transform.Rotate(0, 0, 180f * Time.deltaTime);
                }

                // parryRadius 범위 안의 적 총알을 자동 감지하여 패링 성공 판정!
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, parryRadius);
                foreach (var col in hits)
                {
                    if (col == null) continue;
                    Projectile proj = col.GetComponent<Projectile>();
                    if (proj != null && proj.enemyBullet)
                    {
                        // 패링 성공 처리
                        if (TryTriggerParrySuccess(col.gameObject))
                        {
                            break; // 1회 패링 성공이면 충분
                        }
                    }
                }
            }

            // 이동 처리 (WASD / 방향키)
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 moveDirection = new Vector3(horizontal, vertical, 0).normalized;

            // --- [애니메이션 처리] ---
            Animator anim = GetComponent<Animator>();
            if (anim != null)
            {
                bool isMoving = moveDirection != Vector3.zero;
                anim.SetBool("isMoving", isMoving);

                if (isMoving)
                {
                    anim.SetFloat("InputX", horizontal);
                    anim.SetFloat("InputY", vertical);
                }
            }

            // Rigidbody2D를 이용한 물리 이동 처리
            if (rb != null)
            {
                rb.velocity = moveDirection * baseSpeed;

                Vector2 clampedPos = new Vector2(
                    Mathf.Clamp(rb.position.x, borders.minX, borders.maxX),
                    Mathf.Clamp(rb.position.y, borders.minY, borders.maxY)
                );
                
                if (rb.position != clampedPos)
                {
                    rb.position = clampedPos;
                }
            }
        }
    }

    void ResizeBorders() 
    {
        borders.minX = mainCamera.ViewportToWorldPoint(Vector2.zero).x + borders.minXOffset;
        borders.minY = mainCamera.ViewportToWorldPoint(Vector2.zero).y + borders.minYOffset;
        borders.maxX = mainCamera.ViewportToWorldPoint(Vector2.right).x - borders.maxXOffset;
        borders.maxY = mainCamera.ViewportToWorldPoint(Vector2.up).y - borders.maxYOffset;
    }

    /// <summary>
    /// 패링 보호막 발동 및 판정 코루틴을 실행합니다.
    /// </summary>
    void PerformParry()
    {
        currentEnergy -= parryEnergyCost;
        if (currentEnergy < 0) currentEnergy = 0;
        UpdateEnergyUI();
        
        Debug.Log("🛡️ 패링 보호막 활성화!");
        StartCoroutine(ParryRoutine());
    }

    IEnumerator ParryRoutine()
    {
        isParryActive = true;
        
        // 절차적 glowing 사이버 쉴드 활성화
        GetOrCreateShieldVisual();

        yield return new WaitForSeconds(parryDuration);

        // 유효 기간(0.25초) 내에 데미지 피격을 가로채 패링 성공하지 못했다면 -> 실패(Whiff) 처리!
        if (isParryActive)
        {
            isParryActive = false;
            if (shieldSr != null) shieldSr.gameObject.SetActive(false);

            // 허공에 방패를 날린 패널티로 0.4초간 완벽 경직
            StartCoroutine(ParryWhiffRecoveryRoutine());
        }
    }

    /// <summary>
    /// 패링 실패(Whiff) 시 플레이어를 일시 경직 및 취약 상태로 만듭니다.
    /// </summary>
    IEnumerator ParryWhiffRecoveryRoutine()
    {
        isParryRecovery = true;
        if (rb != null) rb.velocity = Vector2.zero;
        
        // 시각적으로 붉은/회색 톤으로 깜빡여 무방비 경직 상태를 연출합니다.
        SpriteRenderer playerSr = GetComponent<SpriteRenderer>();
        if (playerSr != null) playerSr.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);

        yield return new WaitForSeconds(parryWhiffRecoveryDuration);

        if (playerSr != null) playerSr.color = Color.white;
        isParryRecovery = false;

        // 재사용 대기 시간(쿨타임) 작동 시작
        StartCoroutine(ParryCooldownRoutine());
    }

    IEnumerator ParryCooldownRoutine()
    {
        isParryCooldown = true;
        yield return new WaitForSeconds(parryCooldownDuration);
        isParryCooldown = false;
    }

    /// <summary>
    /// 플레이어가 공격(탄알/적 충돌 등)을 당하는 찰나의 순간에 외부(Player.GetDamage)에서 호출되어 패링을 성사시킵니다.
    /// </summary>
    public bool TryTriggerParrySuccess(GameObject hazard)
    {
        if (!isParryActive) return false;

        isParryActive = false;
        if (shieldSr != null) shieldSr.gameObject.SetActive(false);

        // 1. 역경직 및 카메라 흔들림 (튜토리얼 씬이면 멀미 방지를 위해 대폭 완화하고, 일반 보스 스테이지면 묵직한 타격감을 위해 강하게 유지)
        bool isTutorial = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tutorial");
        float targetHitStop = isTutorial ? 0.04f : 0.15f;
        float shakeDuration = isTutorial ? 0.1f : 0.3f;
        float shakeMagnitude = isTutorial ? 0.12f : 0.6f;

        if (HitStop.instance != null)
        {
            HitStop.instance.Do(targetHitStop); 
        }
        if (Camera.main != null)
        {
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null) camFollow.Shake(shakeDuration, shakeMagnitude); 
        }

        // 2. 강렬한 팽창 쇼크웨이브 이펙트 실행 (Unscaled Time 사용으로 프레임 멈춤 중에도 이펙트 정상 재생)
        StartCoroutine(ParryShockwaveRoutine(transform.position));

        // 3. 에너지 충전 보상 - 패링 비용을 즉각 환급하고 메리트 제공
        AddEnergy(parryEnergyRefund);

        // 4. 안전 무적 판정 부여 (0.8초) 및 시각적 홀로그램 효과(하늘색) 적용
        if (Player.instance != null)
        {
            StartCoroutine(Player.instance.DashInvincibility(parrySuccessInvincibility));
            StartCoroutine(InvincibilityVisualRoutine(parrySuccessInvincibility));
        }

        // 5. 주변 탄막 및 지속성 위협(반경 5.0) 소멸
        ClearBulletsInRadius(Mathf.Max(parryRadius * 2f, 5.0f));

        // 6. 플레이어 위치에서 적 추적 유도 미사일 5발 강력한 방출!
        SpawnCounterProjectiles();

        Debug.Log("🛡️ [패링 성공!] 탄막 제거, 유도 미사일 5발 반격, 에너지 회복!");
        return true;
    }

    /// <summary>
    /// 지정된 반경 안의 적 투사체(Projectile), 레이저(LaserBeam), 격자 폭격 기기(GridStrikePattern)를 영구 소거합니다.
    /// </summary>
    private void ClearBulletsInRadius(float radius)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var col in colliders)
        {
            if (col == null) continue;
            
            // 1. 적 일반/유도 투사체 → 풀링 반환
            Projectile proj = col.GetComponent<Projectile>();
            if (proj != null && proj.enemyBullet)
            {
                proj.gameObject.SetActive(false);
                if (MissionPanel.instance != null)
                {
                    MissionPanel.instance.AddProgressByKeyword("파괴", 1);
                }
                continue;
            }

            // 2. 적 지속 레이저 빔 → 풀링 반환
            LaserBeam laser = col.GetComponent<LaserBeam>();
            if (laser != null)
            {
                laser.gameObject.SetActive(false);
                continue;
            }

            // 3. 적 레이저 경고 패턴 → 풀링 반환
            GridStrikePattern grid = col.GetComponent<GridStrikePattern>();
            if (grid != null)
            {
                grid.gameObject.SetActive(false);
                continue;
            }
        }
    }

    /// <summary>
    /// 정방향 앞 부채꼴 각도로 5발의 강력한 유도 미사일을 생성 및 발사합니다.
    /// </summary>
    private void SpawnCounterProjectiles()
    {
        if (PlayerShooting.instance == null || PlayerShooting.instance.projectileObject == null) return;

        GameObject baseProj = PlayerShooting.instance.projectileObject;
        float[] angles = { -40f, -20f, 0f, 20f, 40f };

        foreach (float angleOffset in angles)
        {
            Vector3 spawnPos = transform.position;
            Quaternion rot = Quaternion.Euler(0, 0, angleOffset);
            
            // 풀링 우선 사용, 없으면 Instantiate 폴백
            GameObject counterMissile = null;
            if (PoolingController.instance != null)
            {
                counterMissile = PoolingController.instance.GetPoolingObject(baseProj);
                if (counterMissile != null)
                {
                    counterMissile.transform.position = spawnPos;
                    counterMissile.transform.rotation = rot;
                    counterMissile.SetActive(true);
                }
            }
            if (counterMissile == null)
            {
                counterMissile = Instantiate(baseProj, spawnPos, rot);
            }

            if (counterMissile != null)
            {
                counterMissile.tag = "Projectile";

                // 1. 비주얼 변주: 파랗게 이글거리는 대형 탄으로 변경
                SpriteRenderer sr = counterMissile.GetComponent<SpriteRenderer>();
                if (sr == null) sr = counterMissile.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = new Color(0f, 0.8f, 1f, 1f); // Neon cyan glow
                    counterMissile.transform.localScale = Vector3.one * 1.8f;
                }

                // 2. 유도탄 설정 주입 (DirectMoving 컴포넌트)
                DirectMoving dm = counterMissile.GetComponent<DirectMoving>();
                if (dm == null) dm = counterMissile.AddComponent<DirectMoving>();
                
                dm.speed = 28f;
                dm.isHoming = true;
                dm.homingTargetEnemy = true;
                dm.homingRotSpeed = 380f;
                dm.homingDuration = 3f;
                
                // 3. 투사체 기본 성질 가공 (Projectile 컴포넌트)
                Projectile p = counterMissile.GetComponent<Projectile>();
                if (p == null) p = counterMissile.AddComponent<Projectile>();
                
                p.enemyBullet = false;
                p.damage = 10;
                p.destroyedByCollision = true;
            }
        }
    }

    /// <summary>
    /// 패링 성공 후 극적인 무적감 피드백을 전달하기 위해 플레이어를 하늘색 홀로그램 상태로 칠합니다.
    /// </summary>
    private IEnumerator InvincibilityVisualRoutine(float duration)
    {
        SpriteRenderer playerSr = GetComponent<SpriteRenderer>();
        if (playerSr != null)
        {
            playerSr.color = new Color(0f, 0.8f, 1f, 1f); // Cyber Cyan
            yield return new WaitForSeconds(duration);
            playerSr.color = Color.white;
        }
    }

    /// <summary>
    /// 무실동(TimeScale=0) 상황 하에서도 정상 기능하는 패링 성공 비주얼 쇼크웨이브를 렌더링합니다.
    /// </summary>
    private IEnumerator ParryShockwaveRoutine(Vector3 spawnPos)
    {
        GameObject shockwaveObj = new GameObject("ParryShockwave");
        shockwaveObj.transform.position = spawnPos;
        
        SpriteRenderer sr = shockwaveObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateShieldSprite();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 54;
        sr.color = new Color(0.2f, 0.9f, 1f, 1f);

        float elapsed = 0f;
        float duration = 0.25f;
        Vector3 startScale = new Vector3(1.2f, 1.2f, 1f);
        Vector3 targetScale = new Vector3(8.0f, 8.0f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // HitStop 타임 정지 시에도 정상 확장 연출 유도
            float t = elapsed / duration;
            shockwaveObj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            Color c = sr.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            sr.color = c;
            
            yield return null;
        }
        
        Destroy(shockwaveObj);
    }

    /// <summary>
    /// 외부 의존성을 배제하고 언제 어디서든 아름답고 화려하게 켜지는 보호막용 절차적 텍스처 스프라이트를 구워냅니다.
    /// </summary>
    private Sprite CreateShieldSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        float center = size / 2f;
        float maxRadius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= maxRadius)
                {
                    float thickness = 8f;
                    if (dist >= maxRadius - thickness)
                    {
                        float t = (dist - (maxRadius - thickness)) / thickness;
                        texture.SetPixel(x, y, new Color(0.2f, 0.8f, 1f, t * 0.9f));
                    }
                    else
                    {
                        float t = dist / (maxRadius - thickness);
                        texture.SetPixel(x, y, new Color(0.2f, 0.8f, 1f, t * 0.25f));
                    }
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

    private void GetOrCreateShieldVisual()
    {
        if (shieldSr == null)
        {
            GameObject shieldObj = new GameObject("ParryShield");
            shieldObj.transform.SetParent(transform);
            shieldObj.transform.localPosition = Vector3.zero;
            shieldObj.transform.localRotation = Quaternion.identity;
            
            shieldSr = shieldObj.AddComponent<SpriteRenderer>();
            shieldSr.sprite = CreateShieldSprite();
            shieldSr.sortingLayerName = "Default";
            shieldSr.sortingOrder = 53;
        }
        shieldSr.gameObject.SetActive(true);
        shieldSr.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        shieldSr.transform.localScale = new Vector3(parryRadius, parryRadius, 1f);
    }

    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        UpdateEnergyUI();
    }

    public void AddRegenBonus(float bonus)
    {
        energyRegenBonus += bonus;
        Debug.Log($"⚡ [에너지 재생력 증가!] 보너스: +{bonus} → 총 재생력: {baseEnergyRegen + energyRegenBonus}/s");
    }

    public void AddDashCharge()
    {
        AddEnergy(itemEnergyRestore);
    }

    void UpdateEnergyUI()
    {
        int current = Mathf.CeilToInt(currentEnergy);
        int max = Mathf.CeilToInt(maxEnergy);
        SetTextIfPossible(dashTextObj, "ENERGY: " + current + " / " + max);
    }

    IEnumerator ShowWarningUI()
    {
        if (warningTextObj != null)
        {
            SetTextIfPossible(warningTextObj, "ENERGY LOW!");
            warningTextObj.SetActive(true);
            yield return new WaitForSeconds(1f);
            warningTextObj.SetActive(false);
        }
    }

    void SetTextIfPossible(GameObject obj, string textValue)
    {
        if (obj == null) return;
        
        var legacyText = obj.GetComponent<UnityEngine.UI.Text>();
        if (legacyText != null) 
        {
            legacyText.text = textValue;
            return;
        }
        
        Component[] components = obj.GetComponents<Component>();
        foreach(var comp in components)
        {
            if (comp == null) continue;
            if (comp.GetType().Name.Contains("Text"))
            {
                var propInfo = comp.GetType().GetProperty("text");
                if (propInfo != null && propInfo.CanWrite)
                {
                    propInfo.SetValue(comp, textValue, null);
                    return;
                }
            }
        }
    }

    public void ApplyKnockback(Vector3 sourcePosition)
    {
        if (!isKnockedBack && gameObject.activeInHierarchy)
            StartCoroutine(KnockbackRoutine(sourcePosition));
    }

    IEnumerator KnockbackRoutine(Vector3 sourcePosition)
    {
        isKnockedBack = true;
        
        Vector3 dir = (transform.position - sourcePosition).normalized;
        if (dir == Vector3.zero) dir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;
        
        if (rb != null) rb.velocity = dir * knockbackForce;

        yield return new WaitForSeconds(knockbackDuration);
        
        if (rb != null) rb.velocity = Vector2.zero;

        if (stunDuration > 0)
        {
            yield return new WaitForSeconds(stunDuration);
        }

        isKnockedBack = false;
    }
}

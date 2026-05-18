using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script defines the borders of ‘Player’s’ movement. Depending on the chosen handling type, it moves the ‘Player’ together with the pointer.
/// </summary>

[System.Serializable]
public class Borders
{
    [Tooltip("offset from viewport borders for player's movement")]
    public float minXOffset = 1.5f, maxXOffset = 1.5f, minYOffset = 1.5f, maxYOffset = 1.5f;
    [HideInInspector] public float minX, maxX, minY, maxY;
}

public class PlayerMoving : MonoBehaviour {

    [Tooltip("offset from viewport borders for player's movement")]
    public Borders borders;
    Camera mainCamera;
    bool controlIsActive = true; 
    [Header("Movement Settings")]
    public float baseSpeed = 15f;

    [Header("Dash Settings")]
    public float normalDashSpeedMultiplier = 2f;
    public float justEvadeSpeedMultiplier = 4f;
    public float justEvadeInvincibilityDuration = 1.0f; 
    public float grazeRadius = 2.5f; // 저스트 회피 감지 범위

    [HideInInspector] public bool isDashing = false;

    [Header("Knockback Settings")]
    public float knockbackForce = 15f;
    public float knockbackDuration = 0.2f;
    [Tooltip("넉백 후 아무것도 못하고 멈춰있는 경직 시간")]
    public float stunDuration = 0.3f;
    [HideInInspector] public bool isKnockedBack = false;

    [Header("Dash Charges")]
    public int maxDashCharges = 5;
    public int currentDashCharges = 5;

    [Header("UI Objects (직접 연결해주세요)")]
    public GameObject dashTextObj;
    public GameObject warningTextObj;

    public static PlayerMoving instance; //unique instance of the script for easy access to the script

    private Rigidbody2D rb;

    private void Awake()
    {
        if (instance == null)
            instance = this;
            
        rb = GetComponent<Rigidbody2D>();
        // 혹시 Rigidbody2D가 없다면 자동으로 추가해줍니다.
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // 물리 충돌이 뚫리지 않도록 강제 설정
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 뚫림 방지
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        ResizeBorders();                //setting 'Player's' moving borders deending on Viewport's size
        
        // --- [UI 자동 복구 코드] ---
        // 텍스트가 안 보이는 현상(화면 밖 이탈, 캔버스 에러 등)을 
        // 게임 시작 시 코드가 강제로 고쳐버립니다!
        if (dashTextObj != null)
        {
            Canvas canvas = dashTextObj.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // 무조건 화면 맨 앞(오버레이)에 붙임
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    // 화면 비율에 따라 UI를 줄이고 늘리도록 강제 세팅 (해상도 밖으로 날아감 방지)
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(640, 920); // 캡처본 기준 사이즈
                    scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
                }
            }
            
            // 혹시 Z값이 안드로메다로 가있을 경우를 대비해 위치 원상복구
            RectTransform rt = dashTextObj.GetComponent<RectTransform>();
            if (rt != null) rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
        }
        // ---------------------------

        UpdateDashUI();
        if (warningTextObj != null) 
            warningTextObj.SetActive(false);
    }

    private void Update()
    {
        if (controlIsActive)
        {
            // 카메라가 이동할 수 있으므로 매 프레임 경계선을 업데이트합니다.
            ResizeBorders();

            if (isKnockedBack)
            {
                // 화면 밖으로 나가는 것 방지
                Vector2 kbClamped = new Vector2(
                    Mathf.Clamp(rb.position.x, borders.minX, borders.maxX),
                    Mathf.Clamp(rb.position.y, borders.minY, borders.maxY)
                );
                if (rb.position != kbClamped) rb.position = kbClamped;
                return; // 넉백 중에는 조작 불가
            }

            // 스페이스바 또는 우클릭 시 대쉬(무적) 발동
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
            {
                if (currentDashCharges > 0 && !isDashing)
                {
                    PerformDash();
                }
                else if (currentDashCharges <= 0 && warningTextObj != null && !warningTextObj.activeSelf)
                {
                    StartCoroutine(ShowWarningUI());
                }
            }

            // 키보드 WASD 또는 방향키 입력 받기
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 moveDirection = new Vector3(horizontal, vertical, 0).normalized;

            // --- [애니메이션 처리] ---
            Animator anim = GetComponent<Animator>();
            if (anim != null)
            {
                // 현재 이동 중인지 확인
                bool isMoving = moveDirection != Vector3.zero;
                anim.SetBool("isMoving", isMoving);

                if (isMoving)
                {
                    // 이동 중일 때만 방향 파라미터 업데이트 (가만히 있을 때 이전 방향을 바라보게 하기 위함)
                    anim.SetFloat("InputX", horizontal);
                    anim.SetFloat("InputY", vertical);
                }
            }
            // -------------------------

            // 기본 속도는 일정하게 (마우스 추적 시절의 30f는 너무 빠르므로)
            float currentSpeed = baseSpeed;
            if (isDashing)
            {
                currentSpeed = Player.instance.isInvincible ? baseSpeed * justEvadeSpeedMultiplier : baseSpeed * normalDashSpeedMultiplier;
            }

            // Rigidbody2D를 이용한 물리 이동 처리 (벽 충돌을 위해 velocity 사용)
            if (rb != null)
            {
                rb.velocity = moveDirection * currentSpeed;

                // 카메라 화면 밖으로 나가지 않게 하는 제한 (transform.position 대신 물리 위치 보정)
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

    //setting 'Player's' movement borders according to Viewport size and defined offset
    void ResizeBorders() 
    {
        borders.minX = mainCamera.ViewportToWorldPoint(Vector2.zero).x + borders.minXOffset;
        borders.minY = mainCamera.ViewportToWorldPoint(Vector2.zero).y + borders.minYOffset;
        borders.maxX = mainCamera.ViewportToWorldPoint(Vector2.right).x - borders.maxXOffset;
        borders.maxY = mainCamera.ViewportToWorldPoint(Vector2.up).y - borders.maxYOffset;
    }

    void PerformDash()
    {
        currentDashCharges--;
        UpdateDashUI();
        
        // 저스트 회피 판정 (내 주변 반경에 적이 있는지 검사)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, grazeRadius);
        bool justEvaded = false;
        foreach (var hit in hits)
        {
            string objName = hit.name.ToLower();
            // 적, 총알, 레이저, 보스, ball 등 (이름이나 태그, 컴포넌트로 싹 다 판별)
            if (hit.CompareTag("Enemy") || hit.CompareTag("Projectile") || 
                objName.Contains("laser") || objName.Contains("boss") || objName.Contains("ball") || objName.Contains("bullet") ||
                hit.GetComponentInParent<LaserBeam>() != null || hit.GetComponentInParent<Projectile>() != null || 
                hit.GetComponentInParent<Enemy>() != null || hit.GetComponentInParent<BossMovement>() != null)
            {
                justEvaded = true;
                break;
            }
        }

        if (justEvaded)
        {
            Debug.Log("✨ [저스트 회피 발동!] 완벽한 타이밍!");
            StartCoroutine(DashRoutine(justEvadeInvincibilityDuration));
            Player.instance.StartCoroutine(Player.instance.DashInvincibility(justEvadeInvincibilityDuration));
            
            // 물리적 통과를 위해 대쉬 시간 동안 충돌체를 Trigger로 변경
            StartCoroutine(PassThroughRoutine(justEvadeInvincibilityDuration));
            
            // 시각적 효과 (시간 느려짐)
            StartCoroutine(HitStopRoutine());
        }
        else
        {
            Debug.Log("💨 일반 대쉬 (짧은 무적 및 통과)");
            StartCoroutine(DashRoutine(0.4f)); 
            Player.instance.StartCoroutine(Player.instance.DashInvincibility(0.4f)); // 일반 대쉬도 0.4초 무적 부여!
            StartCoroutine(PassThroughRoutine(0.4f)); // 일반 대쉬도 몹 통과 가능!
        }
    }

    IEnumerator PassThroughRoutine(float duration)
    {
        // 플레이어 본체뿐만 아니라 자식(하위) 오브젝트에 있는 모든 충돌체를 다 찾습니다.
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        bool[] originalTriggers = new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                originalTriggers[i] = colliders[i].isTrigger;
                colliders[i].isTrigger = true; // 통과 모드 ON
            }
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].isTrigger = originalTriggers[i]; // 원래대로 복구
            }
        }
    }

    IEnumerator HitStopRoutine()
    {
        Time.timeScale = 0.3f;
        yield return new WaitForSecondsRealtime(0.2f);
        Time.timeScale = 1f;
    }



    public void AddDashCharge()
    {
        if (currentDashCharges < maxDashCharges)
        {
            currentDashCharges++;
            UpdateDashUI();
        }
    }

    void UpdateDashUI()
    {
        SetTextIfPossible(dashTextObj, "Dash: " + currentDashCharges + " / " + maxDashCharges);
    }

    IEnumerator ShowWarningUI()
    {
        if (warningTextObj != null)
        {
            SetTextIfPossible(warningTextObj, "NO ITEMS!");
            warningTextObj.SetActive(true);
            yield return new WaitForSeconds(1f);
            warningTextObj.SetActive(false);
        }
    }

    void SetTextIfPossible(GameObject obj, string textValue)
    {
        if (obj == null) return;
        
        // 1. Legacy Text 지원
        var legacyText = obj.GetComponent<UnityEngine.UI.Text>();
        if (legacyText != null) 
        {
            legacyText.text = textValue;
            return;
        }
        
        // 2. TextMeshPro 등 모든 Text 지원 (리플렉션 사용)
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

    IEnumerator DashRoutine(float duration)
    {
        isDashing = true;
        yield return new WaitForSeconds(duration);
        isDashing = false;
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

        // 추가 경직(Stun) 시간 대기
        if (stunDuration > 0)
        {
            yield return new WaitForSeconds(stunDuration);
        }

        isKnockedBack = false;
    }
    
    // 에디터에서 위험 감지 구역(Graze Radius)을 보여주기 위한 기즈모
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, grazeRadius);
    }
}

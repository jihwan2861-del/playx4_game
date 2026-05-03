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
    public float dashSpeedMultiplier = 3f;
    public float dashInvincibilityDuration = 3f; // 장막(데드존) 유지 시간으로 사용
    [Tooltip("야스오 장막(데드존) 프리팹을 여기에 넣으세요. 비워두면 임시로 파란 상자가 나옵니다.")]
    public GameObject deadzonePrefab;
    [HideInInspector] public bool isDashing = false;

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

            // 마우스 추적 시절의 30f는 키보드 조작에는 너무 빠를 수 있어 기본 baseSpeed(15f)를 사용
            float currentSpeed = isDashing ? baseSpeed * dashSpeedMultiplier : baseSpeed;

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
        StartCoroutine(DashRoutine());
        
        // 기존의 플레이어 무적 대신 장막(데드존) 소환
        SpawnDeadzone();
    }

    void SpawnDeadzone()
    {
        Debug.Log("🌀 [스킬 발동] 야스오 장막(Deadzone)이 생성되었습니다!");
        GameObject deadzoneObj;

        // 인스펙터에 프리팹을 등록해두었다면 그것을 생성
        if (deadzonePrefab != null)
        {
            deadzoneObj = Instantiate(deadzonePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            // 프리팹이 없다면 임시로 코드로 파란색 반투명 장막을 만들어 줍니다.
            deadzoneObj = new GameObject("WindWall_Deadzone");
            deadzoneObj.transform.position = transform.position;

            // 투사체를 막을 콜라이더 (유령 모드)
            CircleCollider2D col = deadzoneObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.5f; // 장막 크기

            // 장막 파괴 기능 스크립트 부착
            deadzoneObj.AddComponent<Deadzone>();

            // 눈에 보이도록 임시 파란색 사각형 렌더러 추가
            SpriteRenderer sr = deadzoneObj.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            sr.color = new Color(0.2f, 0.8f, 1f, 0.4f); // 반투명 시안색
            sr.sortingOrder = 999; // 배경에 가려지지 않게 최상단 배치
            deadzoneObj.transform.localScale = new Vector3(5f, 5f, 1f); // 크기 조절
        }

        // 대쉬 지속시간(dashInvincibilityDuration)이 끝나면 장막 자동 철거
        Destroy(deadzoneObj, dashInvincibilityDuration);
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

    IEnumerator DashRoutine()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashInvincibilityDuration);
        isDashing = false;
    }
}

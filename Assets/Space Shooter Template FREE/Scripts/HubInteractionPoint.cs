using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 허브 씬의 상호작용 포인트입니다.
/// 차고, 홀로그램, 작업실 등에 부착하면 플레이어가 가까이 가면 안내 UI가 뜨고,
/// 키를 누르면 해당 기능이 실행됩니다.
/// </summary>
public class HubInteractionPoint : MonoBehaviour
{
    public enum PointType
    {
        Garage,     // 차고 → 출격 (오토바이 미니게임)
        Hologram,   // 홀로그램 → 스토리/통신
        Workshop    // 작업실 → 업그레이드
    }

    [Header("상호작용 설정")]
    public PointType pointType;
    [Tooltip("true 이면 F키를 누르지 않고 밟기만 해도(영역 진입 즉시) 자동으로 상호작용 패널이 열립니다!")]
    public bool triggerOnEnter = false;
    public KeyCode interactKey = KeyCode.F;
    public float interactRadius = 2f;

    [Header("UI")]
    [Tooltip("'F키를 눌러 상호작용' 같은 안내 텍스트 오브젝트 (비어있으면 런타임에 자동으로 아름다운 F키 팝업이 생성됩니다)")]
    public GameObject promptUI;
    [Tooltip("상호작용 안내창에 적용할 사용자 정의 폰트입니다. 비어있으면 시스템 기본 폰트가 자동 적용됩니다.")]
    public Font customFont;

    private bool playerInRange = false;

    void Start()
    {
        // 유니티 인스펙터/씬에 저장된 값을 덮어쓰고, 무조건 F키를 누를 때만 입장하도록 강제합니다!
        triggerOnEnter = false;

        if (promptUI == null && !triggerOnEnter)
        {
            CreateDynamicPrompt();
        }

        if (promptUI != null) 
        {
            promptUI.SetActive(false);
        }
    }

    private void CreateDynamicPrompt()
    {
        // 1. 월드 스페이스 캔버스를 가진 새로운 오브젝트 생성
        GameObject canvasObj = new GameObject("DynamicPromptCanvas_" + pointType.ToString());
        canvasObj.transform.SetParent(this.transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, 1.4f, 0f); // 머리 위 적절한 높이

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // 2.5D 깊이 정렬 완벽 대응: 캐릭터 및 바닥 타일보다 위에 그려지도록 소팅 레이어 및 오더 자동 동기화
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            HubPlayerMovement hpm = FindObjectOfType<HubPlayerMovement>();
            if (hpm != null) playerObj = hpm.gameObject;
        }

        if (playerObj != null)
        {
            SpriteRenderer playerSr = playerObj.GetComponentInChildren<SpriteRenderer>();
            if (playerSr != null)
            {
                canvas.sortingLayerID = playerSr.sortingLayerID;
                canvas.sortingOrder = playerSr.sortingOrder + 100; // 플레이어보다 항상 100오더 위에 그리도록 설정
            }
            else
            {
                canvas.sortingOrder = 9999;
            }
        }
        else
        {
            canvas.sortingOrder = 9999;
        }
        
        RectTransform rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(4.5f, 1.0f); // 가로 세로 스케일
        rect.localScale = new Vector3(0.5f, 0.5f, 0.5f); // 적절하게 월드 크기에 맞춰 비율 축소

        // 2. 둥둥 떠다니는 애니메이션을 위한 Visual Container 생성
        GameObject containerObj = new GameObject("VisualContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRt = containerObj.AddComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;

        // 3. 배경 알약(Pill) 모양 판넬 (사이버네틱 다크 패널)
        GameObject bgObj = new GameObject("PromptBG");
        bgObj.transform.SetParent(containerObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.12f, 0.9f); // 깊고 프리미엄한 다크 네이비

        // 네온 골드 테두리 아웃라인 효과
        var outline = bgObj.AddComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(1f, 0.75f, 0f, 0.9f); // 사이버 황금색 아웃라인
            outline.effectDistance = new Vector2(2f, 2f);
        }

        // 4. 왼쪽 F 키캡 (Keyboard Keycap) UI
        GameObject keycapObj = new GameObject("Keycap");
        keycapObj.transform.SetParent(containerObj.transform, false);
        RectTransform keycapRt = keycapObj.AddComponent<RectTransform>();
        
        // 왼쪽에 깔끔하게 안착
        keycapRt.anchorMin = new Vector2(0.06f, 0.18f);
        keycapRt.anchorMax = new Vector2(0.24f, 0.82f);
        keycapRt.offsetMin = Vector2.zero;
        keycapRt.offsetMax = Vector2.zero;

        Image keycapImg = keycapObj.AddComponent<Image>();
        keycapImg.color = new Color(0.95f, 0.95f, 0.95f, 1f); // 맑고 밝은 실버 화이트 키캡 컬러

        // 키캡 입체감 부여용 어두운 회색 테두리
        var keycapOutline = keycapObj.AddComponent<Outline>();
        if (keycapOutline != null)
        {
            keycapOutline.effectColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            keycapOutline.effectDistance = new Vector2(1f, 1f);
        }

        // 키캡 그림자
        var keycapShadow = keycapObj.AddComponent<Shadow>();
        if (keycapShadow != null)
        {
            keycapShadow.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            keycapShadow.effectDistance = new Vector2(1f, -1f);
        }

        // 키캡 내부 "F" 글자
        GameObject keycapTextObj = new GameObject("KeycapText");
        keycapTextObj.transform.SetParent(keycapObj.transform, false);
        RectTransform keycapTextRt = keycapTextObj.AddComponent<RectTransform>();
        keycapTextRt.anchorMin = Vector2.zero;
        keycapTextRt.anchorMax = Vector2.one;
        keycapTextRt.offsetMin = Vector2.zero;
        keycapTextRt.offsetMax = Vector2.zero;

        Text keycapText = keycapTextObj.AddComponent<Text>();
        keycapText.text = "F";
        keycapText.font = customFont != null ? customFont : GetBuiltinFont();
        keycapText.fontSize = 14;
        keycapText.alignment = TextAnchor.MiddleCenter;
        keycapText.color = new Color(0f, 0f, 0f, 1f); // 검은색 볼드 글자

        // 5. 오른쪽 상호작용 지시 라벨 텍스트
        GameObject actionTextObj = new GameObject("ActionText");
        actionTextObj.transform.SetParent(containerObj.transform, false);
        RectTransform actionTextRt = actionTextObj.AddComponent<RectTransform>();
        
        // 키캡 바로 오른쪽에 배치
        actionTextRt.anchorMin = new Vector2(0.28f, 0f);
        actionTextRt.anchorMax = new Vector2(0.94f, 1f);
        actionTextRt.offsetMin = Vector2.zero;
        actionTextRt.offsetMax = Vector2.zero;

        Text actionText = actionTextObj.AddComponent<Text>();
        string labelText = "";
        switch (pointType)
        {
            case PointType.Garage: labelText = "차고 진입 <color=#FFD700>(출격)</color>"; break;
            case PointType.Hologram: labelText = "통신 연결 <color=#FFD700>(대화)</color>"; break;
            case PointType.Workshop: labelText = "기체 개조 <color=#FFD700>(강화)</color>"; break;
        }
        actionText.text = labelText;
        actionText.font = customFont != null ? customFont : GetBuiltinFont();
        actionText.fontSize = 14;
        actionText.alignment = TextAnchor.MiddleLeft;
        actionText.color = new Color(1f, 1f, 1f, 1f); // 깔끔한 화이트 본문

        // 텍스트 그림자
        var actionShadow = actionTextObj.AddComponent<Shadow>();
        if (actionShadow != null)
        {
            actionShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            actionShadow.effectDistance = new Vector2(1f, -1f);
        }

        promptUI = canvasObj;
    }

    void Update()
    {
        // 플레이어 존재 확인
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            // 태그가 없을 수도 있으니 컴포넌트로 찾기
            HubPlayerMovement hpm = FindObjectOfType<HubPlayerMovement>();
            if (hpm != null) player = hpm.gameObject;
        }
        if (player == null) return;

        // 거리 체크
        float dist = Vector2.Distance(transform.position, player.transform.position);
        bool inRange = dist <= interactRadius;

        // 범위 진입/이탈 시 안내 UI 토글
        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            
            if (triggerOnEnter && playerInRange)
            {
                // 밟으면 열리는 모드이고 방금 범위에 들어왔다면 즉시 패널 오픈! (단 1회만 호출됨)
                Interact();
            }
            else if (!triggerOnEnter && promptUI != null)
            {
                // 일반 키 입력 모드일 때만 안내 Prompt UI 활성화
                promptUI.SetActive(playerInRange);
            }
        }

        // 일반 모드: 범위 내에서 상호작용 키(F)를 직접 입력했을 때 실행
        if (!triggerOnEnter && playerInRange && Input.GetKeyDown(interactKey))
        {
            Interact();
        }

        // 상호작용 안내 UI 둥둥 떠다니는 애니메이션 처리
        if (playerInRange && promptUI != null)
        {
            Transform visual = promptUI.transform.Find("VisualContainer");
            if (visual != null)
            {
                float bobbing = Mathf.Sin(Time.time * 3.5f) * 0.08f;
                visual.localPosition = new Vector3(0f, bobbing, 0f);
            }
        }
    }

    void Interact()
    {
        HubUIManager hub = FindObjectOfType<HubUIManager>();
        if (hub == null)
        {
            Debug.LogWarning("⚠️ HubUIManager를 찾을 수 없습니다!");
            return;
        }

        switch (pointType)
        {
            case PointType.Garage:
                Debug.Log("🏍️ [차고] 출격 준비!");
                hub.OpenStageSelect();
                break;

            case PointType.Hologram:
                Debug.Log("📡 [홀로그램] 통신 연결...");
                hub.OpenHologram();
                break;

            case PointType.Workshop:
                Debug.Log("🔧 [작업실] 업그레이드 터미널 접속...");
                hub.OpenShop();
                break;
        }
    }

    // 에디터에서 상호작용 범위를 시각적으로 확인
    private void OnDrawGizmosSelected()
    {
        Color c = pointType == PointType.Garage ? Color.cyan :
                  pointType == PointType.Hologram ? Color.magenta :
                  Color.yellow;
        Gizmos.color = new Color(c.r, c.g, c.b, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    private Font GetBuiltinFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch {}

        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch {}
        }

        if (font == null)
        {
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            if (fonts != null && fonts.Length > 0)
            {
                font = fonts[0];
            }
        }

        return font;
    }
}

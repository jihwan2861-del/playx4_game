using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 씬 내의 NPC나 특정 상호작용 오브젝트에 붙여서 
/// F키를 누르면 초상화가 나오는 대화(Dialogue)를 시작할 수 있게 해주는 컴포넌트입니다.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    [Header("=== 상호작용 설정 ===")]
    [Tooltip("플레이어가 상호작용할 수 있는 반경")]
    public float interactRadius = 2.5f;
    
    [Tooltip("상호작용할 키 (기본: F)")]
    public KeyCode interactKey = KeyCode.F;

    [Header("=== 대화 리스트 설정 ===")]
    [Tooltip("순서대로 보여줄 대화 데이터들 (이름, 대사, 초상화 스프라이트 설정 가능)")]
    public List<HubDialogueLine> dialogueLines = new List<HubDialogueLine>()
    {
        new HubDialogueLine { speakerName = "홀로그램 AI", message = "반갑네, 파일럿. 시스템을 복구할 준비가 되었는가?", isLeft = true },
        new HubDialogueLine { speakerName = "주인공", message = "물론이지. 무엇을 먼저 처리해야 하나?", isLeft = false }
    };

    [Header("=== 대화 종료 후 이벤트 ===")]
    [Tooltip("대화가 모두 끝난 후 실행하고 싶은 Unity Event가 있다면 연결하세요.")]
    public UnityEngine.Events.UnityEvent onDialogueComplete;

    [Header("=== UI 안내 프롬프트 ===")]
    [Tooltip("F키 안내 UI (비어있으면 자동으로 머리 위에 예쁜 F키 안내 창을 생성합니다)")]
    public GameObject promptUI;
    
    [Tooltip("F키 안내 창에 적용할 폰트")]
    public Font customFont;

    private bool playerInRange = false;

    private void Start()
    {
        // 머리 위 F키 팝업 자동 생성
        if (promptUI == null)
        {
            CreateDialoguePrompt();
        }

        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    private void Update()
    {
        // 플레이어 검색
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            HubPlayerMovement hpm = FindObjectOfType<HubPlayerMovement>();
            if (hpm != null) player = hpm.gameObject;
        }

        if (player == null) return;

        // 플레이어와의 거리 측정
        float distance = Vector2.Distance(transform.position, player.transform.position);
        bool inRange = distance <= interactRadius;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (promptUI != null)
            {
                promptUI.SetActive(playerInRange);
            }
        }

        // 범위 내에서 F키 클릭 시 대화 시작
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            // 현재 다른 대화창이나 패널이 열려있지 않은지 검사
            if (HubDialogueManager.instance != null && !HubDialogueManager.instance.dialoguePanel.activeSelf)
            {
                if (promptUI != null) promptUI.SetActive(false); // 대화 중에는 F 안내 끔
                
                HubDialogueManager.instance.StartDialogue(dialogueLines, () =>
                {
                    // 대화가 다 끝났을 때의 행동
                    if (promptUI != null && playerInRange)
                    {
                        promptUI.SetActive(true); // 대화 끝나면 F 안내 다시 켬
                    }
                    onDialogueComplete.Invoke(); // 연결된 유니티 이벤트가 있으면 실행
                });
            }
        }

        // F 안내 팝업 둥둥 뜨는 애니메이션
        if (playerInRange && promptUI != null)
        {
            Transform container = promptUI.transform.Find("VisualContainer");
            if (container != null)
            {
                float yOffset = Mathf.Sin(Time.time * 3.5f) * 0.08f;
                container.localPosition = new Vector3(0f, yOffset, 0f);
            }
        }
    }

    private void CreateDialoguePrompt()
    {
        // 1. 월드 캔버스 생성
        GameObject canvasObj = new GameObject("DialoguePromptCanvas_" + gameObject.name);
        canvasObj.transform.SetParent(this.transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, 1.4f, 0f); // 머리 위 적절한 높이

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // 플레이어 스프라이트 소팅 오더 맞춰주기
        SpriteRenderer playerSr = null;
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) playerSr = playerObj.GetComponentInChildren<SpriteRenderer>();

        if (playerSr != null)
        {
            canvas.sortingLayerID = playerSr.sortingLayerID;
            canvas.sortingOrder = playerSr.sortingOrder + 100;
        }
        else
        {
            canvas.sortingOrder = 9999;
        }

        RectTransform rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(4.5f, 1.0f);
        rect.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // 2. 비주얼 컨테이너
        GameObject containerObj = new GameObject("VisualContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRt = containerObj.AddComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;

        // 3. 배경 이미지
        GameObject bgObj = new GameObject("PromptBG");
        bgObj.transform.SetParent(containerObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.04f, 0.08f, 0.9f); // 깊고 어두운 공상과학 네이비

        var outline = bgObj.AddComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(0f, 0.9f, 1f, 0.8f); // 네온 민트 아웃라인
            outline.effectDistance = new Vector2(2f, 2f);
        }

        // 4. 키캡 UI (F키)
        GameObject keycapObj = new GameObject("Keycap");
        keycapObj.transform.SetParent(containerObj.transform, false);
        RectTransform keycapRt = keycapObj.AddComponent<RectTransform>();
        keycapRt.anchorMin = new Vector2(0.06f, 0.18f);
        keycapRt.anchorMax = new Vector2(0.24f, 0.82f);
        keycapRt.offsetMin = Vector2.zero;
        keycapRt.offsetMax = Vector2.zero;

        Image keycapImg = keycapObj.AddComponent<Image>();
        keycapImg.color = Color.white;

        var keycapOutline = keycapObj.AddComponent<Outline>();
        if (keycapOutline != null)
        {
            keycapOutline.effectColor = Color.gray;
            keycapOutline.effectDistance = new Vector2(1f, 1f);
        }

        GameObject keycapTextObj = new GameObject("KeycapText");
        keycapTextObj.transform.SetParent(keycapObj.transform, false);
        RectTransform keyTextRt = keycapTextObj.AddComponent<RectTransform>();
        keyTextRt.anchorMin = Vector2.zero;
        keyTextRt.anchorMax = Vector2.one;
        keyTextRt.offsetMin = Vector2.zero;
        keyTextRt.offsetMax = Vector2.zero;

        Text keyText = keycapTextObj.AddComponent<Text>();
        keyText.text = "F";
        keyText.font = customFont != null ? customFont : GetBuiltinFont();
        keyText.fontSize = 14;
        keyText.alignment = TextAnchor.MiddleCenter;
        keyText.color = Color.black;

        // 5. 텍스트 라벨 (이야기 나누기)
        GameObject actionTextObj = new GameObject("ActionText");
        actionTextObj.transform.SetParent(containerObj.transform, false);
        RectTransform actionTextRt = actionTextObj.AddComponent<RectTransform>();
        actionTextRt.anchorMin = new Vector2(0.28f, 0f);
        actionTextRt.anchorMax = new Vector2(0.94f, 1f);
        actionTextRt.offsetMin = Vector2.zero;
        actionTextRt.offsetMax = Vector2.zero;

        Text actionText = actionTextObj.AddComponent<Text>();
        actionText.text = "통신 연결 <color=#00FFFF>(대화)</color>";
        actionText.font = customFont != null ? customFont : GetBuiltinFont();
        actionText.fontSize = 14;
        actionText.alignment = TextAnchor.MiddleLeft;
        actionText.color = Color.white;

        var actionShadow = actionTextObj.AddComponent<Shadow>();
        if (actionShadow != null)
        {
            actionShadow.effectColor = Color.black;
            actionShadow.effectDistance = new Vector2(1f, -1f);
        }

        promptUI = canvasObj;
    }

    private Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.9f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

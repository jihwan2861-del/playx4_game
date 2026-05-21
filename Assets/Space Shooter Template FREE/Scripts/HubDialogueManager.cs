using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class HubDialogueLine
{
    [Tooltip("말하는 캐릭터의 이름")]
    public string speakerName;
    
    [TextArea(3, 5)]
    [Tooltip("대화 대사 내용")]
    public string message;
    
    [Tooltip("말하는 캐릭터의 초상화 이미지 (비어있으면 대화창의 초상화가 숨겨집니다)")]
    public Sprite portrait;

    [Tooltip("초상화와 이름표가 화면 왼쪽에 나올지 오른쪽에 나올지 설정 (기본값: 왼쪽)")]
    public bool isLeft = true;
}

/// <summary>
/// 허브 씬에서 캐릭터 초상화 및 좌우 독립 이름표와 함께 고퀄리티 대화를 처리해주는 매니저 스크립트입니다.
/// 한 글자씩 부드럽게 출력되는 타이핑 연출(Typewriter Effect)과 클릭 시 스킵 기능을 제공합니다.
/// </summary>
public class HubDialogueManager : MonoBehaviour
{
    public static HubDialogueManager instance;

    [Header("=== UI 컴포넌트 연결 ===")]
    [Tooltip("대화창 패널 전체 오브젝트")]
    public GameObject dialoguePanel;
    
    [Header("=== 왼쪽 캐릭터 영역 ===")]
    [Tooltip("화면 왼쪽에 위치할 초상화 Image")]
    public Image leftPortraitImage;
    [Tooltip("왼쪽 이름표 패널 (NameBG)")]
    public GameObject leftNamePanel;
    [Tooltip("왼쪽 이름 Text (Legacy)")]
    public Text leftNameText;
    [Tooltip("왼쪽 이름 Text (TextMeshPro)")]
    public TMPro.TMP_Text leftNameTextTMP;

    [Header("=== 오른쪽 캐릭터 영역 ===")]
    [Tooltip("화면 오른쪽에 위치할 초상화 Image (선택 사항)")]
    public Image rightPortraitImage;
    [Tooltip("오른쪽 이름표 패널 (NameBG)")]
    public GameObject rightNamePanel;
    [Tooltip("오른쪽 이름 Text (Legacy)")]
    public Text rightNameText;
    [Tooltip("오른쪽 이름 Text (TextMeshPro)")]
    public TMPro.TMP_Text rightNameTextTMP;

    [Header("=== 대화 텍스트 및 효과 ===")]
    [Tooltip("대화 내용 Text (Legacy)")]
    public Text messageText;
    [Tooltip("대화 내용 Text (TextMeshPro)")]
    public TMPro.TMP_Text messageTextTMP;
    [Tooltip("대화 진행 안내 아이콘 (다음 버튼 등, 반짝거리는 연출용)")]
    public GameObject nextIndicator;

    [Header("=== 연출 설정 ===")]
    [Tooltip("한 글자당 타이핑 속도 (초)")]
    public float typingSpeed = 0.03f;

    // 대화 상태 관련 변수들
    private List<HubDialogueLine> currentLines = new List<HubDialogueLine>();
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private string fullMessage = "";
    private Coroutine typingCoroutine;
    private System.Action onCompleteCallback;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 새로운 대화를 시작합니다.
    /// </summary>
    public void StartDialogue(List<HubDialogueLine> lines, System.Action onComplete = null)
    {
        if (lines == null || lines.Count == 0) return;

        currentLines = new List<HubDialogueLine>(lines);
        currentLineIndex = 0;
        onCompleteCallback = onComplete;

        SetPlayerMovement(false);

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        DisplayCurrentLine();
    }

    private void Update()
    {
        if (dialoguePanel == null || !dialoguePanel.activeSelf) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F))
        {
            if (isTyping)
            {
                StopTypingAndShowFullText();
            }
            else
            {
                NextLine();
            }
        }
    }

    private void DisplayCurrentLine()
    {
        if (currentLineIndex >= currentLines.Count)
        {
            EndDialogue();
            return;
        }

        HubDialogueLine line = currentLines[currentLineIndex];

        // 1. 이름표 및 초상화 레이아웃 설정
        SetupSpeakerLayout(line);

        // 2. 텍스트 타이핑 효과 시작
        fullMessage = line.message;
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(fullMessage));
    }

    private void SetupSpeakerLayout(HubDialogueLine line)
    {
        if (line.isLeft)
        {
            // --- 왼쪽이 말하는 경우 ---
            // 1. 왼쪽 이름표 켜기 및 이름 쓰기
            if (leftNamePanel != null) leftNamePanel.SetActive(true);
            if (leftNameText != null) leftNameText.text = line.speakerName;
            if (leftNameTextTMP != null) leftNameTextTMP.text = line.speakerName;
            
            // 2. 오른쪽 이름표 끄기
            if (rightNamePanel != null) rightNamePanel.SetActive(false);

            // 3. 왼쪽 초상화 세팅 (밝게)
            if (leftPortraitImage != null)
            {
                if (line.portrait != null)
                {
                    leftPortraitImage.gameObject.SetActive(true);
                    leftPortraitImage.sprite = line.portrait;
                    leftPortraitImage.color = Color.white;
                }
                else
                {
                    leftPortraitImage.gameObject.SetActive(false);
                }
            }

            // 4. 오른쪽 초상화 어둡게 피킹
            if (rightPortraitImage != null)
            {
                if (rightPortraitImage.gameObject.activeSelf)
                {
                    rightPortraitImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
        }
        else
        {
            // --- 오른쪽이 말하는 경우 ---
            // 1. 오른쪽 이름표 켜기 및 이름 쓰기
            if (rightNamePanel != null) rightNamePanel.SetActive(true);
            if (rightNameText != null) rightNameText.text = line.speakerName;
            if (rightNameTextTMP != null) rightNameTextTMP.text = line.speakerName;
            
            // 2. 왼쪽 이름표 끄기
            if (leftNamePanel != null) leftNamePanel.SetActive(false);

            // 3. 오른쪽 초상화 세팅 (밝게)
            if (rightPortraitImage != null)
            {
                if (line.portrait != null)
                {
                    rightPortraitImage.gameObject.SetActive(true);
                    rightPortraitImage.sprite = line.portrait;
                    rightPortraitImage.color = Color.white;
                }
                else
                {
                    rightPortraitImage.gameObject.SetActive(false);
                }
            }

            // 4. 왼쪽 초상화 어둡게 피킹
            if (leftPortraitImage != null)
            {
                if (leftPortraitImage.gameObject.activeSelf)
                {
                    leftPortraitImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
        }
    }

    private IEnumerator TypeText(string textToType)
    {
        isTyping = true;
        if (messageText != null) messageText.text = "";
        if (messageTextTMP != null) messageTextTMP.text = "";
        if (nextIndicator != null)
        {
            nextIndicator.SetActive(false);
        }

        string currentText = "";
        foreach (char c in textToType.ToCharArray())
        {
            currentText += c;
            if (messageText != null) messageText.text = currentText;
            if (messageTextTMP != null) messageTextTMP.text = currentText;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        if (nextIndicator != null)
        {
            nextIndicator.SetActive(true);
        }
    }

    private void StopTypingAndShowFullText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        if (messageText != null) messageText.text = fullMessage;
        if (messageTextTMP != null) messageTextTMP.text = fullMessage;
        
        isTyping = false;
        if (nextIndicator != null)
        {
            nextIndicator.SetActive(true);
        }
    }

    private void NextLine()
    {
        currentLineIndex++;
        DisplayCurrentLine();
    }

    public void EndDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        SetPlayerMovement(true);

        if (onCompleteCallback != null)
        {
            onCompleteCallback.Invoke();
            onCompleteCallback = null;
        }
    }

    private void SetPlayerMovement(bool canMove)
    {
        HubPlayerMovement player = FindObjectOfType<HubPlayerMovement>();
        if (player != null)
        {
            player.canMove = canMove;
            if (!canMove)
            {
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                }
                
                Animator anim = player.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.SetFloat("Speed", 0f);
                }
            }
        }
    }
}

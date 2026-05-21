using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    [Tooltip("말하는 캐릭터의 이름")]
    public string speakerName;
    
    [TextArea(2, 5)]
    [Tooltip("대화 대사 내용")]
    public string message;
    
    [Tooltip("말하는 캐릭터의 초상화 이미지 (비어있으면 대화창의 초상화가 숨겨집니다)")]
    public Sprite portrait;

    [Tooltip("초상화와 이름표가 화면 왼쪽에 나올지 오른쪽에 나올지 설정 (기본값: 왼쪽)")]
    public bool isLeft = true;
}

/// <summary>
/// 스테이지 시작 시 대화(인트로 및 튜토리얼 가이드)를 좌우 초상화 및 이름표와 함께 고퀄리티로 보여주는 스크립트입니다.
/// </summary>
public class IntroDialogueManager : MonoBehaviour
{
    [Header("=== UI 패널 컴포넌트 ===")]
    [Tooltip("대화창 전체를 감싸는 UI 패널")]
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
    [Tooltip("화면 오른쪽에 위치할 초상화 Image")]
    public Image rightPortraitImage;
    [Tooltip("오른쪽 이름표 패널 (NameBG)")]
    public GameObject rightNamePanel;
    [Tooltip("오른쪽 이름 Text (Legacy)")]
    public Text rightNameText;
    [Tooltip("오른쪽 이름 Text (TextMeshPro)")]
    public TMPro.TMP_Text rightNameTextTMP;

    [Header("=== 대화 텍스트 및 효과 ===")]
    [Tooltip("대화 내용이 표시될 Text (Legacy)")]
    public Text messageText;         
    [Tooltip("대화 내용이 표시될 Text (TextMeshPro)")]
    public TMPro.TMP_Text messageTextTMP;
    [Tooltip("대화 진행 안내 아이콘 (다음 버튼 등)")]
    public GameObject nextIndicator;

    [Header("=== 연출 설정 ===")]
    [Tooltip("한 글자당 타이핑 속도 (초)")]
    public float typingSpeed = 0.03f;

    [Header("=== 대화 내용 설정 ===")]
    public DialogueLine[] lines = new DialogueLine[] {
        new DialogueLine { 
            speakerName = "사령관", 
            message = "오, 드디어 깨어났군 파일럿! 이곳은 무기 튜토리얼 가이드 구역이다.",
            isLeft = true 
        },
        new DialogueLine { 
            speakerName = "주인공", 
            message = "비무장 기체로도 싸울 수 있도록 성능 점검을 끝마쳤습니다.",
            isLeft = false 
        },
        new DialogueLine { 
            speakerName = "시스템", 
            message = "[안내] WASD 또는 방향키로 이동하고, Space를 눌러 대시(회피)를 사용할 수 있습니다.",
            isLeft = true 
        }
    };

    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private string fullMessage = "";
    private Coroutine typingCoroutine;
    private System.Action onCompleteCallback;

    void Start()
    {
        if (lines != null && lines.Length > 0 && dialoguePanel != null)
        {
            StartDialogue();
        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 외부(예: TutorialController)에서 동적으로 새로운 대화 데이터를 들려주고
    /// 대화가 끝나면 콜백을 받도록 해주는 함수입니다.
    /// </summary>
    public void StartDialogueWithCallback(DialogueLine[] newLines, System.Action onComplete)
    {
        lines = newLines;
        onCompleteCallback = onComplete;
        StartDialogue();
    }

    void StartDialogue()
    {
        isDialogueActive = true;
        currentLineIndex = 0;
        
        dialoguePanel.SetActive(true);
        SetPlayerControl(false);
        Time.timeScale = 0f;
        
        ShowLine();
    }

    void Update()
    {
        if (!isDialogueActive) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            else
            {
                currentLineIndex++;
                if (currentLineIndex < lines.Length)
                {
                    ShowLine();
                }
                else
                {
                    EndDialogue();
                }
            }
        }
    }

    void ShowLine()
    {
        DialogueLine line = lines[currentLineIndex];

        // 1. 이름표 및 초상화 설정
        SetupSpeakerLayout(line);

        // 2. 텍스트 타이핑 효과 시작
        fullMessage = line.message;
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        if (messageText != null) messageText.text = "";
        if (messageTextTMP != null) messageTextTMP.text = "";
        typingCoroutine = StartCoroutine(TypeText(line.message));
    }

    private void SetupSpeakerLayout(DialogueLine line)
    {
        if (line.isLeft)
        {
            // --- 왼쪽이 말하는 경우 ---
            // 1. 왼쪽 이름표 켜기 및 텍스트 갱신
            if (leftNamePanel != null) leftNamePanel.SetActive(true);
            if (leftNameText != null) leftNameText.text = line.speakerName;
            if (leftNameTextTMP != null) leftNameTextTMP.text = line.speakerName;
            
            // 2. 오른쪽 이름표 끄기
            if (rightNamePanel != null) rightNamePanel.SetActive(false);

            // 3. 왼쪽 초상화 세팅 (말하는 쪽은 밝게)
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

            // 4. 오른쪽 초상화 어둡게 피킹 (상대방이 말하므로)
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
            // 1. 오른쪽 이름표 켜기 및 텍스트 갱신
            if (rightNamePanel != null) rightNamePanel.SetActive(true);
            if (rightNameText != null) rightNameText.text = line.speakerName;
            if (rightNameTextTMP != null) rightNameTextTMP.text = line.speakerName;
            
            // 2. 왼쪽 이름표 끄기
            if (leftNamePanel != null) leftNamePanel.SetActive(false);

            // 3. 오른쪽 초상화 세팅 (말하는 쪽은 밝게)
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

            // 4. 왼쪽 초상화 어둡게 피킹 (상대방이 말하므로)
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

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        if (messageText != null) messageText.text = lines[currentLineIndex].message;
        if (messageTextTMP != null) messageTextTMP.text = lines[currentLineIndex].message;
        
        isTyping = false;
        if (nextIndicator != null)
        {
            nextIndicator.SetActive(true);
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        
        SetPlayerControl(true);
        Time.timeScale = 1f;

        if (onCompleteCallback != null)
        {
            System.Action callback = onCompleteCallback;
            onCompleteCallback = null; // 순환 방지를 위해 클리어 먼저 진행
            callback.Invoke();
        }
    }

    private void SetPlayerControl(bool canMove)
    {
        PlayerMoving player = FindObjectOfType<PlayerMoving>();
        if (player != null)
        {
            player.enabled = canMove;
            if (!canMove)
            {
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
            }
        }
    }
}

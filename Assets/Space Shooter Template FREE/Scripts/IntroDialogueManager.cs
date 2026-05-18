using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(2, 5)]
    public string message;
}

/// <summary>
/// 스테이지 시작 시 대화(인트로)를 보여주는 스크립트입니다.
/// 대화 중에는 게임(Time.timeScale)이 멈추며, 대화가 끝나면 게임이 시작됩니다.
/// </summary>
public class IntroDialogueManager : MonoBehaviour
{
    [Header("UI 연결 (Inspector에서 할당)")]
    [Tooltip("대화창 전체를 감싸는 UI 패널")]
    public GameObject dialoguePanel; 
    [Tooltip("말하는 사람의 이름이 표시될 Text")]
    public Text nameText;            
    [Tooltip("대화 내용이 표시될 Text")]
    public Text messageText;         

    [Header("대화 내용 설정")]
    public DialogueLine[] lines = new DialogueLine[] {
        new DialogueLine { speakerName = "사령관", message = "저 공격성 높은 사이보그들을 모두 처리해 주면..." },
        new DialogueLine { speakerName = "사령관", message = "네 녀석이 그놈들의 부품(잔해)을 챙겨가는 건 특별히 눈감아 주지." },
        new DialogueLine { speakerName = "주인공", message = "알았어. 무기가 없는 비무장 기체로도 그 정도는 충분해." },
        new DialogueLine { speakerName = "시스템", message = "[위협 감지] 무장 사이보그 접근 중. 회피 기동을 준비하십시오." }
    };

    private int currentLineIndex = 0;
    private bool isDialogueActive = false;

    void Start()
    {
        // 대화 내용이 설정되어 있고, UI 패널이 연결되어 있다면 대화 시작
        if (lines != null && lines.Length > 0 && dialoguePanel != null)
        {
            StartDialogue();
        }
        else
        {
            // 설정이 제대로 안 되어있으면 대화창을 숨기고 그냥 게임 시작
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }
    }

    void StartDialogue()
    {
        isDialogueActive = true;
        currentLineIndex = 0;
        
        dialoguePanel.SetActive(true);
        
        // 대화 중에는 게임 시간을 멈춤 (적 스폰, 총알 이동 정지)
        Time.timeScale = 0f;
        
        ShowLine();
    }

    void Update()
    {
        if (!isDialogueActive) return;

        // 마우스 좌클릭, 스페이스바, 엔터키 중 하나를 누르면 다음 대화로 넘어감
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
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

    void ShowLine()
    {
        if (nameText != null) nameText.text = lines[currentLineIndex].speakerName;
        if (messageText != null) messageText.text = lines[currentLineIndex].message;
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        
        // 대화가 끝나면 게임 시간 다시 정상화 -> LevelController 등의 대기 시간(Delay)이 이때부터 흐르기 시작함
        Time.timeScale = 1f;
    }
}

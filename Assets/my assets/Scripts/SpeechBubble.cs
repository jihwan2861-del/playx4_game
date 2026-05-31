using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // TextMeshPro 네임스페이스 추가

/// <summary>
/// 플레이어/캐릭터의 자식 오브젝트로 미리 배치되어 활성화/비활성화되는 정석적인 말풍선 컴포넌트입니다.
/// 사용자가 유니티 에디터 상에서 직접 세팅한 Vertical Layout Group 및 Content Size Fitter 레이아웃 시스템과
/// 100% 깔끔하게 호환되도록 스크립트 상의 수동 크기 조절 로직이 모두 제거된 정석 구조입니다.
/// </summary>
public class SpeechBubble : MonoBehaviour
{
    public static SpeechBubble playerBubbleInstance; // 플레이어 말풍선에 즉시 접근하기 위한 편의성 스태틱 싱글톤

    [Header("=== UI 컴포넌트 연결 ===")]
    [Tooltip("말풍선의 TextMeshPro - Text 컴포넌트")]
    public TextMeshProUGUI textComponent;
    [Tooltip("말풍선 전체 배경 오브젝트")]
    public RectTransform bubbleBackground;

    [Header("=== 타이핑 속도 ===")]
    [Tooltip("글자당 출력 시간 (초)")]
    public float typeSpeed = 0.035f;

    private Coroutine typingCoroutine;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        // 만약 이 오브젝트가 Player의 자식(루트가 Player 태그를 가졌거나 부모 중 PlayerMoving 컴포넌트가 있는 경우)이라면 전역 인스턴스로 등록
        if (transform.root.CompareTag("Player") || GetComponentInParent<PlayerMoving>() != null)
        {
            playerBubbleInstance = this;
            Debug.Log("💬 [SpeechBubble] 플레이어 자식 TMP 말풍선 인스턴스가 성공적으로 전역 등록되었습니다.");
        }

        if (bubbleBackground != null)
        {
            originalScale = bubbleBackground.localScale;
        }

        // 시작할 때는 보이지 않게 꺼둠
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 말풍선을 활성화하고 텍스트를 출력합니다.
    /// </summary>
    /// <param name="text">출력할 대사</param>
    /// <param name="onComplete">대사 출력 완료(또는 닫힐 때) 실행할 콜백</param>
    public void Show(string text, System.Action onComplete = null)
    {
        gameObject.SetActive(true);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeTextRoutine(text, onComplete));
    }

    /// <summary>
    /// 지정된 지연 시간 후에 말풍선을 닫습니다.
    /// </summary>
    public void Close(float delay = 0f)
    {
        StartCoroutine(CloseRoutine(delay));
    }

    private IEnumerator TypeTextRoutine(string fullText, System.Action onComplete)
    {
        if (textComponent != null)
        {
            textComponent.text = "";
        }

        // 등장 이펙트(스케일 애니메이션)가 제거되어 즉시 원래 스케일 상태를 유지합니다.
        if (bubbleBackground != null)
        {
            bubbleBackground.localScale = originalScale;
        }

        if (textComponent != null)
        {
            string currentText = "";
            char[] characters = fullText.ToCharArray();

            for (int i = 0; i < characters.Length; i++)
            {
                currentText += characters[i];
                textComponent.text = currentText;
                yield return new WaitForSecondsRealtime(typeSpeed);
            }
        }
        else
        {
            yield return null;
        }

        // 타이핑 완료 후 콜백 호출
        onComplete?.Invoke();
    }

    private IEnumerator CloseRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (bubbleBackground != null)
        {
            bubbleBackground.localScale = originalScale;
        }

        gameObject.SetActive(false);
    }
}

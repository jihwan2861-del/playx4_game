using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
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

    [Header("=== 타이핑 완급 조절 ===")]
    [Tooltip("마침표 (.)가 찍혔을 때 타이핑 대기 배율 (기본속도의 N배 느려짐)")]
    public float dotDelayMultiplier = 4.5f;

    [Tooltip("쉼표 (,), 느낌표 (!), 물음표 (?)가 찍혔을 때 타이핑 대기 배율")]
    public float punctuationDelayMultiplier = 3.0f;

    [Header("=== 자동 닫기 설정 ===")]
    [Tooltip("ShowDialogueInspector로 대사를 띄웠을 때, 타이핑이 완료되고 몇 초 후에 자동으로 닫을지 지정 (0 이하이면 닫히지 않고 계속 유지됨)")]
    public float defaultAutoCloseDelay = 2.0f;

    [Header("=== 타이핑 효과음 (Typing SFX) ===")]
    [Tooltip("글자가 타이핑될 때 재생할 효과음 클립")]
    public AudioClip typingSound;
    [Tooltip("효과음 재생용 오디오 소스 (비워두면 컴포넌트에서 자동으로 찾거나 생성합니다)")]
    public AudioSource audioSource;
    [Range(0f, 1f)]
    [Tooltip("타이핑 효과음 볼륨")]
    public float typingVolume = 0.5f;
    [Range(1, 5)]
    [Tooltip("소리가 너무 촘촘하게 나는 것을 방지하기 위해 몇 글자마다 소리를 낼지 지정 (기본값: 2 = 두 글자마다)")]
    public int soundInterval = 2;

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

        // AudioSource 자동 캐싱 및 셋업
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

        List<int> shakeIndices;
        string processedText = ProcessShakeTags(text, out shakeIndices);

        // 태그(<...>)를 제거한 순수 텍스트를 기준으로 괄호 여부를 똑똑하게 검사합니다.
        string cleanText = System.Text.RegularExpressions.Regex.Replace(processedText, "<[^>]*>", "");
        bool isEffectSound = cleanText.StartsWith("(") && cleanText.EndsWith(")");

        if (bubbleBackground != null)
        {
            var bgImage = bubbleBackground.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.enabled = !isEffectSound;
            }
            else
            {
                bubbleBackground.gameObject.SetActive(!isEffectSound);
            }
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeTextRoutine(processedText, isEffectSound, shakeIndices, onComplete));
    }

    /// <summary>
    /// 지정된 지연 시간 후에 말풍선을 닫습니다.
    /// </summary>
    public void Close(float delay = 0f)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(CloseRoutine(delay));
        }
    }

    private IEnumerator TypeTextRoutine(string fullText, bool isEffectSound, List<int> shakeIndices, System.Action onComplete)
    {
        if (textComponent != null)
        {
            if (isEffectSound)
            {
                // 효과음일 때는 태그 노출 방지를 위해 미리 텍스트를 채우고 글자 수만 제어합니다.
                textComponent.text = fullText;
                textComponent.maxVisibleCharacters = 0;
                textComponent.ForceMeshUpdate();
            }
            else
            {
                // 일반 대사일 때는 말풍선 크기가 실시간으로 슥 늘어나도록 빈 텍스트로 시작합니다.
                textComponent.text = "";
                textComponent.maxVisibleCharacters = 9999; // 제한 해제
            }
        }

        // 등장 이펙트(스케일 애니메이션)가 제거되어 즉시 원래 스케일 상태를 유지합니다.
        if (bubbleBackground != null)
        {
            bubbleBackground.localScale = originalScale;
        }

        if (textComponent != null)
        {
            if (isEffectSound)
            {
                // [효과음 모드] maxVisibleCharacters를 늘려가며 타이핑 (태그 미노출)
                int totalVisibleCharacters = textComponent.textInfo.characterCount;

                for (int i = 0; i <= totalVisibleCharacters; i++)
                {
                    if (i > 0 && shakeIndices != null && shakeIndices.Contains(i - 1))
                    {
                        TriggerCameraShake();
                    }

                    textComponent.maxVisibleCharacters = i;

                    float delay = typeSpeed;

                    if (i > 0 && i - 1 < textComponent.textInfo.characterInfo.Length)
                    {
                        char c = textComponent.textInfo.characterInfo[i - 1].character;
                        
                        // 타이핑 효과음 재생 (공백 제외 및 글자 간격 체크)
                        if (typingSound != null && audioSource != null && c != ' ' && c != '\n' && c != '\r')
                        {
                            if (i % soundInterval == 0)
                            {
                                audioSource.PlayOneShot(typingSound, typingVolume);
                            }
                        }

                        if (c == '.')
                        {
                            delay = typeSpeed * dotDelayMultiplier;
                        }
                        else if (c == ',' || c == '!' || c == '?')
                        {
                            delay = typeSpeed * punctuationDelayMultiplier;
                        }
                    }

                    yield return new WaitForSecondsRealtime(delay);
                }
            }
            else
            {
                // [일반 대사 모드] 한 글자씩 문자를 늘려가며 말풍선 동적 확장 연출 복원
                string currentText = "";
                char[] characters = fullText.ToCharArray();

                for (int i = 0; i < characters.Length; i++)
                {
                    if (shakeIndices != null && shakeIndices.Contains(i))
                    {
                        TriggerCameraShake();
                    }

                    char c = characters[i];
                    currentText += c;
                    textComponent.text = currentText;

                    // 타이핑 효과음 재생 (공백 제외 및 글자 간격 체크)
                    if (typingSound != null && audioSource != null && c != ' ' && c != '\n' && c != '\r')
                    {
                        if (i % soundInterval == 0)
                        {
                            audioSource.PlayOneShot(typingSound, typingVolume);
                        }
                    }

                    float delay = typeSpeed;

                    if (c == '.')
                    {
                        delay = typeSpeed * dotDelayMultiplier;
                    }
                    else if (c == ',' || c == '!' || c == '?')
                    {
                        delay = typeSpeed * punctuationDelayMultiplier;
                    }

                    yield return new WaitForSecondsRealtime(delay);
                }
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

    /// <summary>
    /// 유니티 인스펙터 이벤트(UnityEvent) 슬롯에서 대사를 다이렉트로 입력해 띄우기 위한 전용 함수입니다.
    /// 타이핑이 끝나면 설정해둔 defaultAutoCloseDelay 초 뒤에 자동으로 닫힙니다.
    /// </summary>
    public void ShowDialogueInspector(string text)
    {
        Show(text, () => {
            if (defaultAutoCloseDelay > 0f)
            {
                Close(defaultAutoCloseDelay);
            }
        });
    }

    private string ProcessShakeTags(string originalText, out List<int> shakeIndices)
    {
        shakeIndices = new List<int>();
        if (string.IsNullOrEmpty(originalText)) return originalText;

        string processed = originalText;
        int index;
        while ((index = processed.IndexOf("#@$")) != -1)
        {
            shakeIndices.Add(index);
            processed = processed.Remove(index, 3); // Remove "#@$"
        }
        return processed;
    }

    private void TriggerCameraShake()
    {
        if (Camera.main != null)
        {
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.Shake(0.5f, 0.35f);
            }
        }
    }
}

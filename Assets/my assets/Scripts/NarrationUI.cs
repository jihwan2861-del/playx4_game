using UnityEngine;
using System;
using System.Collections;
using TMPro;

/// <summary>
/// 화면 상단에서 부드러운 페이드 및 타이핑 연출로 세계관을 설명하는 
/// 시네마틱 나레이션 UI 스크립트입니다.
/// </summary>
public class NarrationUI : MonoBehaviour
{
    public static NarrationUI instance;

    [Header("=== UI 컴포넌트 연결 ===")]
    [Tooltip("나레이션을 띄울 TextMeshPro - Text 컴포넌트")]
    public TextMeshProUGUI textComponent;

    [Header("=== 페이드 속도 ===")]
    [Tooltip("글자가 나타나고 사라지는 페이드 시간 (초)")]
    public float fadeDuration = 0.5f;

    [Header("=== 타이핑 속도 및 완급 조절 ===")]
    [Tooltip("글자당 출력 시간 (초)")]
    public float typeSpeed = 0.05f;
    [Tooltip("마침표 (.)가 찍혔을 때 타이핑 대기 배율")]
    public float dotDelayMultiplier = 4.5f;
    [Tooltip("쉼표 (,), 느낌표 (!), 물음표 (?)가 찍혔을 때 타이핑 대기 배율")]
    public float punctuationDelayMultiplier = 3.0f;

    private Coroutine activeCoroutine;
    private Color originalColor;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (textComponent != null)
        {
            originalColor = textComponent.color;
            // 시작할 때는 완전히 안 보이게 투명하게 설정
            SetTextAlpha(0f);
            textComponent.text = "";
        }
    }

    /// <summary>
    /// 나레이션 텍스트를 출력합니다.
    /// </summary>
    public void Show(string text, Action onComplete = null)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        activeCoroutine = StartCoroutine(ShowRoutine(text, onComplete));
    }

    /// <summary>
    /// 나레이션을 페이드아웃하며 닫습니다.
    /// </summary>
    public void Close(float delay = 0f)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        activeCoroutine = StartCoroutine(CloseRoutine(delay));
    }

    private IEnumerator ShowRoutine(string fullText, Action onComplete)
    {
        if (textComponent == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // 1. 기존 텍스트 지우고 투명한 상태로 설정
        textComponent.text = "";
        SetTextAlpha(0f);

        // 2. 글자 페이드인 (Alpha: 0 -> 1)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaledDeltaTime 적용
            SetTextAlpha(Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        SetTextAlpha(1f);

        // 3. 타이핑 효과 적용 (SpeechBubble과 일치하는 문장부호 완급조절 탑재)
        string currentText = "";
        char[] characters = fullText.ToCharArray();

        for (int i = 0; i < characters.Length; i++)
        {
            char c = characters[i];
            currentText += c;
            textComponent.text = currentText;

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

        // 출력 완료 콜백 호출
        onComplete?.Invoke();
    }

    private IEnumerator CloseRoutine(float delay)
    {
        if (textComponent == null) yield break;

        // 1. 사라지기 전 지정된 시간만큼 지연
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // 2. 글자 페이드아웃 (Alpha: 1 -> 0)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaledDeltaTime 적용
            SetTextAlpha(Mathf.Clamp01(1f - (elapsed / fadeDuration)));
            yield return null;
        }
        SetTextAlpha(0f);
        textComponent.text = "";
    }

    private void SetTextAlpha(float alpha)
    {
        if (textComponent != null)
        {
            Color c = originalColor;
            c.a = alpha;
            textComponent.color = c;
        }
    }
}

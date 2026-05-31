using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 《산나비》 스타일의 동적 캐릭터 말풍선 시스템입니다.
/// 유니티 기본 Text 컴포넌트를 사용하여 한국어를 별도 에셋 로드 없이 100% 선명하게 출력합니다.
/// </summary>
public class SpeechBubble : MonoBehaviour
{
    private Canvas canvas;
    private Image backgroundImage;
    private Text textComponent;
    
    private Transform targetTransform;
    private Vector3 positionOffset = new Vector3(0f, 2.0f, 0f); // 캐릭터 머리 위 여유 오프셋
    
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private System.Action onCompleteCallback;

    /// <summary>
    /// 말풍선을 동적으로 생성 및 초기화합니다.
    /// </summary>
    public static SpeechBubble Create(GameObject owner, string text, Color themeColor, System.Action onComplete = null)
    {
        // 중첩 생성 방지
        SpeechBubble existing = owner.GetComponentInChildren<SpeechBubble>();
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject bubbleObj = new GameObject("DynamicSpeechBubble");
        bubbleObj.transform.SetParent(owner.transform);
        bubbleObj.transform.localPosition = Vector3.zero;

        SpeechBubble speechBubble = bubbleObj.AddComponent<SpeechBubble>();
        speechBubble.InitAndShow(owner.transform, text, themeColor, onComplete);
        return speechBubble;
    }

    private void InitAndShow(Transform target, string text, Color themeColor, System.Action onComplete)
    {
        targetTransform = target;
        onCompleteCallback = onComplete;

        // 1. 월드 스페이스 캔버스 고해상도 빌드 (텍스트 깨짐 방지 대형 스케일법)
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "Default";
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20f;

        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(400f, 200f);
        canvasRt.localScale = new Vector3(0.012f, 0.012f, 1f); // 텍스트 해상도를 확보하고 0.012배로 미니어처화

        // 2. 프리미엄 사이버펑크 스타일 9-Slice 배경 자동 생성
        GameObject bgObj = new GameObject("BubbleBG");
        bgObj.transform.SetParent(transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.sprite = CreateProceduralBubbleSprite(themeColor);
        backgroundImage.type = Image.Type.Sliced;

        RectTransform bgRt = backgroundImage.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.0f); // 꼬리 바닥 기준점
        bgRt.localPosition = Vector3.zero;

        // 3. 한국어 100% 호환 내장 기본 폰트 탑재 텍스트 추가
        GameObject textObj = new GameObject("BubbleText");
        textObj.transform.SetParent(bgObj.transform, false);
        textComponent = textObj.AddComponent<Text>();
        
        // Arial ➔ LegacyRuntime ➔ OS 폰트 순서대로 완벽한 한글 호환 폰트 확보
        Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 24);
        
        textComponent.font = defaultFont;
        textComponent.fontSize = 20;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;

        // 텍스트 여백 설정
        RectTransform textRt = textComponent.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.localPosition = Vector3.zero;
        
        // 내부 패딩 설정
        textRt.offsetMin = new Vector2(15f, 15f);
        textRt.offsetMax = new Vector2(-15f, -15f);

        // 4. 머리 위 팝업 위치 고정
        transform.position = targetTransform.position + positionOffset;

        // 5. 타이핑 이펙트 & 바운싱 애니메이션 기동
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextRoutine(text, bgRt));
        StartCoroutine(PopUpBounceRoutine(bgRt));
    }

    private void Update()
    {
        // 2D 씬 빌보드 처리 (회전 방지)
        transform.rotation = Quaternion.identity;

        // 플레이어 캐릭터 머리 위에 부드럽게 고정 추적
        if (targetTransform != null)
        {
            Vector3 targetPos = targetTransform.position + positionOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 18f);
        }
    }

    public void Close(float delay = 0f)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(CloseRoutine(delay));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator CloseRoutine(float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        RectTransform bgRt = backgroundImage.GetComponent<RectTransform>();
        float elapsed = 0f;
        float duration = 0.15f;
        Vector3 startScale = bgRt.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            bgRt.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);

        if (onCompleteCallback != null)
        {
            onCompleteCallback.Invoke();
            onCompleteCallback = null;
        }
    }

    private IEnumerator TypeTextRoutine(string fullText, RectTransform bgRt)
    {
        isTyping = true;
        textComponent.text = "";
        
        string currentText = "";
        char[] characters = fullText.ToCharArray();

        for (int i = 0; i < characters.Length; i++)
        {
            currentText += characters[i];
            textComponent.text = currentText;

            // 한글 포함 길이에 따라 9-Slice 배경판 실시간 가변 조정 (픽셀 해상도 대응)
            float preferredWidth = Mathf.Clamp(textComponent.preferredWidth + 40f, 150f, 380f);
            float preferredHeight = textComponent.preferredHeight + 35f;
            bgRt.sizeDelta = new Vector2(preferredWidth, preferredHeight);

            yield return new WaitForSecondsRealtime(0.035f); // 찰진 타이핑 타이밍
        }

        isTyping = false;
    }

    private IEnumerator PopUpBounceRoutine(RectTransform bgRt)
    {
        float elapsed = 0f;
        float duration = 0.4f;
        bgRt.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            // 쫀득한 탄성 감쇠 애니메이션 기법
            float scaleValue = Mathf.Sin(t * Mathf.PI * 1.4f) * Mathf.Lerp(1.25f, 1.0f, t);
            if (t >= 0.96f) scaleValue = 1f;

            bgRt.localScale = new Vector3(scaleValue, scaleValue, 1f);
            yield return null;
        }
        bgRt.localScale = Vector3.one;
    }

    private Sprite CreateProceduralBubbleSprite(Color themeColor)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color darkBg = new Color(0.04f, 0.04f, 0.06f, 0.94f); // 중후한 네온 그레이 SF 백그라운드

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distFromBorder = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                
                if (distFromBorder < 0)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (distFromBorder <= 2)
                {
                    tex.SetPixel(x, y, themeColor);
                }
                else
                {
                    tex.SetPixel(x, y, darkBg);
                }
            }
        }

        // 말풍선 아래의 '꼬리' 모양을 픽셀 상에 그려 줌
        int tailWidth = 10;
        int tailHeight = 8;
        int startX = size / 2 - tailWidth / 2;
        int endX = size / 2 + tailWidth / 2;

        for (int y = 0; y < tailHeight; y++)
        {
            int indent = y;
            for (int x = startX + indent; x <= endX - indent; x++)
            {
                if (x >= 0 && x < size)
                {
                    if (x == startX + indent || x == endX - indent || y == 0)
                    {
                        tex.SetPixel(x, y, themeColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, darkBg);
                    }
                }
            }
        }

        tex.Apply();

        // 9-Slice 보더 슬라이싱 영역 (16픽셀 상하좌우 보존)
        Vector4 borderPadding = new Vector4(16, 16, 16, 16);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.0f), 20f, 0, SpriteMeshType.FullRect, borderPadding);
    }
}

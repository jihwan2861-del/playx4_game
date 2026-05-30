using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 《산나비》 스타일의 동적 캐릭터 말풍선 시스템입니다.
/// 특정 오브젝트 머리 위에 팝업되며, 텍스트 길이에 따라 배경 크기가 탄력적으로 맞춤 조절됩니다.
/// 에디터 작업 없이도 동적으로 월드 스페이스 캔버스를 빌드하여 작동할 수 있게 설계되었습니다.
/// </summary>
public class SpeechBubble : MonoBehaviour
{
    private Canvas canvas;
    private Image backgroundImage;
    private TextMeshProUGUI textComponent;
    
    private Transform targetTransform;
    private Vector3 positionOffset = new Vector3(0f, 1.8f, 0f);
    
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private System.Action onCompleteCallback;

    /// <summary>
    /// 말풍선을 동적으로 생성 및 초기화합니다.
    /// </summary>
    public static SpeechBubble Create(GameObject owner, string text, Color themeColor, System.Action onComplete = null)
    {
        // 이미 머리 위에 말풍선이 존재한다면 제거하여 중첩을 방지합니다.
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

        // 1. 월드 스페이스 캔버스 동적 빌드
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // 소팅 레이어 및 오더 설정 (항상 몬스터나 장애물보다 앞에 그려지도록 높여 둠)
        canvas.sortingLayerName = "Default";
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20f;

        // 캔버스 크기 제어
        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(5f, 2.5f);
        canvasRt.localScale = new Vector3(0.7f, 0.7f, 1f); // 2.5D 및 일반 씬 스케일에 알맞게 스케일 조정

        // 2. 프리미엄 사이버펑크 스타일 배경 패널 자동 생성
        GameObject bgObj = new GameObject("BubbleBG");
        bgObj.transform.SetParent(transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.sprite = CreateProceduralBubbleSprite(themeColor);
        backgroundImage.type = Image.Type.Sliced; // 9-Slice 대응

        RectTransform bgRt = backgroundImage.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.0f); // 말풍선 꼬리 쪽(바닥)을 기준점으로 잡음
        bgRt.localPosition = Vector3.zero;

        // 3. 텍스트 컴포넌트 추가 및 폰트 세팅
        GameObject textObj = new GameObject("BubbleText");
        textObj.transform.SetParent(bgObj.transform, false);
        textComponent = textObj.AddComponent<TextMeshProUGUI>();
        
        textComponent.fontSize = 2.0f; // 월드 스페이스 크기 대응
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        
        // 폰트 스타일을 살짝 굵게 설정
        textComponent.fontStyle = FontStyles.Bold;

        // 텍스트 여백 설정
        RectTransform textRt = textComponent.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.localPosition = Vector3.zero;
        
        // 패딩 적용
        textComponent.margin = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);

        // 4. 머리 위 팝업 위치 고정
        transform.position = targetTransform.position + positionOffset;

        // 5. 타이핑 이펙트 & 바운싱 스케일 오프닝 연출 시작
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextRoutine(text, bgRt));
        StartCoroutine(PopUpBounceRoutine(bgRt));
    }

    private void Update()
    {
        // 카메라를 똑바로 바라보도록 빌보드(Billboard) 처리
        transform.rotation = Quaternion.identity;

        // 대상의 위치를 부드럽게 실시간 추적 (Y축 오프셋 자동 연동)
        if (targetTransform != null)
        {
            Vector3 targetPos = targetTransform.position + positionOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 15f);
        }
    }

    /// <summary>
    /// 말풍선이 닫힐 때 완전히 소멸하거나 페이드아웃 되게 조작하는 외부 메서드입니다.
    /// </summary>
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

        // 부드럽게 0으로 수축하며 소멸
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

    /// <summary>
    /// 타이핑 글자 출력 및 9-Slice 크기 자동 리사이징 루틴입니다.
    /// </summary>
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

            // 텍스트 크기에 맞게 배경 패널 크기를 실시간 리사이징
            float preferredWidth = Mathf.Clamp(textComponent.preferredWidth + 0.8f, 2.5f, 6.0f);
            float preferredHeight = textComponent.preferredHeight + 0.6f;
            bgRt.sizeDelta = new Vector2(preferredWidth, preferredHeight);

            // 산나비 특유의 글자 타이핑 딜레이 속도 (초당)
            yield return new WaitForSecondsRealtime(0.04f);
        }

        isTyping = false;
    }

    /// <summary>
    /// 0.0에서 1.0으로 띠용 하며 튀어 오르는 탄성(Elastic) 애니메이션 연출입니다.
    /// </summary>
    private IEnumerator PopUpBounceRoutine(RectTransform bgRt)
    {
        float elapsed = 0f;
        float duration = 0.45f;
        bgRt.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            // Elastic Ease Out 공식 대입 (쫀득하게 튀어 오름)
            float scaleValue = Mathf.Sin(t * Mathf.PI * 1.5f) * Mathf.Lerp(1.2f, 1.0f, t);
            if (t >= 0.95f) scaleValue = 1f;

            bgRt.localScale = new Vector3(scaleValue, scaleValue, 1f);
            yield return null;
        }
        bgRt.localScale = Vector3.one;
    }

    /// <summary>
    /// 애셋 이미지 파일 로드 없이도 자체적으로 구워내는 사이버 네온 스타일 말풍선 9-Slice 텍스처입니다.
    /// </summary>
    private Sprite CreateProceduralBubbleSprite(Color themeColor)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color darkBg = new Color(0.05f, 0.05f, 0.07f, 0.9f); // 불투명한 하이테크 그레이

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 테두리로부터의 최소 거리 계산 (둥근 모서리용)
                float distFromBorder = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                
                if (distFromBorder < 0)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (distFromBorder <= 2)
                {
                    // 가장자리 2픽셀은 강렬한 네온 보더라인 처리
                    tex.SetPixel(x, y, themeColor);
                }
                else
                {
                    // 안쪽은 반투명하고 묵직한 하이테크 배경색
                    tex.SetPixel(x, y, darkBg);
                }
            }
        }

        // 말풍선 아래의 '꼬리' 모양을 픽셀 상에 그려 줌
        int tailWidth = 8;
        int tailHeight = 6;
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

        // 9-Slice 보더 패딩 영역 지정 (동적 크기 조절 대응)
        Vector4 borderPadding = new Vector4(12, 12, 12, 12);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.0f), 20f, 0, SpriteMeshType.FullRect, borderPadding);
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("이동할 씬 이름")]
    [Tooltip("터치 시 즉시 넘어갈 대상 씬의 정확한 이름입니다. (기본값: ride_scene)")]
    public string gameSceneName = "ride_scene";

    [Header("UI 설정")]
    [Tooltip("화면에 표시할 스타트 안내 문구입니다.")]
    public string startText = "TOUCH TO START";
    
    [Tooltip("텍스트 폰트 크기")]
    public int fontSize = 50;

    [Tooltip("텍스트가 깜빡이는 속도")]
    public float blinkSpeed = 2f;

    [Header("효과음 (선택사항)")]
    public AudioClip startSFX;

    private bool isStarting = false;
    private Text textComponent;
    private Canvas dynamicCanvas;

    private void Start()
    {
        // 시간 배속을 원래대로 돌려놓습니다. (정상 스케일 보장)
        Time.timeScale = 1f; 

        // 씬에 UI가 직접 구성되어 있지 않은 경우를 대비해, 동적으로 Canvas와 Text를 생성합니다.
        CreateDynamicUI();

        // 텍스트 깜빡임 루프 시작
        if (textComponent != null)
        {
            StartCoroutine(BlinkTextRoutine());
        }
    }

    private void Update()
    {
        if (isStarting) return;

        // 마우스 클릭 또는 화면 터치 감지
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            StartGame();
        }
    }

    private void CreateDynamicUI()
    {
        // 씬에 이미 Canvas가 있는지 확인
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        Transform parentTransform = null;

        if (existingCanvas == null)
        {
            // Canvas가 없다면 새로 생성
            GameObject canvasObj = new GameObject("TitleCanvas");
            dynamicCanvas = canvasObj.AddComponent<Canvas>();
            dynamicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dynamicCanvas.sortingOrder = 100;

            // Canvas Scaler 추가 (화면 해상도 대응)
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Graphic Raycaster 추가 (이벤트 감지용)
            canvasObj.AddComponent<GraphicRaycaster>();
            
            parentTransform = canvasObj.transform;
        }
        else
        {
            parentTransform = existingCanvas.transform;
            // 기존 캔버스 백업 (페이드용)
            dynamicCanvas = existingCanvas;
        }

        // 씬에 이미 Text가 존재하는지 확인
        Text existingText = FindObjectOfType<Text>();
        if (existingText == null)
        {
            // 텍스트가 없다면 동적 생성
            GameObject textObj = new GameObject("TouchToStartText");
            textObj.transform.SetParent(parentTransform, false);

            textComponent = textObj.AddComponent<Text>();
            
            // 폰트 설정 (기본 Arial)
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.text = startText;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;

            // 그림자 효과 추가 (텍스트 가독성 향상)
            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(2f, -2f);

            // 레이아웃 위치 설정 (화면 중앙 하단)
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.25f);
            rect.anchorMax = new Vector2(0.5f, 0.25f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800, 100);
        }
        else
        {
            textComponent = existingText;
            // 텍스트 내용 강제 적용
            if (string.IsNullOrEmpty(textComponent.text))
            {
                textComponent.text = startText;
            }
        }
    }

    private IEnumerator BlinkTextRoutine()
    {
        while (!isStarting)
        {
            if (textComponent != null)
            {
                // PingPong 함수를 이용해 알파 값을 0.2 ~ 1.0 사이로 부드럽게 조절
                float alpha = 0.2f + Mathf.PingPong(Time.time * blinkSpeed, 0.8f);
                Color color = textComponent.color;
                color.a = alpha;
                textComponent.color = color;
            }
            yield return null;
        }
    }

    private void StartGame()
    {
        isStarting = true;
        Debug.Log($"[MainMenuManager] 터치 감지됨! [{gameSceneName}]으로 진입합니다.");

        // 클릭 효과음 재생
        if (startSFX != null)
        {
            AudioSource.PlayClipAtPoint(startSFX, Camera.main != null ? Camera.main.transform.position : transform.position);
        }
        else
        {
            // 기본 SFX가 할당 안되었을 경우, HubUIManager 등이 있으면 그것의 리소스를 시도하거나 생략
            if (HubUIManager.instance != null && HubUIManager.instance.buttonClickSFX != null)
            {
                AudioSource.PlayClipAtPoint(HubUIManager.instance.buttonClickSFX, Camera.main != null ? Camera.main.transform.position : transform.position);
            }
        }

        // 텍스트를 빠르게 깜빡이거나 하이라이트하는 간단한 연출
        StartCoroutine(StartTransitionRoutine());
    }

    private IEnumerator StartTransitionRoutine()
    {
        // 텍스트 깜빡임 속도를 아주 빠르게 만들어 선택 피드백 제공
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            if (textComponent != null)
            {
                textComponent.enabled = !textComponent.enabled;
            }
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }

        if (textComponent != null)
        {
            textComponent.enabled = true;
            Color c = textComponent.color;
            c.a = 1f;
            textComponent.color = c;
        }

        // GameTransitionManager를 통해 부드러운 페이드 아웃/인 전환을 거쳐 텔레포트합니다.
        if (GameTransitionManager.instance != null)
        {
            GameTransitionManager.instance.TriggerTransition(gameSceneName);
        }
        else
        {
            // 직접 페이드아웃 효과 구현 (만약 씬에 TransitionManager가 없는 독립 씬일 경우를 대비)
            yield return StartCoroutine(FadeOutCanvasRoutine());
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private IEnumerator FadeOutCanvasRoutine()
    {
        if (dynamicCanvas == null) yield break;

        // 검은색 페이드 이미지 생성
        GameObject blackImageObj = new GameObject("FadeOutImage");
        blackImageObj.transform.SetParent(dynamicCanvas.transform, false);
        Image blackImg = blackImageObj.AddComponent<Image>();
        blackImg.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rect = blackImg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.one;

        float timer = 0f;
        float fadeDuration = 0.8f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            if (blackImg != null) blackImg.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
    }
}

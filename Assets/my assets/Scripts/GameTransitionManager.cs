using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 전투 씬(game_Scene)의 시작 연출과 죽었을 때의 화면 페이드 효과를 관리합니다.
/// </summary>
public class GameTransitionManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Image fadeImage;          // 화면 전체를 덮는 검은색 UI 이미지
    public GameObject gameOverPanel; // [마을로], [재시작] 버튼이 있는 패널

    [Header("시간 설정")]
    public float blackWaitTime = 2f;    // 처음 새카만 화면 유지 시간
    public float fadeOutTime = 2f;      // 서서히 밝아지는 시간
    public float deathFadeTime = 2f;    // 죽었을 때 서서히 어두워지는 시간

    public static GameTransitionManager instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        // 시작 시 초기 셋업
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 1); // 완전 검은색
        }

        // 게임 시작 연출 실행
        StartCoroutine(StartTransitionRoutine());
    }

    private IEnumerator StartTransitionRoutine()
    {
        // 1. 월드 시간 완전 정지
        Time.timeScale = 0f;

        // 2. 정지된 상태에서 2초 동안 대기 (현실 시간 기준)
        yield return new WaitForSecondsRealtime(blackWaitTime);

        // 3. 서서히 게임 화면이 보임 (투명해짐)
        float timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // 4. 연출이 끝나면 게임 시간 정상화
        Time.timeScale = 1f;
        if (fadeImage != null) fadeImage.gameObject.SetActive(false);
    }

    // 플레이어 스크립트에서 죽었을 때 이 함수를 호출함
    public void OnPlayerDeath()
    {
        StartCoroutine(DeathTransitionRoutine());
    }

    private IEnumerator DeathTransitionRoutine()
    {
        // 죽자마자 화면을 서서히 검게 칠함
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0); // 투명 상태에서 시작
        }

        float timer = 0f;
        while (timer < deathFadeTime)
        {
            timer += Time.unscaledDeltaTime; // 게임 시간이 정지될 수도 있으므로 현실 시간 기준
            float alpha = Mathf.Lerp(0f, 1f, timer / deathFadeTime);
            if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // 완전히 까매지면 사망 패널 띄우고 시간 정지
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // 버튼 이벤트: 마을로 돌아가기
    public void OnClickReturnToVillage()
    {
        TriggerTransition("Hub_Scene");
    }

    // 버튼 이벤트: 재시작
    public void OnClickRestart()
    {
        TriggerTransition(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 씬이 파괴될 때 코루틴이 끊기는 현상을 완벽 차단하기 위해
    /// DontDestroyOnLoad 오브젝트를 먼저 만들고, 여기에 TransitionRunner 컴포넌트를 붙여 트랜지션을 실행합니다.
    /// </summary>
    private void TriggerTransition(string sceneName)
    {
        GameObject transitionCanvas = new GameObject("PersistentTransitionCanvas");
        DontDestroyOnLoad(transitionCanvas);

        TransitionRunner runner = transitionCanvas.AddComponent<TransitionRunner>();
        runner.StartCoroutine(runner.Run(sceneName, gameOverPanel, this));
    }
}

/// <summary>
/// 씬이 전환되어 기존 GameTransitionManager 오브젝트가 파괴되더라도
/// 중단 없이 독립적으로 살아남아 비동기 로드 대기 및 부드러운 페이드 아웃/인 처리를 완수하는 영속성 도우미 컴포넌트입니다.
/// </summary>
public class TransitionRunner : MonoBehaviour
{
    public IEnumerator Run(string sceneName, GameObject gameOverPanel, GameTransitionManager originalManager)
    {
        // 1. 월드 시간 정상화 (씬 전환 및 비동기 처리를 원활히 진행하기 위해 1f로 복원)
        Time.timeScale = 1f;

        // 2. GameOver UI 패널 페이드 아웃 연출 (CanvasGroup이 달려있다면 페이드, 없으면 즉각 꺼짐)
        if (gameOverPanel != null)
        {
            CanvasGroup cg = gameOverPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                float t = 0f;
                while (t < 0.4f)
                {
                    t += Time.unscaledDeltaTime;
                    cg.alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
                    yield return null;
                }
            }
            gameOverPanel.SetActive(false);
        }

        // 3. 씬이 바뀌어도 파괴되지 않는 블랙 캔버스를 빌드합니다.
        GameObject transitionCanvas = this.gameObject;
        
        Canvas canvas = transitionCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 모든 UI, 파티클 위에 렌더링되도록 최상단 레이어 부여

        // 해상도 대응용 Scaler 추가
        var scaler = transitionCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // 전체화면을 덮을 블랙 이미지 배치
        GameObject blackImageObj = new GameObject("BlackImage");
        blackImageObj.transform.SetParent(transitionCanvas.transform, false);
        Image blackImg = blackImageObj.AddComponent<Image>();
        blackImg.color = new Color(0f, 0f, 0f, 1f); // 완전한 블랙스크린

        // RectTransform을 화면 전체 꽉 채움 정렬로 구성
        RectTransform rect = blackImg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.one;

        // 4. 비동기(Asynchronous)로 새로운 씬 로드 진행
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 5. 로드 완료 직후, 새로 로딩된 씬에 독자적인 페이드 시스템(GameTransitionManager)이 존재하는지 확인합니다.
        // 이때 originalManager는 이미 씬 해제와 함께 파괴되었으므로, 새로 찾아진 다른 인스턴스인지만 체크합니다.
        GameTransitionManager newSceneManager = FindObjectOfType<GameTransitionManager>();
        if (newSceneManager != null && newSceneManager != originalManager)
        {
            // 새 씬의 트랜지션 매니저가 자연스럽게 자신의 블랙 스크린부터 시작하게끔 위임하고 즉시 파괴
            Destroy(transitionCanvas);
            yield break;
        }

        // 6. 자체 페이드 기능이 없는 씬(예: Hub_Scene)에 진입한 경우, 이 캔버스를 통해 1.0초간 부드럽게 밝아지는 페이드인(Fade In)을 실행합니다.
        float fadeTimer = 0f;
        float fadeDuration = 1.0f;
        while (fadeTimer < fadeDuration)
        {
            if (blackImg == null) break;
            
            fadeTimer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);
            blackImg.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        // 7. 연출 완료 시 메모리 누수를 방지하기 위해 캔버스 완전 파괴
        Destroy(transitionCanvas);
    }
}

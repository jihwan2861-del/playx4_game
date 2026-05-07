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
        Time.timeScale = 1f; // 시간 정지 풀고 넘어가야 함
        SceneManager.LoadScene("Village_Scene");
    }

    // 버튼 이벤트: 재시작
    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

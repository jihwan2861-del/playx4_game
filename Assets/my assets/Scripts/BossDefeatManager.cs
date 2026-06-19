using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 보스 사망 시 페이드아웃 후 허브 씬으로 자동 복귀하는 독립형 매니저입니다.
/// 하이어라키에 빈 게임오브젝트를 만들고 이 스크립트를 붙이면 끝!
/// 다른 스크립트에 의존하지 않고 독자적으로 보스 사망을 감지합니다.
/// </summary>
public class BossDefeatManager : MonoBehaviour
{
    [Header("=== 설정 ===")]
    [Tooltip("보스 사망 후 페이드아웃 시작까지 대기 시간 (초)")]
    public float delayBeforeFade = 2.5f;

    [Tooltip("페이드아웃 지속 시간 (초)")]
    public float fadeDuration = 1.5f;

    [Tooltip("이동할 씬 이름")]
    public string hubSceneName = "Hub_Scene";

    private bool hasTriggered = false;

    private void Update()
    {
        // 이미 트리거됐으면 무시
        if (hasTriggered) return;

        // BossManager가 존재하고, 보스 HP가 0 이하가 되면 발동
        if (BossManager.instance != null && BossManager.instance.currentHp <= 0)
        {
            hasTriggered = true;
            Debug.Log("💀 [BossDefeatManager] 보스 사망 감지! 허브 씬 복귀 시퀀스를 시작합니다.");
            StartReturnSequence();
        }
    }

    private void StartReturnSequence()
    {
        // 이 오브젝트를 DontDestroyOnLoad로 만들어서 씬 전환 중에도 살아남게 함
        DontDestroyOnLoad(gameObject);

        // 타임스케일 안전 복원
        Time.timeScale = 1f;

        // 플레이어 무적 처리
        if (Player.instance != null)
        {
            Player.instance.safeZoneInvincible = true;
        }

        // 모든 적/투사체 제거
        ClearBattlefield();

        // 승리 UI 표시
        if (PlayerUI.instance != null)
        {
            PlayerUI.instance.ShowVictory();
        }

        // 칩 보상 지급
        GiveRewards();

        // 미션 완료 저장
        SaveMissionProgress();

        // 페이드아웃 → 씬 전환 코루틴 실행
        StartCoroutine(FadeAndLoadHub());
    }

    private IEnumerator FadeAndLoadHub()
    {
        // 1. 승리 화면 대기 (현실 시간 기준 - timeScale 무관)
        yield return new WaitForSecondsRealtime(delayBeforeFade);

        Debug.Log("🏠 [BossDefeatManager] 페이드아웃 시작...");

        // 2. 페이드아웃 캔버스 생성
        GameObject fadeCanvasObj = new GameObject("DefeatFadeCanvas");
        DontDestroyOnLoad(fadeCanvasObj);

        Canvas canvas = fadeCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99999;

        var scaler = fadeCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // 3. 블랙 이미지 생성
        GameObject blackObj = new GameObject("BlackFade");
        blackObj.transform.SetParent(fadeCanvasObj.transform, false);
        Image blackImg = blackObj.AddComponent<Image>();
        blackImg.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rect = blackImg.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 4. 페이드아웃 (투명 → 검정)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            blackImg.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        blackImg.color = Color.black;

        // 5. 허브 씬 로드
        Debug.Log($"🏠 [BossDefeatManager] {hubSceneName} 씬으로 이동합니다!");
        SceneManager.LoadScene(hubSceneName);

        // 6. 씬 로드 후 정리
        yield return null;
        Destroy(fadeCanvasObj);
        Destroy(gameObject);
    }

    private void ClearBattlefield()
    {
        // 적 제거
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies) Destroy(e);

        // 토템 제거
        var totems = FindObjectsOfType<TotemAI>();
        foreach (var t in totems) if (t != null) Destroy(t.gameObject);

        // 적 탄알 제거
        var projectiles = FindObjectsOfType<Projectile>();
        foreach (var p in projectiles)
            if (p != null && p.enemyBullet) Destroy(p.gameObject);

        // 레이저 제거
        var lasers = FindObjectsOfType<LaserBeam>();
        foreach (var l in lasers) if (l != null) Destroy(l.gameObject);

        Debug.Log("🧹 [BossDefeatManager] 전장 정리 완료!");
    }

    private void GiveRewards()
    {
        if (PlayerDataManager.instance == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        int chipReward = 20;
        if (sceneName == "Stage2_Scene") chipReward = 25;
        else if (sceneName == "Stage3_Scene") chipReward = 30;

        PlayerDataManager.instance.AddChips(chipReward);
        Debug.Log($"💎 [BossDefeatManager] 칩 {chipReward}개 지급!");
    }

    private void SaveMissionProgress()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "game_Scene")
            PlayerPrefs.SetInt("Mission_Stage1_Completed", 1);
        else if (sceneName == "Stage2_Scene")
            PlayerPrefs.SetInt("Mission_Stage2_Completed", 1);
        else if (sceneName == "Stage3_Scene")
            PlayerPrefs.SetInt("Mission_Stage3_Completed", 1);

        PlayerPrefs.Save();
        Debug.Log($"💾 [BossDefeatManager] 미션 완료 저장! ({sceneName})");
    }
}

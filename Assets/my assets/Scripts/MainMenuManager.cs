using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("이동할 씬 이름")]
    [Tooltip("게임이 시작되자마자 즉시 넘어갈 대상 씬의 정확한 이름입니다. (기본값: Hub_Scene)")]
    public string gameSceneName = "ride_scene";

    private void Start()
    {
        // 시간 배속을 원래대로 돌려놓습니다. (정상 스케일 보장)
        Time.timeScale = 1f; 

        Debug.Log($"[MainMenuManager] 게임 기동 완료. [{gameSceneName}]으로 페이드 아웃하며 진입합니다!");
        
        // GameTransitionManager를 통해 부드러운 페이드 아웃/인 전환을 거쳐 텔레포트합니다.
        if (GameTransitionManager.instance != null)
        {
            GameTransitionManager.instance.TriggerTransition(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
}

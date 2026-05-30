using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("이동할 씬 이름")]
    [Tooltip("게임이 시작되자마자 즉시 넘어갈 대상 씬의 정확한 이름입니다. (기본값: Hub_Scene)")]
    public string gameSceneName = "Hub_Scene";

    private void Start()
    {
        // 시간 배속을 원래대로 돌려놓습니다. (정상 스케일 보장)
        Time.timeScale = 1f; 

        Debug.Log($"[MainMenuManager] 게임 기동 완료. 복잡한 메인 UI를 건너뛰고 [{gameSceneName}]으로 즉시 진입합니다!");
        
        // 씬 로딩 지연 없이 곧바로 허브 씬(또는 지정된 씬)으로 텔레포트합니다.
        SceneManager.LoadScene(gameSceneName);
    }
}

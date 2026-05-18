using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("이동할 씬 이름")]
    [Tooltip("게임 시작 버튼을 눌렀을 때 넘어갈 스테이지 씬의 이름을 정확히 적어주세요. (예: Level_1)")]
    public string gameSceneName = "Level_1";

    /// <summary>
    /// 게임 시작 버튼에 연결할 함수입니다.
    /// </summary>
    public void OnClickStartGame()
    {
        Debug.Log($"[{gameSceneName}] 씬으로 이동합니다!");
        
        // 시간 배속을 원래대로 돌려놓습니다. (혹시 이전 씬에서 멈춰있었을 경우 대비)
        Time.timeScale = 1f; 
        
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// 게임 종료 버튼에 연결할 함수입니다.
    /// </summary>
    public void OnClickQuitGame()
    {
        Debug.Log("게임을 종료합니다.");
        Application.Quit(); // 실제 빌드된 게임에서만 작동합니다.
    }
}

using UnityEngine;
using UnityEngine.SceneManagement; // 🔄 씬 전환을 위해 네임스페이스 추가

/// <summary>
/// 에디터 및 테스트 빌드에서 게임 속도를 조절하거나 씬을 즉시 변경하는 개발자 치트(디버그) 매니저입니다.
/// 씬에 오브젝트를 배치하지 않아도 시작 시 자동으로 활성화됩니다.
/// </summary>
public class DeveloperSpeedController : MonoBehaviour
{
    // 게임 시작 시 백그라운드에서 자동으로 치트 매니저 오브젝트를 생성하고 파괴불가 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameObject speedController = new GameObject("GlobalDeveloperSpeedController");
        speedController.AddComponent<DeveloperSpeedController>();
        DontDestroyOnLoad(speedController);
        Debug.Log("🛠️ [개발자 치트] 글로벌 매니저 활성화. (F1~F4: 속도 조절, 숫자 1: game_Scene 즉시 이동)");
    }

    private void Update()
    {
        // 🚀 숫자 1 키를 누르면 타임스케일을 1배속으로 원복하고 game_Scene 씬으로 즉시 전환
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            SceneManager.LoadScene("game_Scene");
            Debug.Log("🔄 [개발자 치트] 1번 키 입력 - game_Scene 씬으로 즉시 이동합니다.");
        }

        // F1~F4 키를 눌러 타임스케일(배속) 조절
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetSpeed(1.0f);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            SetSpeed(2.0f);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            SetSpeed(3.0f);
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            SetSpeed(4.0f);
        }
    }

    private void SetSpeed(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * scale; // 기본 fixedDeltaTime = 0.02f 기준으로 보정
        Debug.Log($"⏳ [개발자 치트] 게임 속도가 {scale}배속으로 변경되었습니다.");
    }
}

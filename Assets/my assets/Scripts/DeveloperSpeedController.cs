using UnityEngine;

/// <summary>
/// 시연 및 테스트 속도를 단축하기 위해 모든 씬에서 작동하는 전역 배속 조절(개발자 치트) 매니저입니다.
/// 씬에 직접 오브젝트를 배치하지 않아도 런타임 시작 시 자동으로 생성되어 동작합니다.
/// </summary>
public class DeveloperSpeedController : MonoBehaviour
{
    // 런타임 시작 시 씬 배치 없이 자동으로 치트 오브젝트를 초기화 및 생성합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameObject speedController = new GameObject("GlobalDeveloperSpeedController");
        speedController.AddComponent<DeveloperSpeedController>();
        DontDestroyOnLoad(speedController);
        Debug.Log("🚀 [개발자 모드] 배속 조절 매니저가 전역 활성화되었습니다. (단축키: F1=1배속, F2=2배속, F3=3배속, F4=4배속)");
    }

    private void Update()
    {
        // F1, F2, F3, F4 단축키로 게임 전체 배속(Time.timeScale) 제어
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
        // 물리 프레임 주기도 배속에 맞춰 스케일링해주어야 물리 충돌과 움직임이 깨지지 않고 부드럽습니다.
        Time.fixedDeltaTime = 0.02f * scale; // 유니티 기본 fixedDeltaTime = 0.02f
        Debug.Log($"⚡ [개발자 모드] 게임 속도가 {scale}배속으로 변경되었습니다.");
    }
}

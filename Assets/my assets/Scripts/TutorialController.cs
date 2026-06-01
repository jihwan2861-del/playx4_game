using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 제어 및 기본적인 유틸리티 이벤트를 제공하는 경량 시네마틱 매니저입니다.
/// 무거운 하드코딩 대사 관리를 모두 걷어내고, 유니티 인스펙터(UnityEvent)에서
/// 호출할 수 있는 유연한 기능(씬 이동, 플레이어 제어)만 제공하도록 정석 설계되었습니다.
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController instance;

    // 외부 호환성 유지를 위한 컴포넌트 자동 검색 필드
    [HideInInspector] public PlayerMoving player;

    [HideInInspector] public int currentPhase = 2; // 이전 스크립트들과의 컴파일 호환성을 위한 유지

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 플레이어 캐릭터 자동 검색 및 캐싱
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerMoving>();
        }
    }

    /// <summary>
    /// 인스펙터 이벤트 슬롯에서 다이렉트로 호출하여 다른 씬으로 넘어갈 때 사용하는 함수입니다.
    /// </summary>
    /// <param name="sceneName">이동할 씬 이름 (예: Hub_Scene)</param>
    public void LoadNextScene(string sceneName)
    {
        Debug.Log($"🎬 [TutorialController] 씬 전환을 시도합니다: -> {sceneName}");
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 대화나 시네마틱 연출 중에 플레이어 조작을 일시적으로 차단/허용하는 인스펙터 연동용 함수입니다.
    /// </summary>
    /// <param name="freeze">true이면 조작 차단, false이면 조작 허용</param>
    public void FreezePlayer(bool freeze)
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.GetComponent<PlayerMoving>();
        }

        if (player != null)
        {
            player.enabled = !freeze;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            Debug.Log($"🎮 [TutorialController] 플레이어 기체 제어 상태 변경: Freeze = {freeze}");
        }
    }
}

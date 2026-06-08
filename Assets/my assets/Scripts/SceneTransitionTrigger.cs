using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어 기체(Player)가 닿으면(TriggeEnter2D) 지정된 씬으로
/// 즉시 안전하게 이동시키는 정석적인 씬 전환 트리거 스크립트입니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("=== 씬 전환 설정 ===")]
    [Tooltip("플레이어가 닿았을 때 로딩할 유니티 씬(Scene) 이름 (예: game_Scene, Hub_Scene 등)")]
    public string sceneToLoad = "game_Scene";

    [Tooltip("씬 전환 시 혹시 꼬여있을 수 있는 타임스케일(슬로우 모션 등)을 1.0(정상)으로 복구할지 여부")]
    public bool resetTimeScale = true;

    private void Awake()
    {
        // 물리 트리거 연동을 위해 Is Trigger 강제 활성화
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log($"🛡️ [{gameObject.name}] SceneTransitionTrigger의 Collider2D 'Is Trigger' 옵션을 강제로 활성화했습니다.");
        }
    }

    private void Start()
    {
        // 🔄 [스테이지 2개 단축 연동]
        // 캠페인 진행 단계 (게임시작-라이드-폐연구실-허브-스테이지1-스테이지2)에 맞춰 포탈 타겟을 강제 연쇄시킵니다.
        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene == "ride_scene" && (sceneToLoad == "game_Scene" || sceneToLoad == "Stage2_Scene"))
        {
            sceneToLoad = "Lab_scene";
            Debug.Log($"🔄 [SceneTransitionTrigger] 스테이지 연쇄: {activeScene} ➔ 'Lab_scene'으로 강제 보정 완료.");
        }
        else if (activeScene == "Lab_scene" && (sceneToLoad == "Stage2_Scene" || sceneToLoad == "Stage3_Scene" || sceneToLoad == "game_Scene"))
        {
            sceneToLoad = "Hub_Scene";
            Debug.Log($"🔄 [SceneTransitionTrigger] 스테이지 연쇄: {activeScene} 완료 ➔ 'Hub_Scene'으로 강제 보정 완료.");
        }
        else if (activeScene == "game_Scene" && sceneToLoad == "Stage3_Scene")
        {
            sceneToLoad = "Stage2_Scene";
            Debug.Log($"🔄 [SceneTransitionTrigger] 스테이지 연쇄: {activeScene} 완료 ➔ 'Stage2_Scene'으로 강제 보정 완료.");
        }
        else if (activeScene == "Stage2_Scene" && (sceneToLoad == "Stage3_Scene" || sceneToLoad == "game_Scene"))
        {
            sceneToLoad = "Hub_Scene";
            Debug.Log($"🔄 [SceneTransitionTrigger] 스테이지 연쇄: {activeScene} 완료 ➔ 'Hub_Scene' 복귀로 강제 보정 완료.");
        }
    }

    /// <summary>
    /// UnityEvent(예: PlayerAutoMove의 OnArrivedWaypoint)에서 
    /// 직접 호출하여 안전하게 지정된 씬으로 전환시킬 수 있는 public 함수입니다.
    /// </summary>
    public void TriggerTransition()
    {
        if (resetTimeScale)
        {
            Time.timeScale = 1f;
        }

        Debug.Log($"🎬 [SceneTransitionTrigger] TriggerTransition() 호출! '{sceneToLoad}' 씬을 로드합니다.");
        SceneManager.LoadScene(sceneToLoad);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 진입한 오브젝트가 플레이어인지 태그 검사
        if (other.CompareTag("Player"))
        {
            if (resetTimeScale)
            {
                Time.timeScale = 1f;
            }

            Debug.Log($"🎬 [SceneTransitionTrigger] 플레이어 감지! '{sceneToLoad}' 씬을 로드합니다.");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}

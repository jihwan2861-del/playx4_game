using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어가 닿았을 때 다른 씬으로 이동하게 해주는 포탈 컴포넌트입니다.
/// GameTransitionManager와 연계하여 부드러운 페이드 아웃 연출과 함께 씬을 안전하게 전환합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    [Header("=== 목적지 설정 ===")]
    [Tooltip("이동하고자 하는 목표 씬의 정확한 이름")]
    public string targetSceneName;

    [Header("=== 작동 옵션 ===")]
    [Tooltip("트리거에 진입했을 때 자동으로 씬을 바로 로딩할지 여부")]
    public bool triggerOnTouch = true;

    private bool isTransitioning = false;

    private void Awake()
    {
        // 2D 트리거 충돌이 필수적이므로 Is Trigger 옵션을 강제 적용
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        // 🔄 [스테이지 2개 단축 연동]
        // 캠페인 진행 단계 (게임시작-라이드-폐연구실-허브-스테이지1-스테이지2)에 맞춰 포탈 타겟을 강제 연쇄시킵니다.
        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene == "ride_scene" && (targetSceneName == "game_Scene" || targetSceneName == "Stage2_Scene"))
        {
            targetSceneName = "Lab_scene";
            Debug.Log($"🔄 [ScenePortal] 스테이지 연쇄: {activeScene} ➔ 'Lab_scene'으로 강제 보정 완료.");
        }
        else if (activeScene == "Lab_scene" && (targetSceneName == "Stage2_Scene" || targetSceneName == "Stage3_Scene" || targetSceneName == "game_Scene"))
        {
            targetSceneName = "Hub_Scene";
            Debug.Log($"🔄 [ScenePortal] 스테이지 연쇄: {activeScene} 완료 ➔ 'Hub_Scene'으로 강제 보정 완료.");
        }
        else if (activeScene == "game_Scene" && targetSceneName == "Stage3_Scene")
        {
            targetSceneName = "Stage2_Scene";
            Debug.Log($"🔄 [ScenePortal] 스테이지 연쇄: {activeScene} 완료 ➔ 'Stage2_Scene'으로 강제 보정 완료.");
        }
        else if (activeScene == "Stage2_Scene" && (targetSceneName == "Stage3_Scene" || targetSceneName == "game_Scene"))
        {
            targetSceneName = "Hub_Scene";
            Debug.Log($"🔄 [ScenePortal] 스테이지 연쇄: {activeScene} 완료 ➔ 'Hub_Scene' 복귀로 강제 보정 완료.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnTouch || isTransitioning) return;

        // 플레이어 태그 검사
        if (other.CompareTag("Player"))
        {
            TriggerSceneTransition();
        }
    }

    /// <summary>
    /// 다른 씬으로의 로딩 트랜지션을 작동시킵니다.
    /// </summary>
    public void TriggerSceneTransition()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"⚠️ [{gameObject.name}] ScenePortal의 targetSceneName이 비어있습니다!");
            return;
        }

        isTransitioning = true;
        Debug.Log($"🌌 [{gameObject.name}] '{targetSceneName}' 씬으로 이동 연출을 시작합니다.");

        // GameTransitionManager를 통한 고급 페이드 연출 씬 이동 시도
        if (GameTransitionManager.instance != null)
        {
            GameTransitionManager.instance.TriggerTransition(targetSceneName);
        }
        else
        {
            // 백업: 직접 일반 씬 전환 실행
            SceneManager.LoadScene(targetSceneName);
        }
    }
}

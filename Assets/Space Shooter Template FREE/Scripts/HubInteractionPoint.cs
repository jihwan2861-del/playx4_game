using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 허브 씬의 상호작용 포인트입니다.
/// 차고, 홀로그램, 작업실 등에 부착하면 플레이어가 가까이 가면 안내 UI가 뜨고,
/// 키를 누르면 해당 기능이 실행됩니다.
/// </summary>
public class HubInteractionPoint : MonoBehaviour
{
    public enum PointType
    {
        Garage,     // 차고 → 출격 (오토바이 미니게임)
        Hologram,   // 홀로그램 → 스토리/통신
        Workshop    // 작업실 → 업그레이드
    }

    [Header("상호작용 설정")]
    public PointType pointType;
    public KeyCode interactKey = KeyCode.F;
    public float interactRadius = 2f;

    [Header("UI")]
    [Tooltip("'F키를 눌러 상호작용' 같은 안내 텍스트 오브젝트")]
    public GameObject promptUI;

    private bool playerInRange = false;

    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        // 플레이어 존재 확인
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            // 태그가 없을 수도 있으니 컴포넌트로 찾기
            HubPlayerMovement hpm = FindObjectOfType<HubPlayerMovement>();
            if (hpm != null) player = hpm.gameObject;
        }
        if (player == null) return;

        // 거리 체크
        float dist = Vector2.Distance(transform.position, player.transform.position);
        bool inRange = dist <= interactRadius;

        // 범위 진입/이탈 시 안내 UI 토글
        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (promptUI != null) promptUI.SetActive(playerInRange);
        }

        // 키 입력 시 상호작용 실행
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    void Interact()
    {
        HubUIManager hub = FindObjectOfType<HubUIManager>();
        if (hub == null)
        {
            Debug.LogWarning("⚠️ HubUIManager를 찾을 수 없습니다!");
            return;
        }

        switch (pointType)
        {
            case PointType.Garage:
                Debug.Log("🏍️ [차고] 출격 준비!");
                hub.OpenStageSelect();
                break;

            case PointType.Hologram:
                Debug.Log("📡 [홀로그램] 통신 연결...");
                hub.OpenHologram();
                break;

            case PointType.Workshop:
                Debug.Log("🔧 [작업실] 업그레이드 터미널 접속...");
                hub.OpenShop();
                break;
        }
    }

    // 에디터에서 상호작용 범위를 시각적으로 확인
    private void OnDrawGizmosSelected()
    {
        Color c = pointType == PointType.Garage ? Color.cyan :
                  pointType == PointType.Hologram ? Color.magenta :
                  Color.yellow;
        Gizmos.color = new Color(c.r, c.g, c.b, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

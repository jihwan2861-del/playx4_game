using UnityEngine;
using System.Collections;

/// <summary>
/// '1st scene' 시네마틱 오프닝 튜토리얼을 관리하는 마스터 컨트롤러입니다.
/// 비주얼 교체 제어를 완전히 제거하고, 첫 번째 포인트(자동 정차 지점) 및 두 번째 포인트(연구소 입구 등)에서
/// 순차적으로 출력될 대사(Dialogue) 시퀀스를 통합 제어하도록 리팩토링되었습니다.
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController instance;

    // 플레이어 캐싱 필드 (자동 검색)
    [HideInInspector] public PlayerMoving player;

    [Header("=== 첫 번째 포인트 대사 설정 ===")]
    [Tooltip("첫 번째 도착지 체크포인트 트리거")]
    public StoryTriggerZone firstCheckpoint;
    [TextArea(2, 5)]
    [Tooltip("첫 번째 포인트에 정차했을 때 출력할 순차 대사 목록")]
    public string[] firstCheckpointDialogs = new string[] {
        "여기가 첫 번째 구역인가... 조심해야겠군."
    };

    [Header("=== 두 번째 포인트 대사 설정 ===")]
    [Tooltip("두 번째 포인트 트랜스폼 (연구소 입구 오브젝트 등을 드래그)")]
    public Transform secondCheckpointTransform;
    [TextArea(2, 5)]
    [Tooltip("두 번째 포인트 근처에 도달했을 때 출력할 순차 대사 목록")]
    public string[] secondCheckpointDialogs = new string[] {
        "저기가 연구소 입구인가?",
        "안에 무언가 숨겨져 있을 것 같군."
    };

    // 시네마틱 페이즈 관리 (외부 스크립트와의 호환성 유지)
    // 1: 오토바이 자동 주행 중
    // 2: 도착 완료 및 자유 조작 가능 상태
    [HideInInspector] public int currentPhase = 1;

    private CameraFollow mainCameraFollow;
    private bool secondTriggered = false;

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

    private void Start()
    {
        // 메인 카메라의 카메라 폴로우 컴포넌트 자동 캐싱
        if (Camera.main != null)
        {
            mainCameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        // PlayerAutoMove에 도착 콜백을 동적으로 자동 등록
        if (player != null)
        {
            PlayerAutoMove autoMove = player.GetComponent<PlayerAutoMove>();
            if (autoMove != null)
            {
                autoMove.onArrivedEvent.AddListener(PlayFirstCheckpointDialogues);
            }

            // 💬 [안전 보완] 플레이어 자식 오브젝트에서 비활성화된 말풍선 컴포넌트까지 전부 샅샅이 뒤져서 바인딩합니다.
            SpeechBubble bubble = player.GetComponentInChildren<SpeechBubble>(true);
            if (bubble != null)
            {
                SpeechBubble.playerBubbleInstance = bubble;
                Debug.Log("💬 [TutorialController] 플레이어 자식 SpeechBubble을 성공적으로 찾아 playerBubbleInstance로 등록했습니다.");
            }
            else
            {
                Debug.LogWarning("⚠️ [TutorialController] 플레이어의 자식 오브젝트 중에서 SpeechBubble 스크립트를 찾을 수 없습니다! 계층 구조를 확인해 주세요.");
            }
        }
    }

    private void Update()
    {
        // 2단계 (자유 운전 중)이고 두 번째 포인트가 등록되어 있다면 거리 감시
        if (currentPhase == 2 && secondCheckpointTransform != null && player != null && !secondTriggered)
        {
            float dist = Vector3.Distance(player.transform.position, secondCheckpointTransform.position);
            if (dist <= 2.0f) // 2미터 반경 이내로 접근 시 대사 작동
            {
                PlaySecondCheckpointDialogues();
            }
        }
    }

    /// <summary>
    /// 첫 번째 포인트 도착 대사 시퀀스를 실행합니다. (자동 주행 도착 시 호출)
    /// </summary>
    public void PlayFirstCheckpointDialogues()
    {
        if (currentPhase != 1) return;
        currentPhase = 2;
        
        StartCoroutine(FirstCheckpointDialogRoutine());
    }

    private IEnumerator FirstCheckpointDialogRoutine()
    {
        if (player == null) yield break;

        // 대사 출력 중에는 오토바이 조작 일시 정지
        player.enabled = false;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        // 첫 번째 대사 목록 순차 출력
        foreach (string dialog in firstCheckpointDialogs)
        {
            if (string.IsNullOrEmpty(dialog)) continue;

            bool nextDial = false;
            if (SpeechBubble.playerBubbleInstance != null)
            {
                SpeechBubble.playerBubbleInstance.Show(dialog, () => nextDial = true);
            }
            else
            {
                nextDial = true;
            }
            yield return new WaitUntil(() => nextDial);
            yield return new WaitForSeconds(1.0f);
        }

        // 마지막 대사 소멸 대기 및 닫기
        if (SpeechBubble.playerBubbleInstance != null)
        {
            SpeechBubble.playerBubbleInstance.Close(1.0f);
            yield return new WaitForSeconds(1.2f);
        }

        // 대사 완료 후 수동 조작(WASD) 완전 해금!
        player.enabled = true;
    }

    /// <summary>
    /// 두 번째 포인트 도달 대사 시퀀스를 실행합니다.
    /// </summary>
    public void PlaySecondCheckpointDialogues()
    {
        if (secondTriggered) return;
        secondTriggered = true;
        
        StartCoroutine(SecondCheckpointDialogRoutine());
    }

    private IEnumerator SecondCheckpointDialogRoutine()
    {
        if (player == null) yield break;

        // 대사 출력 중 조작 일시 정지
        player.enabled = false;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        // 두 번째 대사 목록 순차 출력
        foreach (string dialog in secondCheckpointDialogs)
        {
            if (string.IsNullOrEmpty(dialog)) continue;

            bool nextDial = false;
            if (SpeechBubble.playerBubbleInstance != null)
            {
                SpeechBubble.playerBubbleInstance.Show(dialog, () => nextDial = true);
            }
            else
            {
                nextDial = true;
            }
            yield return new WaitUntil(() => nextDial);
            yield return new WaitForSeconds(1.0f);
        }

        // 마지막 대사 소멸 대기 및 닫기
        if (SpeechBubble.playerBubbleInstance != null)
        {
            SpeechBubble.playerBubbleInstance.Close(1.0f);
            yield return new WaitForSeconds(1.2f);
        }

        // 대사 완료 후 다시 조작 개방!
        player.enabled = true;
    }

    private IEnumerator RidingDialogueRoutine()
    {
        if (player == null) yield break;

        yield return new WaitForSeconds(0.5f);

        string[] dialogs = new string[] {
            "또 연료 필터가 나갔네.",
            "이번엔 제대로 된 부품을 구해야겠어.",
            "저번에 스캔해둔 구역이 이 근처였는데...",
            "응?",
            "저런 시설이 여기 있었나?",
            "지도에도 없던 곳인데."
        };

        float[] waitTimes = new float[] { 1.2f, 1.2f, 1.5f, 0.6f, 1.0f, 1.0f };

        for (int i = 0; i < dialogs.Length; i++)
        {
            bool nextDial = false;
            if (SpeechBubble.playerBubbleInstance != null)
            {
                SpeechBubble.playerBubbleInstance.Show(dialogs[i], () => nextDial = true);
            }
            else
            {
                nextDial = true;
            }
            yield return new WaitUntil(() => nextDial);
            yield return new WaitForSeconds(waitTimes[i]);
        }

        if (SpeechBubble.playerBubbleInstance != null)
        {
            SpeechBubble.playerBubbleInstance.Close(1.0f);
        }
    }
}

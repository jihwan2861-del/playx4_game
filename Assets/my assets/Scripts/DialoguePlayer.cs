using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 유니티 인스펙터(UnityEvent) 슬롯에서 직접 여러 대사 시퀀스를 순차 재생할 수 있게 돕는
/// 초정석적이고 직관적인 1회성/순차 대사 플레이어 컴포넌트입니다.
/// </summary>
public class DialoguePlayer : MonoBehaviour
{
    [Header("=== 대사 리스트 ===")]
    [TextArea(2, 5)]
    [Tooltip("순차적으로 출력할 대사 목록")]
    public List<string> dialogues = new List<string>();

    [Tooltip("한 대사의 타이핑이 완전히 끝난 후, 다음 대사로 넘어가기 전까지 쉬어갈 시간 (초)")]
    public float delayBetweenDialogues = 1.5f;

    [Header("=== 마지막 대사 설정 ===")]
    [Tooltip("모든 대사 출력이 끝나고, 마지막 대사를 화면에 몇 초 동안 보여준 뒤 출발시킬지 지정 (0.8~1.0초 등 짧은 설정 추천)")]
    public float lastDialogueCloseDelay = 1.0f;

    [Header("=== 자동 주행 연동 ===")]
    [Tooltip("대사 재생 중에 PlayerAutoMove 자동 주행을 자동으로 일시정지 시킬지 여부")]
    public bool pauseAutoMoveDuringDialogue = true;

    private Coroutine dialogueCoroutine;

    /// <summary>
    /// 인스펙터 이벤트 창에서 이 함수를 단 한 번 드래그 앤 드롭으로 호출해주면,
    /// 등록된 모든 대사 리스트가 순차적으로 타이핑되며 완벽하게 출력됩니다!
    /// </summary>
    public void PlayDialogues()
    {
        if (dialogues == null || dialogues.Count == 0) return;

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }
        dialogueCoroutine = StartCoroutine(PlayDialoguesRoutine());
    }

    private IEnumerator PlayDialoguesRoutine()
    {
        SpeechBubble bubble = SpeechBubble.playerBubbleInstance;
        if (bubble == null)
        {
            // 백업용 검색
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                bubble = player.GetComponentInChildren<SpeechBubble>(true);
            }
        }

        if (bubble == null)
        {
            Debug.LogError("⚠️ [DialoguePlayer] 화면에 띄울 SpeechBubble 인스턴스를 찾을 수 없습니다!");
            yield break;
        }

        // ⏸️ [자동 정차 연동] 대사 시작 시 주행 일시 정지 호출
        PlayerAutoMove autoMove = null;
        if (pauseAutoMoveDuringDialogue)
        {
            autoMove = bubble.GetComponentInParent<PlayerAutoMove>();
            if (autoMove == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) autoMove = player.GetComponent<PlayerAutoMove>();
            }

            if (autoMove != null)
            {
                autoMove.PauseMove();
            }
        }

        // 순차 출력 루프
        for (int i = 0; i < dialogues.Count; i++)
        {
            string dialogue = dialogues[i];
            if (string.IsNullOrEmpty(dialogue)) continue;

            bool isTypingDone = false;
            
            // 중요: 순차 재생 중에는 개별 대사 도중 자동으로 말풍선이 닫히지 않도록,
            // SpeechBubble의 일반 Show(text, onComplete) 호출을 사용하여 흐름을 독점적으로 제어합니다.
            bubble.Show(dialogue, () => isTypingDone = true);

            // 해당 줄 타이핑이 다 끝날 때까지 대기
            yield return new WaitUntil(() => isTypingDone);

            // 마지막 대사가 아니라면, 지정된 지연시간만큼 대기 후 다음 대사로 전환
            if (i < dialogues.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenDialogues);
            }
        }

        // 🏁 모든 대사 재생이 끝난 최종 완료 시점에 말풍선을 닫아줍니다.
        // 이제 획일적인 딜레이가 아닌, 개발자가 인스펙터에서 지정한 마지막 대사 전용 딜레이(lastDialogueCloseDelay)를 적용합니다!
        bubble.Close(lastDialogueCloseDelay);

        // ⏳ [말풍선 완전 소멸 타이밍 대기 연동]
        // 마지막 대사 유지 시간(lastDialogueCloseDelay) + 스케일 수축 여유 시간(0.3초)만큼 기다립니다.
        yield return new WaitForSeconds(lastDialogueCloseDelay + 0.3f);

        // ▶️ [자동 주행 재개 연동] 모든 대사가 끝나고 말풍선이 완전히 화면에서 사라진 순간에 주행 재개 호출
        if (pauseAutoMoveDuringDialogue && autoMove != null)
        {
            autoMove.ResumeMove();
        }
    }
}

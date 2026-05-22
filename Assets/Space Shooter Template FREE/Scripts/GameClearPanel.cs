using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 스테이지 클리어(승리) 시 화면에 반투명 어두운 패널과 함께 텍스트를 부드럽게 페이드인 시켜주는 연출 스크립트입니다.
/// 일반 UI Text와 TextMesh Pro(TMP) 모두를 자동으로 찾아 지원하며, 다른 UI에 가려지지 않도록 최상단 자동 정렬 기능을 포함합니다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class GameClearPanel : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    [Header("페이드 연출 설정")]
    [Tooltip("패널 전체가 완전히 페이드인 되는 데 소요되는 시간 (초)")]
    public float fadeDuration = 1.8f;

    [Header("클리어 텍스트 객체 연결 (Text/TMP 무관)")]
    [Tooltip("화면에 크게 뜰 STAGE CLEAR 타이틀 텍스트 오브젝트 (드래그하여 대입)")]
    public GameObject clearTitleObject;
    [Tooltip("하단에 작게 뜰 서브 정보 텍스트 오브젝트 (드래그하여 대입)")]
    public GameObject clearSubObject;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        
        // 씬 시작 시에는 완전히 숨겨두고 꺼둡니다.
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 외부(PlayerUI 등)에서 클리어 연출을 트리거할 때 호출하는 메소드입니다.
    /// </summary>
    public void PlayClearAnimation()
    {
        gameObject.SetActive(true);
        
        // UI가 다른 이미지/패널 뒤에 숨는 것을 원천 방지하기 위해 캔버스 최상단 레이어로 정렬
        transform.SetAsLastSibling();
        
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;

        // 텍스트 내용 주입 (일반 Text 와 TextMeshPro(TMP) 모두에 대응 가능하도록 안전 보조 처리)
        SetTextSafe(clearTitleObject, "STAGE CLEAR");
        SetTextSafe(clearSubObject, "MISSION COMPLETE!\nSAFE ZONE REACHED");

        // 지정된 시간 동안 알파 값을 0에서 1로 부드럽게 증가시킴
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 대상 오브젝트에서 일반 UI Text 또는 TextMeshPro(TMP_Text) 컴포넌트를 찾아 안전하게 텍스트를 주입합니다.
    /// </summary>
    private void SetTextSafe(GameObject targetObj, string textValue)
    {
        if (targetObj == null) return;

        // 1. 일반 UI.Text 감지 및 대입
        Text normalText = targetObj.GetComponent<Text>();
        if (normalText != null)
        {
            // 비어 있거나 기본 설정인 경우에만 덮어씀
            if (string.IsNullOrEmpty(normalText.text) || normalText.text == "New Text" || normalText.text == "Text")
            {
                normalText.text = textValue;
            }
            return;
        }

        // 2. TextMesh Pro (TMP) 감지 및 대입 (Reflection 에러 방지 위해 표준 TMPro.TMP_Text 참조)
        var tmpText = targetObj.GetComponent<TMPro.TMP_Text>();
        if (tmpText != null)
        {
            if (string.IsNullOrEmpty(tmpText.text) || tmpText.text == "New Text" || tmpText.text == "Text")
            {
                tmpText.text = textValue;
            }
            return;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 이미지를 여러 장(슬라이드)으로 넘겨가며 보여주는 스크립트입니다.
/// </summary>
public class TutorialSlider : MonoBehaviour
{
    [Header("슬라이드 패널들 (1장, 2장, 3장... 순서대로)")]
    public GameObject[] slides;
    
    [Header("UI 버튼 및 텍스트")]
    public Button prevButton;   // [이전] 버튼
    public Button nextButton;   // [다음] 버튼
    public Text pageText;       // "1 / 3" 표시용 텍스트

    private int currentIndex = 0;

    // 패널이 켜질 때마다 무조건 1페이지(인덱스 0)부터 시작
    private void OnEnable()
    {
        ShowSlide(0);
    }

    public void ShowSlide(int index)
    {
        if (slides == null || slides.Length == 0) return;

        // 인덱스가 배열 범위를 벗어나지 않게 고정
        currentIndex = Mathf.Clamp(index, 0, slides.Length - 1);

        // 모든 슬라이드를 끄고, 현재 인덱스의 슬라이드만 켬
        for (int i = 0; i < slides.Length; i++)
        {
            slides[i].SetActive(i == currentIndex);
        }

        // 첫 페이지면 [이전] 버튼 비활성화, 마지막 페이지면 [다음] 버튼 비활성화
        if (prevButton != null) prevButton.interactable = (currentIndex > 0);
        if (nextButton != null) nextButton.interactable = (currentIndex < slides.Length - 1);

        // 페이지 숫자 표시 (예: 1 / 3)
        if (pageText != null) pageText.text = (currentIndex + 1) + " / " + slides.Length;
    }

    // 버튼의 OnClick에 연결할 함수들
    public void NextSlide()
    {
        ShowSlide(currentIndex + 1);
    }

    public void PrevSlide()
    {
        ShowSlide(currentIndex - 1);
    }

    public void CloseTutorial()
    {
        gameObject.SetActive(false);
    }
}

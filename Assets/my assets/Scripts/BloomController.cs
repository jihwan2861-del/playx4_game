// BloomController 기능 비활성화 - 이 프로젝트는 URP를 사용하지 않습니다.
// Bloom 효과는 유니티 에디터에서 Post Processing 패키지를 별도로 설치해야 사용 가능합니다.
// 현재는 컴파일 오류 방지를 위해 빈 껍데기로 유지합니다.
using UnityEngine;

public class BloomController : MonoBehaviour
{
    public static BloomController instance;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // Bloom 미지원 환경 - 호출해도 아무 동작 안 함 (오류 없이 무시)
    public void DoBloom(float targetIntensity, float duration) { }
}

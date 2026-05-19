using UnityEngine;

/// <summary>
/// 2D/2.5D 탑다운 및 Isometric 게임에서 캐릭터와 벽의 Y좌표(높낮이)에 따라
/// 그리기 순서(Sorting Order)를 실시간으로 자동 정렬해주는 스크립트입니다.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class YSorter : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("움직이지 않는 장애물(벽, 기둥 등)은 True로 설정하여 한 번만 정렬해 렉을 방지합니다.")]
    public bool isStatic = true;

    [Tooltip("정밀 정렬을 위한 미세 오프셋 값 (필요 시 조절)")]
    public int sortingOffset = 0;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (spriteRenderer == null) return;

        // 화면 상에서 발밑(Y좌표가 낮을수록)에 있을수록 카메라와 가까우므로
        // Sorting Order 값을 높여 가장 앞에 그려지게 합니다.
        // 정밀한 계산을 위해 100을 곱해 정밀도를 높입니다.
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100) + sortingOffset;

        // 정적인 오브젝트(벽, 장애물)는 게임 실행 시 한 번만 계산하고 스크립트를 꺼서 성능을 보존합니다.
        if (isStatic && Application.isPlaying)
        {
            enabled = false;
        }
    }
}

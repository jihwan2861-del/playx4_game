using UnityEngine;
using System.Collections;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.Tilemaps;
#endif

/// <summary>
/// 플레이어가 천장 타일이나 구조물 아래로 들어갔을 때, 
/// 천장을 부드럽게 반투명하게(Fade Out) 만들어 캐릭터가 보이도록 하고,
/// 벗어나면 다시 원래대로 불투명하게(Fade In) 돌려놓는 프리미엄 2.5D 효과 스크립트입니다.
/// SpriteRenderer와 Tilemap 컴포넌트를 모두 지원합니다.
/// </summary>
public class CeilingFader : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("플레이어가 밑에 있을 때 천장의 불투명도 (0 = 완전 투명, 1 = 완전 불투명)")]
    [Range(0f, 1f)] public float fadedAlpha = 0.25f;
    [Tooltip("페이드 속도 (값이 클수록 더 빠르게 전환됩니다)")]
    public float fadeSpeed = 5.0f;

    private SpriteRenderer spriteRenderer;
#if UNITY_2017_2_OR_NEWER
    private Tilemap tilemap;
#endif
    private float targetAlpha = 1.0f;
    private Color currentColor;
    private bool isTilemap = false;

    private void Awake()
    {
        // 1. SpriteRenderer가 있는지 확인
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            currentColor = spriteRenderer.color;
            targetAlpha = 1.0f;
            isTilemap = false;
            return;
        }

#if UNITY_2017_2_OR_NEWER
        // 2. Tilemap 컴포넌트가 있는지 확인
        tilemap = GetComponent<Tilemap>();
        if (tilemap != null)
        {
            currentColor = tilemap.color;
            targetAlpha = 1.0f;
            isTilemap = true;
        }
#endif
    }

    private void Update()
    {
        // 컴포넌트가 없으면 업데이트 안 함
        if (spriteRenderer == null && !isTilemap) return;

        // 현재 알파값을 타겟 알파값으로 부드럽게 보간(Lerp)합니다.
        float currentAlpha = isTilemap ? GetTilemapAlpha() : spriteRenderer.color.a;
        float newAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        
        currentColor.a = newAlpha;

        if (isTilemap)
        {
            SetTilemapColor(currentColor);
        }
        else
        {
            spriteRenderer.color = currentColor;
        }
    }

    private float GetTilemapAlpha()
    {
#if UNITY_2017_2_OR_NEWER
        if (tilemap != null) return tilemap.color.a;
#endif
        return 1f;
    }

    private void SetTilemapColor(Color color)
    {
#if UNITY_2017_2_OR_NEWER
        if (tilemap != null) tilemap.color = color;
#endif
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 오브젝트가 플레이어인지 확인합니다
        if (collision.CompareTag("Player") || collision.GetComponent<PlayerMoving>() != null || collision.GetComponent<Player>() != null)
        {
            targetAlpha = fadedAlpha;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponent<PlayerMoving>() != null || collision.GetComponent<Player>() != null)
        {
            targetAlpha = 1.0f; // 원래 투명도로 원상복구
        }
    }
}

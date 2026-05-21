using UnityEngine;

/// <summary>
/// 보스 대시 이동 시 생성되는 네온 잔상 효과를 제어합니다.
/// </summary>
public class BossAfterimage : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color startColor;
    private float fadeDuration;
    private float elapsedTime = 0f;

    /// <summary>
    /// 잔상의 속성을 초기화합니다.
    /// </summary>
    public void Initialize(Sprite sourceSprite, Color neonColor, float duration, Vector3 scale, Quaternion rotation, bool flipX, bool flipY, int sortingOrder)
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sourceSprite;
        spriteRenderer.color = neonColor;
        spriteRenderer.flipX = flipX;
        spriteRenderer.flipY = flipY;
        spriteRenderer.sortingOrder = sortingOrder - 1; // 보스 바로 뒤에 배치하여 자연스럽게 보이도록 함

        transform.localScale = scale;
        transform.rotation = rotation;
        
        startColor = neonColor;
        fadeDuration = duration;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        float progress = elapsedTime / fadeDuration;

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
        else
        {
            // 투명도(Alpha)를 0으로 선형 보간하여 서서히 페이드 아웃
            Color currentColor = startColor;
            currentColor.a = Mathf.Lerp(startColor.a, 0f, progress);
            spriteRenderer.color = currentColor;
        }
    }
}

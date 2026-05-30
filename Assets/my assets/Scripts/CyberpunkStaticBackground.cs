using UnityEngine;

/// <summary>
/// 배경 이미지를 화면 전체에 꽉 차게 고정시켜 주는 스크립트입니다.
/// 빈 오브젝트가 아니라 **배경 이미지(SpriteRenderer)가 달린 오브젝트**에 직접 붙여주세요!
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CyberpunkStaticBackground : MonoBehaviour
{
    void Start()
    {
        Camera mainCam = Camera.main;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (mainCam == null || sr.sprite == null) 
        {
            Debug.LogError("[정적 배경] SpriteRenderer에 이미지가 없습니다! 이미지를 먼저 넣어주세요.");
            return;
        }

        // 화면 크기에 맞게 스케일 조절
        float worldScreenHeight = mainCam.orthographicSize * 2.0f;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;
        
        float spriteWidth = sr.sprite.bounds.size.x;
        float spriteHeight = sr.sprite.bounds.size.y;

        // 화면을 꽉 채우도록 스케일 계산 (가로/세로 비율 유지하면서 더 큰 쪽에 맞춤)
        float scaleX = worldScreenWidth / spriteWidth;
        float scaleY = worldScreenHeight / spriteHeight;
        float finalScale = Mathf.Max(scaleX, scaleY);
        
        transform.localScale = new Vector3(finalScale, finalScale, 1);
        
        // 카메라 중앙에 배치하되, 배경이므로 다른 오브젝트를 가리지 않게 맨 뒤로(Z축) 보냄
        transform.position = new Vector3(mainCam.transform.position.x, mainCam.transform.position.y, 50f);
        
        // SortingOrder도 확실하게 맨 뒤로
        sr.sortingOrder = -100;
    }
}

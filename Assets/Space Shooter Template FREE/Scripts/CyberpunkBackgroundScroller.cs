using UnityEngine;

/// <summary>
/// 배경 이미지를 무한 스크롤해 주는 스크립트입니다.
/// 빈 오브젝트가 아니라 **배경 이미지(SpriteRenderer)가 달린 오브젝트**에 직접 붙여주세요!
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CyberpunkBackgroundScroller : MonoBehaviour
{
    [Header("스크롤 설정")]
    [Tooltip("배경이 내려오는 속도")]
    public float scrollSpeed = 3.0f; 

    private SpriteRenderer mainRenderer;
    private Transform buddyTransform;
    private float spriteHeight;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        mainRenderer = GetComponent<SpriteRenderer>();

        if (mainRenderer.sprite == null)
        {
            Debug.LogError("[배경 스크롤러] SpriteRenderer에 이미지가 비어있습니다! 이미지를 먼저 넣어주세요.");
            return;
        }

        // 1. 화면 가로 크기에 맞춰서 본체 스케일 자동 조절
        ScaleToFitScreen();

        // 2. 텍스처 실제 세로 길이 계산
        spriteHeight = mainRenderer.sprite.bounds.size.y * transform.localScale.y;

        // 3. 무한 스크롤을 위해 본체 바로 위에 똑같은 분신(Buddy)을 하나 복제해서 얹어둠
        GameObject buddy = new GameObject(gameObject.name + "_Buddy");
        buddy.transform.SetParent(this.transform.parent); // 본체와 같은 폴더/위치에 두기
        
        SpriteRenderer buddyRenderer = buddy.AddComponent<SpriteRenderer>();
        buddyRenderer.sprite = mainRenderer.sprite;
        buddyRenderer.color = mainRenderer.color;
        buddyRenderer.sortingOrder = mainRenderer.sortingOrder;
        buddyRenderer.sortingLayerID = mainRenderer.sortingLayerID;

        // 분신의 크기와 위치 맞추기
        buddy.transform.localScale = this.transform.localScale;
        buddy.transform.position = this.transform.position + new Vector3(0, spriteHeight, 0);
        
        buddyTransform = buddy.transform;
    }

    void Update()
    {
        if (buddyTransform == null) return;

        // 본체와 분신을 동시에 아래로 이동
        transform.Translate(Vector3.down * scrollSpeed * Time.deltaTime);
        buddyTransform.Translate(Vector3.down * scrollSpeed * Time.deltaTime);

        // 화면 아래쪽 한계선 계산 (카메라 기준)
        float bottomLimit = mainCam.transform.position.y - spriteHeight;

        // 본체가 한계선을 넘어가면, 분신의 바로 위쪽으로 텔레포트
        if (transform.position.y <= bottomLimit)
        {
            transform.position = new Vector3(transform.position.x, buddyTransform.position.y + spriteHeight, transform.position.z);
        }
        // 분신이 한계선을 넘어가면, 본체의 바로 위쪽으로 텔레포트
        else if (buddyTransform.position.y <= bottomLimit)
        {
            buddyTransform.position = new Vector3(buddyTransform.position.x, transform.position.y + spriteHeight, buddyTransform.position.z);
        }
    }

    void ScaleToFitScreen()
    {
        if (mainCam == null) return;

        float worldScreenHeight = mainCam.orthographicSize * 2.0f;
        float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

        float spriteWidth = mainRenderer.sprite.bounds.size.x;
        
        // 가로 길이를 화면에 꽉 차게 맞춤
        float scale = worldScreenWidth / spriteWidth;
        transform.localScale = new Vector3(scale, scale, 1);
    }
}

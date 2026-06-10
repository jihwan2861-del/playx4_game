using UnityEngine;

public class ClickCursorChanger : MonoBehaviour
{
    // 싱글톤 인스턴스 (어디서든 접근 가능)
    public static ClickCursorChanger instance;

    [Header("Cursor Textures")]
    public Texture2D normalCursor;
    public Texture2D clickedCursor;
    public Texture2D hpHoverCursor;

    [Header("Settings")]
    public Vector2 hotSpot = Vector2.zero;

    private Camera mainCamera;

    void Awake()
    {
        // 씬을 이동해도 마우스 매니저가 파괴되지 않고 계속 유지되도록 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 이 오브젝트를 영구 보존
        }
        else
        {
            Destroy(gameObject); // 이미 마우스 매니저가 다른 씬에서 넘어왔다면, 중복 생성을 막기 위해 스스로 파괴
            return;
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        SetCursorTexture(normalCursor);
    }

    void Update()
    {
        // 매 씬마다 메인 카메라가 바뀔 수 있으므로 갱신해 줍니다.
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        bool isHoveringHp = false;

        if (mainCamera != null)
        {
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (hit.collider != null)
            {
                if (CheckIfObjectHasHP(hit.collider.gameObject))
                {
                    isHoveringHp = true;
                }
            }
        }

        Texture2D targetTexture = normalCursor;

        if (Input.GetMouseButton(0))
        {
            targetTexture = clickedCursor;
        }
        else if (isHoveringHp)
        {
            targetTexture = hpHoverCursor;
        }
        else
        {
            targetTexture = normalCursor;
        }

        SetCursorTexture(targetTexture);
    }

    private bool CheckIfObjectHasHP(GameObject obj)
    {
        if (obj.GetComponent<Enemy>() != null) return true;
        if (obj.GetComponent<Player>() != null) return true;
        if (obj.GetComponent("TutorialDummy") != null) return true;
        return false;
    }

    private Texture2D lastSetTexture = null;

    private void SetCursorTexture(Texture2D tex)
    {
        if (tex == null) return;

        // 🚀 성능 최적화: 이전 프레임과 동일한 텍스처라면 중복 세팅을 건너뜁니다.
        if (lastSetTexture == tex) return;

        Cursor.SetCursor(tex, hotSpot, CursorMode.Auto);
        lastSetTexture = tex;
    }
}
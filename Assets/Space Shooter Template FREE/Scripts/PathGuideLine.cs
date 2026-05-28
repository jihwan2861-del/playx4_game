using UnityEngine;

[System.Serializable]
public class Vector3UnityEventForPath : UnityEngine.Events.UnityEvent<Vector3> { }

/// <summary>
/// 유니티 LineRenderer 컴포넌트를 기반으로 작동하며,
/// 플레이어 기체로부터 현재 가야 할 목표(더미 봇, 워프 포인트 등)까지 아름다운 네온 가이드 유도선을 실시간으로 그려주는 스크립트입니다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PathGuideLine : MonoBehaviour
{
    public static PathGuideLine instance;

    [Header("=== 대상 설정 ===")]
    [Tooltip("추적할 대상 Transform")]
    public Transform targetTransform;
    [Tooltip("추적할 고정 좌표 (Transform이 없을 때 사용)")]
    public Vector3 targetPosition;
    [Tooltip("목표물이 Transform인지 단순 좌표인지 여부")]
    public bool useTransformTarget = true;

    [Header("=== 선 두께 및 색상 설정 ===")]
    public float startWidth = 0.15f;
    public float endWidth = 0.05f;
    public Color startColor = new Color(0f, 0.8f, 1f, 0.8f); // 밝은 네온 하늘색
    public Color endColor = new Color(0f, 0.8f, 1f, 0.1f);  // 소멸 지점은 페이드아웃

    [Header("=== 흐름 효과 애니메이션 ===")]
    [Tooltip("선에 들어갈 화살표나 점선 텍스처가 흐르는 속도 (음수면 목적지 방향으로 전진)")]
    public float scrollSpeed = -2.0f;

    private LineRenderer lineRenderer;
    private Transform playerTransform;
    private Material lineMaterial;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        // 씬 내부의 플레이어 인스턴스 탐색 및 캐싱
        if (PlayerMoving.instance != null)
        {
            playerTransform = PlayerMoving.instance.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        SetupDefaultLineRenderer();
    }

    /// <summary>
    /// 에디터에서 수동으로 채우지 않아도 코드로 아름다운 2D 라인을 자동 구성합니다.
    /// </summary>
    private void SetupDefaultLineRenderer()
    {
        if (lineRenderer == null) return;

        lineRenderer.sortingLayerName = "Default";
        lineRenderer.sortingOrder = 55; // 다른 레이어들 위에 안전 노출되도록 조정

        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;

        // 곡선 꺾임 및 캡을 둥글고 부드럽게 세팅
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;

        // 기본 Additive 셰이더 적용
        Shader defaultShader = Shader.Find("Sprites/Default") ?? Shader.Find("Legacy Shaders/Particles/Additive");
        if (defaultShader != null)
        {
            lineMaterial = new Material(defaultShader);
            lineRenderer.material = lineMaterial;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            // 실시간 플레이어 재탐색 (씬 전환 대응)
            if (PlayerMoving.instance != null)
            {
                playerTransform = PlayerMoving.instance.transform;
            }
            else
            {
                lineRenderer.enabled = false;
                return;
            }
        }

        Vector3 destination = GetDestinationPosition();

        // 추적할 대상이 유효하지 않은 경우 유도선을 비활성화
        if (useTransformTarget && targetTransform == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        // 라인 렌더러 좌표 업데이트
        lineRenderer.SetPosition(0, playerTransform.position);
        lineRenderer.SetPosition(1, destination);

        // 점선 텍스처를 스크롤하여 흐르는 전류 같은 효과 연출
        if (lineMaterial != null && scrollSpeed != 0)
        {
            float offset = Time.time * scrollSpeed;
            lineMaterial.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }
    }

    private Vector3 GetDestinationPosition()
    {
        if (useTransformTarget)
        {
            return targetTransform != null ? targetTransform.position : Vector3.zero;
        }
        return targetPosition;
    }

    // ── 외부 컴포넌트 호출용 가이드라인 API ──────────────────────────────────────

    public void SetTarget(Transform newTarget)
    {
        targetTransform = newTarget;
        useTransformTarget = true;
        gameObject.SetActive(true);
        if (lineRenderer != null) lineRenderer.enabled = true;
    }

    public void SetTarget(Vector3 staticPos)
    {
        targetPosition = staticPos;
        useTransformTarget = false;
        gameObject.SetActive(true);
        if (lineRenderer != null) lineRenderer.enabled = true;
    }

    public void ClearTarget()
    {
        targetTransform = null;
        if (lineRenderer != null) lineRenderer.enabled = false;
    }
}

using UnityEngine;

/// <summary>
/// 보스가 해킹 모드일 때 플레이어와 보스 사이에 전기처럼 꼬불거리는 흰색 계열의 연결 광선(LineRenderer)을 그리는 이펙트 스크립트입니다.
/// </summary>
public class HackingConnectionLine : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private BossPatternController bossPC;
    private PlayerMoving player;

    [Header("시각 효과 설정")]
    [Tooltip("라인을 구성할 정점(세그먼트)의 개수. 많을수록 부드러워집니다.")]
    public int segments = 25;
    [Tooltip("꼬불거리는 파동의 세기(폭)")]
    public float waveAmplitude = 0.5f;
    [Tooltip("찌릿찌릿 움직이는 주파수(속도)")]
    public float waveFrequency = 22f;
    [Tooltip("노이즈 패턴의 간격 스케일")]
    public float noiseScale = 0.4f;

    [Tooltip("연결선의 두께 (기본값: 0.15f, 이전 값: 0.08f)")]
    public float lineWidth = 0.15f;

    private void Start()
    {
        bossPC = GetComponent<BossPatternController>();
        player = PlayerMoving.instance;

        // LineRenderer 컴포넌트 자동 추가 및 세팅
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // 2D 환경에서 가장 안전한 Sprites/Default 셰이더로 머티리얼 구성 (핑크 에러 방지)
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            lineRenderer.material = new Material(spriteShader);
        }
        else
        {
            // 폴백으로 스프라이트용 기본 마테리얼 찾기 시도
            lineRenderer.material = Canvas.GetDefaultCanvasMaterial();
        }

        // 광선 색상 설정: 찌릿거리는 흰색 ~ 라이트 블루톤의 그라데이션
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = new Color(0.9f, 0.95f, 1f, 0.85f);
        
        lineRenderer.positionCount = segments;
        lineRenderer.sortingOrder = 20; // 다른 스프라이트보다 앞에 렌더링되도록 소팅 오더 확보
        lineRenderer.enabled = false;
    }

    private void Update()
    {
        // 런타임에 싱글톤 인스턴스가 갱신되었을 수 있으므로 안전 보강
        if (player == null)
        {
            player = PlayerMoving.instance;
        }

        // 보스의 해킹 상태 여부 및 플레이어와 보스의 활성화 여부에 맞춰 선 켜고 끄기
        if (bossPC != null && bossPC.isHacking && player != null)
        {
            if (!lineRenderer.enabled)
            {
                lineRenderer.enabled = true;
            }

            // 실시간 두께 변경 사항을 적용
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            DrawHackingLine();
        }
        else
        {
            if (lineRenderer != null && lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// 플레이어와 보스 사이의 거리를 계산하여 2D 법선 벡터 상에 찌릿찌릿 흔들리는 꼬불이 라인을 그립니다.
    /// </summary>
    private void DrawHackingLine()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = player.transform.position;

        // 두 타겟 간의 벡터 및 정방향 단위 벡터 계산
        Vector3 direction = endPos - startPos;
        Vector3 dirNormalized = direction.normalized;

        // 진행 방향의 2D 수직(법선) 벡터 산출 (z축 0 고정 회전)
        Vector3 perpendicular = new Vector3(-dirNormalized.y, dirNormalized.x, 0f);

        lineRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);

            // 시작점(보스)과 끝점(플레이어)은 정확한 지점에 딱 붙도록 오프셋 제외
            if (i == 0)
            {
                lineRenderer.SetPosition(i, startPos);
                continue;
            }
            if (i == segments - 1)
            {
                lineRenderer.SetPosition(i, endPos);
                continue;
            }

            // 아치형 곡선 감쇄 팩터 계산 (양 끝은 0, 중앙부는 1에 가깝게)
            float fadeFactor = Mathf.Sin(t * Mathf.PI);

            // Perlin Noise와 Sin 파동을 다이나믹하게 조합하여 인공미 없는 야생 전기 스파크 재현
            float timeScale = Time.time * waveFrequency;
            float noiseInput = i * noiseScale + timeScale;
            float waveValue = (Mathf.PerlinNoise(noiseInput, 0f) * 2f - 1f) * waveAmplitude * fadeFactor;

            // 추가적인 빠른 고주파 사인 미세 파동을 가미해 찌르르르 떨리는 디테일 빔 완성
            waveValue += Mathf.Sin(timeScale * 1.6f + t * Mathf.PI * 4f) * 0.15f * waveAmplitude * fadeFactor;

            // 최종 보간 지점에 수직 파동 오프셋을 더해 정점 위치 결정
            Vector3 segmentPos = Vector3.Lerp(startPos, endPos, t) + perpendicular * waveValue;
            lineRenderer.SetPosition(i, segmentPos);
        }
    }
}

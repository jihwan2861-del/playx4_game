using UnityEngine;

/// <summary>
/// 보스에게 부착하여 마우스 호버링을 통한 '해킹' 기능을 수행하는 스크립트입니다.
/// </summary>
public class BossHacking : MonoBehaviour
{
    [Header("해킹 설정")]
    [Tooltip("해킹 시 보여줄 이펙트나 색상 변경을 위한 렌더러")]
    public SpriteRenderer bossRenderer;
    public Color hackingColor = new Color(0.5f, 1f, 1f, 1f); // 해킹 중일 때의 민트색 광채
    private Color originalColor;

    [Header("거리 기반 해킹 설정")]
    [Tooltip("이 반경(원) 안에 플레이어가 들어오면 자동으로 해킹이 진행됩니다!")]
    public float hackingRadius = 4.0f; 

    [Header("커스텀 해킹존 이미지 (선택)")]
    [Tooltip("직접 그리신 해킹존 이미지가 달린 오브젝트(SpriteRenderer)를 여기에 넣어주세요!")]
    public SpriteRenderer customHackingZone;

    private bool isHackingActive = false;
    private Color originalZoneColor; // 해킹존 원본 색상 저장용

    void Start()
    {
        if (bossRenderer == null) bossRenderer = GetComponent<SpriteRenderer>();
        if (bossRenderer != null) originalColor = bossRenderer.color;

        // 시작할 때 커스텀 해킹존의 원래 색상(투명도 등)을 기억해둡니다.
        if (customHackingZone != null)
        {
            originalZoneColor = customHackingZone.color;
        }
    }

    void Update()
    {
        if (Player.instance == null) return;
        BossPatternController bossController = GetComponent<BossPatternController>();

        // 보스와 플레이어 사이의 거리 계산
        float distanceToPlayer = Vector3.Distance(transform.position, Player.instance.transform.position);

        // 플레이어가 해킹 반경(원) 안에 들어왔을 때
        if (distanceToPlayer <= hackingRadius)
        {
            if (!isHackingActive)
            {
                isHackingActive = true;
                if (bossController != null) bossController.isHacking = true;
                
                if (bossRenderer != null) bossRenderer.color = hackingColor;
                
                // 직접 그리신 해킹존 이미지를 해킹 중인 색상으로 빛나게 변경!
                if (customHackingZone != null)
                {
                    customHackingZone.color = hackingColor; // 진한 민트색으로 번쩍임
                }
            }
        }
        // 플레이어가 반경 밖으로 나갔을 때
        else
        {
            if (isHackingActive)
            {
                isHackingActive = false;
                if (bossController != null) bossController.isHacking = false;
                
                if (bossRenderer != null) bossRenderer.color = originalColor;

                // 해킹 중단 시 커스텀 해킹존을 다시 유저가 세팅한 원본 색상으로 복구
                if (customHackingZone != null)
                {
                    customHackingZone.color = originalZoneColor;
                }
            }
        }
    }

    void OnDisable()
    {
        BossPatternController bossController = GetComponent<BossPatternController>();
        if (bossController != null && isHackingActive)
        {
            bossController.isHacking = false;
            isHackingActive = false;
        }
    }

    // 유니티 에디터에서 해킹 반경(원)을 시각적으로 보여주는 기능
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // 반투명한 민트색
        Gizmos.DrawWireSphere(transform.position, hackingRadius);
    }
}

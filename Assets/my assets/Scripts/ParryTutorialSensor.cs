using UnityEngine;

/// <summary>
/// 플레이어의 자식 오브젝트에 부착되어 날아오는 적의 총알을 감지하고,
/// 최초 1회 극적인 슬로우모션과 나레이션 가이드를 제공하는 물리 트리거 스크립트입니다.
/// </summary>
public class ParryTutorialSensor : MonoBehaviour
{
    private bool hasTriggered = false;
    private bool isWaitingForInput = false;

    void Start()
    {
        // IsTrigger 검증 예외 처리
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"⚠️ [{gameObject.name}] Collider2D가 Trigger로 설정되어 있지 않아 자동으로 Trigger로 변경했습니다.");
        }
    }

    void Update()
    {
        // 슬로우모션 연출 중 Spacebar(패링 키) 입력 감지 대기
        if (isWaitingForInput && Input.GetKeyDown(KeyCode.Space))
        {
            isWaitingForInput = false;
            
            // 시간 흐름 정상화
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            // 나레이션 UI 닫기
            if (NarrationUI.instance != null)
            {
                NarrationUI.instance.Close();
            }

            Debug.Log("🛡️ [ParryTutorialSensor] 패링 가이드 연출 성공 완료. 센서 오브젝트를 파괴합니다.");

            // 이 센서 오브젝트 자체를 파괴하여 1회성 연출을 완벽히 마칩니다.
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered) return;

        // 충돌한 물체가 적의 투사체(Projectile)인지 판별
        Projectile proj = collision.GetComponent<Projectile>();
        if (proj == null)
        {
            proj = collision.GetComponentInParent<Projectile>();
        }

        if (proj != null && proj.enemyBullet && collision.gameObject.activeInHierarchy)
        {
            TriggerTutorial();
        }
    }

    private void TriggerTutorial()
    {
        hasTriggered = true;
        isWaitingForInput = true;

        Debug.Log("⏳ [ParryTutorialSensor] 총알 진입 감지! 극적 슬로우모션 및 나레이션 가이드를 시작합니다.");

        // 1. 극적인 슬로우 모션 (시간을 5% 속도로 낮춤)
        Time.timeScale = 0.05f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 2. 상단 나레이션으로 가이드 출력
        if (NarrationUI.instance != null)
        {
            NarrationUI.instance.Show("스페이스바를 눌러서 패링하세요.");
        }
    }
}

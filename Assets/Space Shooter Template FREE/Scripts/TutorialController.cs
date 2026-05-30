using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// '1st scene' 시네마틱 오프닝 튜토리얼을 관리하는 마스터 컨트롤러입니다.
/// 절차적 드로잉 코드를 완전히 배제하고, 개발자가 직접 배치한 게임 오브젝트 및 프리패브를
/// 인스펙터 슬롯에서 직접 매핑하여 제어하는 정석적인 유니티 형태로 설계되었습니다.
/// </summary>
public class TutorialController : MonoBehaviour
{
    public static TutorialController instance;

    [Header("=== 씬 이동 설정 ===")]
    [Tooltip("튜토리얼 완료 후 이동할 메인 기지 씬 이름")]
    public string lobbySceneName = "Hub_Scene";

    [Header("=== 플레이어 비주얼 참조 (씬에서 드래그) ===")]
    [Tooltip("플레이어 루트 오브젝트 (PlayerMoving 스크립트가 부착된 본체)")]
    public PlayerMoving player;
    [Tooltip("플레이어 오토바이 탑승 비주얼 (활성화/비활성화)")]
    public GameObject playerRidingVisual;
    [Tooltip("플레이어 보행 상태 비주얼 (활성화/비활성화)")]
    public GameObject playerWalkingVisual;
    [Tooltip("등 뒤에 짊어진 수거된 기체 비주얼 (활성화/비활성화)")]
    public GameObject carriedMechVisual;

    [Header("=== 월드 오브젝트 참조 (씬에서 드래그) ===")]
    [Tooltip("폐허 입구에 미리 주차되어 세워져 있을 오토바이 오브젝트")]
    public GameObject parkedMotorcycle;
    [Tooltip("구석에 방치되어 쓰러져 잠들어 있을 고성능 기체 오브젝트")]
    public GameObject deactivatedMech;
    [Tooltip("고장난 적 사이보그 오브젝트 (TutorialDummy 장착)")]
    public TutorialDummy cyborgDummy;

    [Header("=== 사격 프리팹 참조 (폴더에서 드래그) ===")]
    [Tooltip("사이보그가 패링용으로 발사할 총알 프리팹")]
    public GameObject bulletPrefab;

    [Header("=== 연출 판정 속성 ===")]
    [Tooltip("오토바이 복귀 완료로 인정될 복귀 거리 반경")]
    public float bikeReturnRadius = 2.5f;

    // 시네마틱 페이즈 관리
    // 1: 오토바이 질주 시작
    // 2: 오토바이 하차 완료 및 보행 탐색 시작
    // 3: 적 사이보그와 대치 및 단발 탄막 대치
    // 4: 패링 성공 및 안도 독백 연출 완료 ➔ 폭주 대기
    // 5: 적 폭주 ➔ 플레이어 방전 피격 넉백 ➔ 기체 발견 대화
    // 6: 기체 수거 완료 ➔ 오토바이 복귀 유도 상태
    // 7: 오토바이 재탑승 탈출 연출 중
    [HideInInspector] public int currentPhase = 1;

    private bool playerHasMech = false;
    private bool cyborgEncounterTriggered = false;
    private bool mechFoundTriggered = false;
    private List<GameObject> activeBullets = new List<GameObject>();
    private CameraFollow mainCameraFollow;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 메인 카메라의 카메라 폴로우 컴포넌트 자동 캐싱
        if (Camera.main != null)
        {
            mainCameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        // 초기 비주얼 셋업: 플레이어는 오토바이 탑승 비주얼만 활성화된 상태로 시작
        SetRidingMode(true);
        if (carriedMechVisual != null) carriedMechVisual.SetActive(false);
        if (parkedMotorcycle != null) parkedMotorcycle.SetActive(false);

        // 주인공 오프닝 첫 대사 말풍선 출력
        if (player != null)
        {
            SpeechBubble.Create(player.gameObject, "여기도 꽝인가 멀쩡한 부품 비슷한것도 안보이네;;", new Color(0.1f, 0.9f, 0.8f));
        }
    }

    private void Update()
    {
        // 3단계 감시: 패링 훈련 중 날아오는 총알 실시간 감시 (슬로우모션 트리거)
        if (currentPhase == 3)
        {
            CheckBulletProximityForSlowMo();
        }

        // 4단계 감시: 연속 패링 도중 과열/에너지 감시 (플레이어가 패링 시 에너지 감소 확인)
        if (currentPhase == 4 && player != null)
        {
            if (player.currentEnergy < player.parryEnergyCost)
            {
                currentPhase = 5;
                StartCoroutine(PlayerEnergyExhaustedRoutine());
            }
        }

        // 6단계 감시: 기체 수거 후 다시 오토바이로 돌아왔는지 체크 (인스펙터 거리 기반 판정)
        if (currentPhase == 6 && player != null && parkedMotorcycle != null && playerHasMech)
        {
            float distToBike = Vector3.Distance(player.transform.position, parkedMotorcycle.transform.position);
            if (distToBike <= bikeReturnRadius)
            {
                TriggerRidingEscape();
            }
        }
    }

    // ==========================================
    // 🎯 에디터 트리거 연동용 퍼블릭 인터페이스 (Triggers)
    // ==========================================

    /// <summary>
    /// 플레이어가 오토바이 주차 영역에 도달했을 때 하차를 진행합니다.
    /// (Trigger 스크립트 등에서 호출)
    /// </summary>
    public void OnPlayerArrived()
    {
        if (currentPhase != 1) return;
        currentPhase = 2;
        Debug.Log("🎯 [시네마틱] 플레이어 주차 지점 도착 - 보행 모드 전환");

        // 1. 플레이어 가속을 멈추고 씬에 주차용 오토바이 오브젝트 소환
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        if (parkedMotorcycle != null && player != null)
        {
            parkedMotorcycle.transform.position = player.transform.position;
            parkedMotorcycle.SetActive(true);
        }

        // 2. 오토바이 비주얼 끄고 보행용 자식 비주얼 활성화
        SetRidingMode(false);

        // 3. 하차 독백 말풍선 출력
        SpeechBubble.Create(player.gameObject, "여기서부터는 걸어서 진입해야겠군... 쓸만한 부품이 남아있는지 찾아보자.", new Color(0.1f, 0.9f, 0.8f));
    }

    /// <summary>
    /// 적 사이보그 시야 영역에 도달했을 때 대치 컷씬을 진행합니다.
    /// (Trigger 스크립트 등에서 호출)
    /// </summary>
    public void OnCyborgEncountered()
    {
        if (currentPhase != 2 || cyborgEncounterTriggered) return;
        cyborgEncounterTriggered = true;
        
        StartCoroutine(CyborgEncounterRoutine());
    }

    /// <summary>
    /// 쓰러진 고성능 기체(페렐만) 근처에 플레이어가 도달했을 때 발견 컷씬을 진행합니다.
    /// (Trigger 스크립트 등에서 호출)
    /// </summary>
    public void OnMechFound()
    {
        if (currentPhase != 5 || mechFoundTriggered) return;
        mechFoundTriggered = true;
        
        StartCoroutine(MechDiscoveryRoutine());
    }

    // ==========================================
    // 🎬 세부 시네마틱 연출 코루틴 (Cinematics)
    // ==========================================

    /// <summary>
    /// 적 사이보그 조우 대화 및 동적 카메라 패닝 연출
    /// </summary>
    private IEnumerator CyborgEncounterRoutine()
    {
        currentPhase = 3;

        // 1. 대화 시 스토리 집중을 위해 캐릭터 조작 일시 정지
        player.enabled = false;
        Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
        if (prb != null) prb.velocity = Vector2.zero;

        yield return new WaitForSeconds(0.4f);

        // 2. [카메라 포커싱 전환] 카메라 시점을 사이보그 쪽으로 글라이딩!
        if (mainCameraFollow != null && cyborgDummy != null)
        {
            mainCameraFollow.target = cyborgDummy.transform;
        }

        yield return new WaitForSeconds(0.8f);

        // 3. 사이보그 위태로운 경고 말풍선 출력
        bool dialFinished = false;
        if (cyborgDummy != null)
        {
            SpeechBubble.Create(cyborgDummy.gameObject, "페@#*를 보...호..하..라", Color.red, () => dialFinished = true);
        }
        else
        {
            dialFinished = true;
        }

        yield return new WaitUntil(() => dialFinished);
        yield return new WaitForSeconds(0.4f);

        // 4. [카메라 포커싱 복귀] 시점을 플레이어 쪽으로 되돌림!
        if (mainCameraFollow != null && player != null)
        {
            mainCameraFollow.target = player.transform;
        }

        yield return new WaitForSeconds(0.8f);

        // 5. 주인공 당황 말풍선 출력
        dialFinished = false;
        SpeechBubble.Create(player.gameObject, "ㅁ..뭐.뭐야!??", new Color(0.1f, 0.9f, 0.8f), () => dialFinished = true);

        yield return new WaitUntil(() => dialFinished);
        yield return new WaitForSeconds(0.3f);

        // 6. 조작 해제 및 훈련용 단발 사격 개시
        player.enabled = true;

        if (cyborgDummy != null && player != null && bulletPrefab != null)
        {
            Vector3 shootDir = (player.transform.position - cyborgDummy.transform.position).normalized;
            SpawnTutorialBullet(cyborgDummy.transform.position, shootDir, 5.5f);
        }
    }

    /// <summary>
    /// 단발 사격 총알이 근접했을 때 화면을 감속시키고 패링 유도 가이드를 켭니다.
    /// </summary>
    private void CheckBulletProximityForSlowMo()
    {
        if (player == null) return;

        activeBullets.RemoveAll(b => b == null || !b.activeInHierarchy);

        foreach (var bullet in activeBullets)
        {
            float dist = Vector3.Distance(bullet.transform.position, player.transform.position);
            if (dist <= 2.2f && Time.timeScale > 0.2f)
            {
                // 극적인 화면 슬로우 모션 (불릿 타임)
                Time.timeScale = 0.15f;

                // 패링 화면 가이드 텍스트 출력
                if (player.dashTextObj != null)
                {
                    player.dashTextObj.SetActive(true);
                    var text = player.dashTextObj.GetComponent<UnityEngine.UI.Text>();
                    if (text != null) text.text = "[Space] 키를 눌러 타이밍에 맞춰 패링하세요!";
                }
                break;
            }
        }

        // 플레이어가 완벽한 타이밍에 패링을 성공하여 총알이 제거되었을 때 4단계로 전환
        if (activeBullets.Count == 0 && Time.timeScale < 0.3f)
        {
            Time.timeScale = 1.0f; // 프레임 속도 즉각 회복
            
            if (player.dashTextObj != null)
            {
                player.dashTextObj.SetActive(false);
            }

            TriggerPhase4Success();
        }
    }

    /// <summary>
    /// 패링 성공 후 안도 독백 연출 진행
    /// </summary>
    private void TriggerPhase4Success()
    {
        currentPhase = 4;
        Debug.Log("🎯 4단계 진입: 패링 성공 완료 - 안도 대사 출력");

        // 대화 진행 중 조작 잠금
        player.enabled = false;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        SpeechBubble.Create(player.gameObject, "휴... 해치웠나? 큰일 날 뻔했네.. 그래도..오늘 하루 건졌다 헤헤....!?", new Color(0.1f, 0.9f, 0.8f), () => {
            // 조작 복구 및 적의 폭주 난사 탄막 연출 기동
            player.enabled = true;
            StartCoroutine(CyborgOverloadRoutine());
        });
    }

    /// <summary>
    /// 사이보그가 폭주 상태에 빠져 무차별 난사를 뿜는 루틴
    /// </summary>
    private IEnumerator CyborgOverloadRoutine()
    {
        if (cyborgDummy == null) yield break;

        yield return new WaitForSeconds(0.4f);

        // 사이보그 폭주 대사 출력
        SpeechBubble.Create(cyborgDummy.gameObject, "페@#*를 보...호..하..라", Color.red);

        // 연속 패링 연타를 강제 유도하기 위해 에너지 재생 차단 및 소모비용 증가
        player.baseEnergyRegen = 0f;
        player.energyRegenBonus = 0f;
        player.parryEnergyCost = 28f; // 빠른 방전 유도

        // 플레이어 에너지가 방전될 때까지 무차별 분산 탄막 스폰 루프
        while (currentPhase == 4)
        {
            if (player != null && bulletPrefab != null)
            {
                Vector3 shootDir = (player.transform.position - cyborgDummy.transform.position).normalized;
                shootDir = Quaternion.Euler(0, 0, Random.Range(-18f, 18f)) * shootDir; // 분산 탄막
                SpawnTutorialBullet(cyborgDummy.transform.position, shootDir, 7.5f);
            }
            yield return new WaitForSeconds(0.24f);
        }
    }

    /// <summary>
    /// 에너지 과열 방전 ➔ 피격 넉백 ➔ 기체 옆 기절 시퀀스
    /// </summary>
    private IEnumerator PlayerEnergyExhaustedRoutine()
    {
        Debug.Log("🎯 5단계 진입: 플레이어 에너지 방전 및 강제 넉백 피격 연출 기동");

        // 1. 방전/과열 말풍선 강제 출력
        SpeechBubble.Create(player.gameObject, "너무 많이써서 과열화 되버렸어!!!", Color.yellow);

        // 2. 조작권 완전 잠금 및 무적 피격 판정 비활성화
        player.isParryActive = false;
        player.enabled = false;

        // 폭주 탄막 전량 영구 파괴
        activeBullets.RemoveAll(b => b == null || !b.activeInHierarchy);
        foreach (var b in activeBullets) Destroy(b);
        activeBullets.Clear();

        yield return new WaitForSeconds(0.4f);

        // 3. 넉백 유도용 타격탄 1발 사격
        if (cyborgDummy != null && bulletPrefab != null)
        {
            Vector3 shootDir = (player.transform.position - cyborgDummy.transform.position).normalized;
            SpawnTutorialBullet(cyborgDummy.transform.position, shootDir, 12.0f);
        }

        // 투사체가 플레이어에 도달하여 물리적 충돌이 날 때까지 프레임 대기
        bool hitTriggered = false;
        while (!hitTriggered)
        {
            activeBullets.RemoveAll(b => b == null || !b.activeInHierarchy);
            if (activeBullets.Count > 0)
            {
                float dist = Vector3.Distance(activeBullets[0].transform.position, player.transform.position);
                if (dist <= 0.8f)
                {
                    Destroy(activeBullets[0]);
                    hitTriggered = true;
                }
            }
            else
            {
                hitTriggered = true;
            }
            yield return null;
        }

        // 4. [피격 연출] 강렬한 물리 힘을 가해 우측으로 미끄러져 날아가게 함
        Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            prb.velocity = new Vector2(1f, 0.15f).normalized * 24f;
        }

        // 사이보그는 정지 상태로 말풍선 '.....' 출력
        if (cyborgDummy != null)
        {
            SpeechBubble.Create(cyborgDummy.gameObject, ".....", Color.grey);
        }

        yield return new WaitForSeconds(0.4f);
        if (prb != null) prb.velocity = Vector2.zero;

        // 플레이어를 최첨단 기체 DeactivatedMech 바로 옆에 기절한 형태로 포지셔닝 고정
        if (deactivatedMech != null)
        {
            player.transform.position = new Vector3(deactivatedMech.transform.position.x - 1.8f, deactivatedMech.transform.position.y, 0f);
        }

        yield return new WaitForSeconds(0.6f);

        // 5. [카메라 포커싱 전환] 카메라 시점을 쓰러져 있는 기체(DeactivatedMech) 방향으로 포커싱!
        if (mainCameraFollow != null && deactivatedMech != null)
        {
            mainCameraFollow.target = deactivatedMech.transform;
        }

        yield return new WaitForSeconds(0.8f);

        // 6. 주인공 머리맡 순차적 기절 및 기체 발견 독백
        bool nextDial = false;
        SpeechBubble.Create(player.gameObject, "으윽... 역시 이런 임시 고철 슈트로는 탄막을 감당할 수 없나...", new Color(0.1f, 0.9f, 0.8f), () => nextDial = true);
        yield return new WaitUntil(() => nextDial);
        yield return new WaitForSeconds(0.6f);

        nextDial = false;
        SpeechBubble.Create(player.gameObject, "응...? 이건... 엄청난 부품들이잖아...!", new Color(0.1f, 0.9f, 0.8f), () => nextDial = true);
        yield return new WaitUntil(() => nextDial);
        
        // 트리거를 걸 수 있도록 상태를 풀어둠
        OnMechFound();
    }

    /// <summary>
    /// 기체를 짊어지며 수거 완료하는 연출 처리
    /// </summary>
    private IEnumerator MechDiscoveryRoutine()
    {
        yield return new WaitForSeconds(0.4f);

        // 1. 바닥에 쓰러져있던 기체 비활성화 및 주인공 등 뒤 기체 비주얼 활성화
        if (deactivatedMech != null) deactivatedMech.SetActive(false);
        if (carriedMechVisual != null) carriedMechVisual.SetActive(true);

        playerHasMech = true;
        currentPhase = 6;
        Debug.Log("🎯 6단계 진입: 기체 수거 완수 ➔ 오토바이 복귀 상태");

        // 2. [카메라 포커싱 복귀] 카메라 타겟을 다시 주인공으로 리셋!
        if (mainCameraFollow != null && player != null)
        {
            mainCameraFollow.target = player.transform;
        }

        yield return new WaitForSeconds(0.8f);

        // 3. 조작 복구 및 안내 UI 갱신
        player.enabled = true;
        
        // 에너지 회복 및 스펙 초기화 (탈출 대비)
        player.baseEnergyRegen = 2f;
        player.currentEnergy = player.maxEnergy;

        if (player.dashTextObj != null)
        {
            player.dashTextObj.SetActive(true);
            var text = player.dashTextObj.GetComponent<UnityEngine.UI.Text>();
            if (text != null) text.text = "수거한 기체를 짊어지고 왼쪽의 오토바이로 복귀하세요!";
        }
    }

    /// <summary>
    /// 오토바이 근접 시 탑승 및 질주 탈출
    /// </summary>
    private void TriggerRidingEscape()
    {
        currentPhase = 7;
        Debug.Log("🎯 7단계 진입: 오토바이 재탑승 탈출 씬 개시");

        if (player.dashTextObj != null)
        {
            player.dashTextObj.SetActive(false);
        }

        // 주차해 둔 오토바이 및 등 뒤 기체 비주얼 비활성화
        if (parkedMotorcycle != null) parkedMotorcycle.SetActive(false);
        if (carriedMechVisual != null) carriedMechVisual.SetActive(false);

        // 오토바이 탑승 비주얼만 켬
        SetRidingMode(true);

        // 질주 물리 가속도 강제 부여
        player.enabled = false;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = new Vector2(15f, 0f); // 오른쪽으로 쏜살같이 가속
        }

        StartCoroutine(EscapeFadeOutAndReturnRoutine());
    }

    private IEnumerator EscapeFadeOutAndReturnRoutine()
    {
        yield return new WaitForSeconds(1.2f);

        // 씬 전환 세이브 플래그 저장
        if (PlayerDataManager.instance != null)
        {
            PlayerDataManager.instance.tutorialCompleted = true;
            PlayerDataManager.instance.SaveData();
        }
        PlayerPrefs.SetInt("Mission_Tutorial_Completed", 1);
        PlayerPrefs.Save();

        // 허브 씬으로 복귀 로드
        SceneManager.LoadScene(lobbySceneName);
    }

    // ==========================================
    // ⚙️ 하위 보조 유틸리티 메서드 (Helpers)
    // ==========================================

    private void SetRidingMode(bool isRiding)
    {
        if (playerRidingVisual != null) playerRidingVisual.SetActive(isRiding);
        if (playerWalkingVisual != null) playerWalkingVisual.SetActive(!isRiding);

        // 오토바이를 탄 상태에서는 부피 충돌체(BoxCollider)를 대폭 슬림하게 하여 피격 꼬임 방지
        BoxCollider2D col = player.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.size = isRiding ? new Vector2(1.2f, 0.4f) : new Vector2(0.8f, 1.6f);
        }
    }

    private void SpawnTutorialBullet(Vector3 startPosition, Vector3 direction, float speed)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, startPosition, Quaternion.identity);
        
        // 투사체 진행 방향 각도 보정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        // 물리 이동 주입
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb == null) rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.velocity = direction * speed;

        // 적 투사체 트리거 판정
        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj == null) proj = bullet.AddComponent<Projectile>();
        proj.enemyBullet = true;
        proj.damage = 1;

        activeBullets.Add(bullet);
    }
}

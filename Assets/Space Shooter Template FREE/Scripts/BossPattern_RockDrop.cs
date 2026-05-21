using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 2의 아우렐리온 솔 스타일 공전(위성) 돌 패턴 제어기입니다.
/// 보스 본체 주위에 4개의 돌을 90도 격차로 생성하여 지속적인 회전 쉴드 형태로 방어 및 공격을 전개합니다.
/// </summary>
public class BossPattern_RockDrop : MonoBehaviour
{
    [Header("프리팹 설정")]
    [Tooltip("보스가 소환할 돌 프리팹 (Boss2RockProjectile 스크립트 포함)")]
    public GameObject rockPrefab;

    [Header("공전 설정 (아우렐리온 솔 옛날버전 방식)")]
    [Tooltip("돌들이 공전할 기본 반지름 (늘어났다 줄어들었다가 하는 패턴 미사용 시)")]
    public float orbitRadius = 3.2f;
    [Tooltip("공전 속도 (초당 회전 각도)")]
    public float orbitSpeed = 120f;
    [Tooltip("돌 개수 (기본 4개 고정)")]
    public int rockCount = 4;
    [Tooltip("돌 자체 자전 속도")]
    public float rockSpinSpeed = 250f;

    [Header("공전 궤도 수축/팽창 설정")]
    [Tooltip("궤도 수축/팽창 패턴 사용 여부")]
    public bool useRadiusPulse = true;
    [Tooltip("최소 공전 반지름")]
    public float minOrbitRadius = 1.8f;
    [Tooltip("최대 공전 반지름")]
    public float maxOrbitRadius = 4.5f;
    [Tooltip("수축/팽창 속도 (값이 클수록 더 빠르게 팽창/수축 반복)")]
    public float pulseSpeed = 1.2f;

    [Header("시간 및 반복 설정")]
    [Tooltip("이 패턴의 활성화 지속 시간 (초)")]
    public float duration = 7.0f;
    [Tooltip("패턴 쿨타임")]
    public float cooldown = 2.5f;
    [Tooltip("체크 시 이 패턴 오브젝트가 씬에 머물며 쿨타임 후 지속 반복")]
    public bool repeatPattern = false;

    [Header("애니메이션")]
    [Tooltip("보스의 애니메이터 컴포넌트")]
    public Animator bossAnimator;

    private List<GameObject> spawnedRocks = new List<GameObject>();
    private float currentAngle = 0f;
    private BossPatternController bossPC;
    private bool isPatternActive = false;

    private void Start()
    {
        // 보스 패턴 컨트롤러와 애니메이터 찾기
        bossPC = FindObjectOfType<BossPatternController>();
        if (bossAnimator == null && bossPC != null)
        {
            bossAnimator = bossPC.GetComponentInChildren<Animator>();
        }

        StartCoroutine(OrbitPatternRoutine());
    }

    private IEnumerator OrbitPatternRoutine()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Attack");
        }

        // 돌 스폰 및 공전 패턴 개시 (보스가 존재하는 한 영구 공전)
        SpawnRocks();
        isPatternActive = true;

        // 보스가 생존하는 내내 무한 루프로 대기 (파괴 및 소실 관리는 Update()와 OnDestroy()에서 안전 전담)
        while (true)
        {
            yield return null;
        }
    }

    private void Update()
    {
        // 실시간으로 보스가 소실(사망 등)되었는지 감지
        if (bossPC == null)
        {
            CleanUpRocks();
            Destroy(gameObject);
            return;
        }

        // 패턴 구동 중일 때 돌들의 위치를 공전 궤도 좌표로 실시간 계산 및 갱신
        if (isPatternActive && spawnedRocks.Count > 0)
        {
            // 시간 경과에 따른 기본 각도 갱신
            currentAngle += orbitSpeed * Time.deltaTime;
            if (currentAngle >= 360f)
            {
                currentAngle -= 360f;
            }

            // 실시간 수축/팽창 반지름 계산
            float currentRadius = orbitRadius;
            if (useRadiusPulse)
            {
                // Sin 함수를 이용하여 -1 ~ 1 범위를 0 ~ 1 범위로 변환 후 Lerp 적용
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                currentRadius = Mathf.Lerp(minOrbitRadius, maxOrbitRadius, t);
            }

            Vector3 centerPos = bossPC.transform.position;

            for (int i = 0; i < spawnedRocks.Count; i++)
            {
                if (spawnedRocks[i] != null)
                {
                    // 4개의 돌맹이를 정확히 90도 간격으로 균등 분할
                    float offsetAngle = i * (360f / rockCount);
                    float rad = (currentAngle + offsetAngle) * Mathf.Deg2Rad;

                    Vector3 targetPos = new Vector3(
                        centerPos.x + Mathf.Cos(rad) * currentRadius,
                        centerPos.y + Mathf.Sin(rad) * currentRadius,
                        0f
                    );

                    spawnedRocks[i].transform.position = targetPos;
                }
            }
        }
    }

    /// <summary>
    /// 보스 주변 4방향에 돌 프리팹을 인스턴스화합니다.
    /// </summary>
    private void SpawnRocks()
    {
        if (rockPrefab == null) return;

        CleanUpRocks();

        Vector3 centerPos = bossPC != null ? bossPC.transform.position : transform.position;

        for (int i = 0; i < rockCount; i++)
        {
            GameObject rockObj = Instantiate(rockPrefab, centerPos, Quaternion.identity);

            // 돌의 자전 속도 및 공격 데미지 속성 실시간 주입
            Boss2RockProjectile projectileScript = rockObj.GetComponent<Boss2RockProjectile>();
            if (projectileScript != null)
            {
                projectileScript.spinSpeed = rockSpinSpeed;
                projectileScript.damage = 1;
            }

            spawnedRocks.Add(rockObj);
        }
    }

    /// <summary>
    /// 소환되어 공전 중인 모든 돌들을 파괴 및 리스트를 정리합니다.
    /// </summary>
    private void CleanUpRocks()
    {
        if (spawnedRocks != null)
        {
            for (int i = 0; i < spawnedRocks.Count; i++)
            {
                if (spawnedRocks[i] != null)
                {
                    Destroy(spawnedRocks[i]);
                }
            }
            spawnedRocks.Clear();
        }
    }

    private void OnDestroy()
    {
        CleanUpRocks();
    }
}

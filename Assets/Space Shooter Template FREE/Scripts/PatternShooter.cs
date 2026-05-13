using UnityEngine;
using System.Collections;

/// <summary>
/// 적이나 보스가 화려한 탄막(원형, 나선형, 부채꼴)을 쏘게 해주는 스크립트입니다.
/// </summary>
public class PatternShooter : MonoBehaviour
{
    public enum PatternType
    {
        Circle,      // 사방으로 둥글게 퍼지는 탄막
        Spiral,      // 빙글빙글 돌면서 쏘는 나선형 탄막
        ConeTarget   // 플레이어를 향해 부채꼴(샷건) 모양으로 쏘는 탄막
    }

    [Header("기본 설정")]
    public GameObject projectilePrefab; // 발사할 총알 프리팹 (EnemyLaser 등)
    public PatternType patternType = PatternType.Circle;
    public float startDelay = 1f;       // 적이 스폰된 후 몇 초 뒤부터 쏠지
    public float fireInterval = 0.5f;   // 몇 초 간격으로 계속 쏠지
    public bool loopFire = true;        // 계속 쏠 것인가?

    [Header("버스트(연사) & 쿨타임 세팅")]
    public int burstCount = 0;          // 0이면 쉬지 않고 무한 발사, N이면 N번 쏘고 쉼
    public float restTimeMin = 1.5f;    // 최소 쿨타임 (초)
    public float restTimeMax = 3.0f;    // 최대 쿨타임 (초)

    [Header("원형/나선형 세팅")]
    public int projectilesPerFire = 8;  // 한 번에 쏘는 총알 개수
    public float spiralRotSpeed = 15f;  // 나선형일 때 회전하는 속도

    [Header("부채꼴(Cone) 세팅")]
    public int coneProjectiles = 3;     // 부채꼴로 쏠 때 한 번에 나가는 개수
    public float coneAngle = 45f;       // 부채꼴이 벌어지는 각도

    [Header("애니메이션 설정")]
    [Tooltip("비워두면 자동으로 현재 오브젝트나 자식에서 애니메이터를 찾습니다.")]
    public Animator shooterAnimator;
    [Tooltip("총알을 쏠 때 실행할 애니메이션 Trigger 이름 (예: Attack)")]
    public string attackAnimTrigger = "Attack";

    private float currentSpiralAngle = 0f;

    void Start()
    {
        // 애니메이터가 비어있다면 자동 검색
        if (shooterAnimator == null)
        {
            shooterAnimator = GetComponentInChildren<Animator>();
            if (shooterAnimator == null) shooterAnimator = GetComponent<Animator>();
        }

        if (projectilePrefab != null)
        {
            StartCoroutine(ShootRoutine());
        }
    }

    IEnumerator ShootRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        int currentBurst = 0;

        while (true)
        {
            // 발사할 때마다 애니메이션 실행
            if (shooterAnimator != null && !string.IsNullOrEmpty(attackAnimTrigger))
            {
                shooterAnimator.SetTrigger(attackAnimTrigger);
            }

            FirePattern();
            currentBurst++;

            if (!loopFire) break;

            // 설정한 버스트 횟수에 도달하면 쿨타임을 가집니다.
            if (burstCount > 0 && currentBurst >= burstCount)
            {
                float randomRest = Random.Range(restTimeMin, restTimeMax);
                yield return new WaitForSeconds(randomRest); // 랜덤 쿨타임 휴식
                currentBurst = 0; // 발사 횟수 초기화
            }
            else
            {
                yield return new WaitForSeconds(fireInterval); // 일반 연사 간격
            }
        }
    }

    void FirePattern()
    {
        switch (patternType)
        {
            case PatternType.Circle:
                FireCircle();
                break;
            case PatternType.Spiral:
                FireSpiral();
                break;
            case PatternType.ConeTarget:
                FireCone();
                break;
        }
    }

    void FireCircle()
    {
        float angleStep = 360f / projectilesPerFire;
        for (int i = 0; i < projectilesPerFire; i++)
        {
            SpawnProjectile(i * angleStep);
        }
    }

    void FireSpiral()
    {
        float angleStep = 360f / projectilesPerFire;
        for (int i = 0; i < projectilesPerFire; i++)
        {
            SpawnProjectile(i * angleStep + currentSpiralAngle);
        }
        currentSpiralAngle += spiralRotSpeed; // 쏠 때마다 조금씩 회전시킴
    }

    void FireCone()
    {
        if (Player.instance == null) return;

        // 플레이어가 있는 방향을 계산
        Vector2 dirToPlayer = (Player.instance.transform.position - transform.position).normalized;
        
        // 방향을 각도(Angle)로 변환 (유니티 2D 기준 위쪽이 0도가 되도록 -90 보정)
        float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg - 90f; 

        float angleStep = coneAngle / Mathf.Max(1, (coneProjectiles - 1));
        float startAngle = baseAngle - (coneAngle / 2f);

        for (int i = 0; i < coneProjectiles; i++)
        {
            SpawnProjectile(startAngle + (angleStep * i));
        }
    }

    void SpawnProjectile(float angle)
    {
        // 일반 Instantiate 대신 오브젝트 풀링을 사용합니다.
        if (PoolingController.instance != null)
        {
            GameObject proj = PoolingController.instance.GetPoolingObject(projectilePrefab);
            proj.transform.position = transform.position;
            proj.transform.rotation = Quaternion.Euler(0, 0, angle);
            proj.SetActive(true);
        }
        else
        {
            Instantiate(projectilePrefab, transform.position, Quaternion.Euler(0, 0, angle));
        }
    }
}

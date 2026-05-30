using System.Collections;
using UnityEngine;

/// <summary>
/// 보스의 기하학적 탄막 패턴(Ring Wave, Double Helix, Targeted Flower) 및
/// 백그라운드 상시 나선형 탄막을 전담 관리하는 스크립트입니다.
/// </summary>
public class BossBulletPatternController : MonoBehaviour
{
    [Header("기본 기하학적 탄막 (백그라운드 나선형 상시 패턴)")]
    [Tooltip("체크 시 보스가 백그라운드에서 상시로 동방풍 나선형 탄막을 발사합니다.")]
    public bool enableBackgroundSpiral = true;
    [Tooltip("상시 발사할 탄알 프리팹 (비워두면 씬의 투사체에서 자동 탐색)")]
    public GameObject baseProjectilePrefab;
    [Tooltip("몇 초 간격으로 쏠 것인가?")]
    public float spiralFireInterval = 1.4f;
    [Tooltip("한 번 쏠 때 나가는 총알 개수")]
    public int spiralBulletCount = 4;
    [Tooltip("쏠 때마다 회전하는 속도 (도)")]
    public float spiralRotationSpeed = 12f;

    [Header("기하학적 패턴 1: 동심원 팽창 설정")]
    public RingWaveSettings ringWaveSettings = new RingWaveSettings();

    [Header("기하학적 패턴 2: 교차 회전 이중 나선 설정")]
    public DoubleHelixSettings doubleHelixSettings = new DoubleHelixSettings();

    [Header("기하학적 패턴 3: 플레이어 저격 별꽃 설정")]
    public TargetedFlowerSettings targetedFlowerSettings = new TargetedFlowerSettings();

    private float currentSpiralAngle = 0f;
    private Coroutine spiralCoroutine;
    private BossPatternController bossController;

    void Start()
    {
        bossController = GetComponent<BossPatternController>();
        if (bossController == null)
        {
            bossController = GetComponentInChildren<BossPatternController>();
        }

        if (enableBackgroundSpiral)
        {
            spiralCoroutine = StartCoroutine(BackgroundSpiralRoutine());
        }
    }

    /// <summary>
    /// 동방풍 탄막 게임 특유의 3가지 아름다운 기하학적 탄막 중 하나를 무작위 선택하여 전개합니다.
    /// </summary>
    public void TriggerGeometricPattern()
    {
        int pType = Random.Range(0, 3);
        switch (pType)
        {
            case 0:
                StartCoroutine(GeometricRingWaveRoutine());
                break;
            case 1:
                StartCoroutine(GeometricDoubleHelixRoutine());
                break;
            case 2:
                StartCoroutine(GeometricTargetedFlowerRoutine());
                break;
        }
    }

    private float GetHpRatio()
    {
        if (bossController != null)
        {
            return Mathf.Clamp01(bossController.currentSurvivalTimer / bossController.bossSurvivalTime);
        }
        return 1.0f;
    }

    // 1. 기하학 동심원 팽창 탄막 — 안전지대가 매 웨이브마다 변화하는 압박형 패턴
    private IEnumerator GeometricRingWaveRoutine()
    {
        Debug.Log("🌀 [총알 패턴] 기하학적 동심원 탄막 발사!");
        float hpRatio = GetHpRatio();
        int waveCount = hpRatio > 0.5f ? ringWaveSettings.ezWaveCount : ringWaveSettings.hardWaveCount;

        for (int wave = 0; wave < waveCount; wave++)
        {
            EnsureBaseProjectilePrefab();
            if (baseProjectilePrefab == null) yield break;

            int bulletCount = ringWaveSettings.baseBulletCount + wave * ringWaveSettings.bulletsPerWave;
            float angleStep = 360f / bulletCount;
            // 핵심: 매 웨이브마다 반 칸씩 회전 → 안전지대가 흔들림
            float startAngle = wave * (angleStep * 0.5f) + wave * 7f;

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle + i * angleStep;
                // 탄속이 웨이브마다 가속 → 점점 조여오는 압박감
                float speed = ringWaveSettings.baseSpeed + wave * ringWaveSettings.speedIncreasePerWave;
                // 살짝 랜덤 지터 → 완벽한 예측 방지
                angle += Random.Range(-2f, 2f);
                SpawnGeometricBullet(angle, speed);
            }

            // 웨이브 간격도 점점 짧아짐
            float delay = Mathf.Lerp(ringWaveSettings.maxWaveDelay, ringWaveSettings.minWaveDelay, (float)wave / waveCount);
            yield return new WaitForSeconds(delay);
        }
    }

    // 2. 기하학 이중 교차 회전 나선 — 회전 가감속 + 탄속 맥동 + 팔 수 증가
    private IEnumerator GeometricDoubleHelixRoutine()
    {
        Debug.Log("🌀 [총알 패턴] 기하학적 교차 회전 이중 나선 탄막 발사!");
        float hpRatio = GetHpRatio();
        float duration = hpRatio > 0.5f ? doubleHelixSettings.ezDuration : doubleHelixSettings.hardDuration;
        float elapsed = 0f;
        float fireInterval = doubleHelixSettings.fireInterval;
        float currentAngle = 0f;

        while (elapsed < duration)
        {
            EnsureBaseProjectilePrefab();
            if (baseProjectilePrefab == null) yield break;

            float progress = elapsed / duration; // 0 → 1

            // 회전 가감속: 시작 느리게 → 중반 빠르게 → 끝 느리게 (호흡감)
            float rotSpeed = doubleHelixSettings.baseRotSpeed + doubleHelixSettings.rotSpeedAmplitude * Mathf.Sin(progress * Mathf.PI);

            // 팔 수가 패턴 진행에 따라 증가
            int spiralArms = doubleHelixSettings.baseSpiralArms + Mathf.FloorToInt(progress * doubleHelixSettings.extraArmsOverTime);
            float angleStep = 360f / spiralArms;

            // 탄속 맥동: 사인파로 빠름↔느림 반복 → 탄알 밀도가 물결치듯 변화
            float speedPulse = doubleHelixSettings.baseSpeed + doubleHelixSettings.speedPulseAmplitude * Mathf.Sin(elapsed * doubleHelixSettings.speedPulseFrequency);

            for (int i = 0; i < spiralArms; i++)
            {
                // 정방향(우회전) 나선
                SpawnGeometricBullet(currentAngle + i * angleStep, speedPulse);
                // 역방향(좌회전) 나선
                SpawnGeometricBullet(-currentAngle + i * angleStep, speedPulse * 0.85f);
            }

            currentAngle += rotSpeed;
            elapsed += fireInterval;
            yield return new WaitForSeconds(fireInterval);
        }
    }

    // 3. 플레이어 저격 별꽃 — 꽃잎 수 증가 + 광폭/협폭 교차 + 탄속 레이어링
    private IEnumerator GeometricTargetedFlowerRoutine()
    {
        Debug.Log("🌀 [총알 패턴] 기하학적 플레이어 조준 별꽃 탄막 발사!");
        if (Player.instance == null) yield break;

        float hpRatio = GetHpRatio();
        int burstCount = hpRatio > 0.5f ? targetedFlowerSettings.ezBurstCount : targetedFlowerSettings.hardBurstCount;

        for (int burst = 0; burst < burstCount; burst++)
        {
            if (Player.instance == null) yield break;
            EnsureBaseProjectilePrefab();
            if (baseProjectilePrefab == null) yield break;

            Vector2 dirToPlayer = (Player.instance.transform.position - transform.position).normalized;
            float baseAngle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg - 90f;

            // 꽃잎 수가 버스트마다 증가
            int petalCount = targetedFlowerSettings.basePetalCount + burst * targetedFlowerSettings.petalsPerBurst;
            // 광폭 ↔ 협폭 교차: 홀수 버스트는 넓게, 짝수는 좁게
            float spreadAngle = (burst % 2 == 0) ? targetedFlowerSettings.wideSpreadAngle : targetedFlowerSettings.narrowSpreadAngle;
            float angleStep = spreadAngle / Mathf.Max(1, petalCount - 1);
            float startAngle = baseAngle - (spreadAngle / 2f);

            for (int i = 0; i < petalCount; i++)
            {
                float angle = startAngle + i * angleStep;
                // 2중 레이어: 빠른 탄 + 느린 탄을 동시에 쏴서 시간차 겹침 유도
                float fastSpeed = targetedFlowerSettings.baseFastSpeed + burst * targetedFlowerSettings.fastSpeedIncrease;
                float slowSpeed = targetedFlowerSettings.baseSlowSpeed + burst * targetedFlowerSettings.slowSpeedIncrease;
                SpawnGeometricBullet(angle, fastSpeed);
                SpawnGeometricBullet(angle + angleStep * 0.5f, slowSpeed);
            }

            // 버스트 간격에 리듬감 부여: 짧은-긴 교차
            float delay = (burst % 2 == 0) ? targetedFlowerSettings.wideDelay : targetedFlowerSettings.narrowDelay;
            yield return new WaitForSeconds(delay);
        }
    }

    private void EnsureBaseProjectilePrefab()
    {
        if (baseProjectilePrefab != null)
        {
            try { var _ = baseProjectilePrefab.name; }
            catch { baseProjectilePrefab = null; }
        }

        if (baseProjectilePrefab == null)
        {
            var shooter = FindObjectOfType<PatternShooter>();
            if (shooter != null)
            {
                baseProjectilePrefab = shooter.projectilePrefab;
            }
        }
    }

    private void SpawnGeometricBullet(float angle, float speed)
    {
        if (baseProjectilePrefab == null) return;

        GameObject bullet = null;
        if (PoolingController.instance != null)
        {
            bullet = PoolingController.instance.GetPoolingObject(baseProjectilePrefab);
            if (bullet == null) return;
            bullet.transform.position = transform.position;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            bullet.SetActive(true);
        }
        else
        {
            bullet = Instantiate(baseProjectilePrefab, transform.position, Quaternion.Euler(0, 0, angle));
        }

        if (bullet != null)
        {
            bullet.tag = "Projectile";
            DirectMoving dm = bullet.GetComponent<DirectMoving>();
            if (dm == null) dm = bullet.AddComponent<DirectMoving>();
            dm.speed = speed;
            dm.isHoming = false; 
        }
    }

    IEnumerator BackgroundSpiralRoutine()
    {
        if (bossController != null)
        {
            yield return new WaitForSeconds(bossController.initialDelay);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        while (true)
        {
            if (LevelController.instance == null || !LevelController.instance.isFrenzyPhase)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            EnsureBaseProjectilePrefab();

            if (baseProjectilePrefab != null)
            {
                float angleStep = 360f / spiralBulletCount;
                for (int i = 0; i < spiralBulletCount; i++)
                {
                    float angle = (i * angleStep) + currentSpiralAngle;
                    SpawnProjectile(angle);
                }
                currentSpiralAngle += spiralRotationSpeed;
            }

            yield return new WaitForSeconds(spiralFireInterval);
        }
    }

    void SpawnProjectile(float angle)
    {
        if (baseProjectilePrefab == null) return;

        if (PoolingController.instance != null)
        {
            GameObject proj = PoolingController.instance.GetPoolingObject(baseProjectilePrefab);
            if (proj == null) return;
            proj.transform.position = transform.position;
            proj.transform.rotation = Quaternion.Euler(0, 0, angle);
            proj.SetActive(true);
        }
        else
        {
            Instantiate(baseProjectilePrefab, transform.position, Quaternion.Euler(0, 0, angle));
        }
    }

    void OnDestroy()
    {
        if (spiralCoroutine != null) StopCoroutine(spiralCoroutine);
    }

    [System.Serializable]
    public class RingWaveSettings
    {
        [Tooltip("체력 50% 이상일 때의 웨이브 횟수")]
        public int ezWaveCount = 3;
        [Tooltip("체력 50% 미만일 때의 웨이브 횟수")]
        public int hardWaveCount = 4;
        [Tooltip("기본 총알 개수 (웨이브 1)")]
        public int baseBulletCount = 10;
        [Tooltip("웨이브마다 추가되는 총알 개수")]
        public int bulletsPerWave = 2;
        [Tooltip("기본 총알 속도")]
        public float baseSpeed = 4.5f;
        [Tooltip("웨이브마다 증가하는 속도")]
        public float speedIncreasePerWave = 1.0f;
        [Tooltip("최대 웨이브 지연 시간 (웨이브가 진행될수록 점점 짧아짐)")]
        public float maxWaveDelay = 0.70f;
        [Tooltip("최소 웨이브 지연 시간 (웨이브가 진행될수록 점점 짧아짐)")]
        public float minWaveDelay = 0.45f;
    }

    [System.Serializable]
    public class DoubleHelixSettings
    {
        [Tooltip("체력 50% 이상일 때의 지속 시간 (초)")]
        public float ezDuration = 3.0f;
        [Tooltip("체력 50% 미만일 때의 지속 시간 (초)")]
        public float hardDuration = 4.5f;
        [Tooltip("발사 간격 (초)")]
        public float fireInterval = 0.24f;
        [Tooltip("기본 회전 속도 (도/초)")]
        public float baseRotSpeed = 15f;
        [Tooltip("회전 속도 진폭 (도/초)")]
        public float rotSpeedAmplitude = 25f;
        [Tooltip("시작 시 나선형 팔(Arm) 수")]
        public int baseSpiralArms = 2;
        [Tooltip("패턴 진행에 따라 증가하는 최대 팔 수")]
        public float extraArmsOverTime = 1.0f;
        [Tooltip("기본 탄속")]
        public float baseSpeed = 5.5f;
        [Tooltip("탄속 맥동 진폭")]
        public float speedPulseAmplitude = 2.5f;
        [Tooltip("탄속 맥동 주파수")]
        public float speedPulseFrequency = 5f;
    }

    [System.Serializable]
    public class TargetedFlowerSettings
    {
        [Tooltip("체력 50% 이상일 때의 버스트(점사) 횟수")]
        public int ezBurstCount = 3;
        [Tooltip("체력 50% 미만일 때의 버스트(점사) 횟수")]
        public int hardBurstCount = 5;
        [Tooltip("기본 꽃잎(탄알) 개수")]
        public int basePetalCount = 4;
        [Tooltip("버스트마다 추가되는 꽃잎 개수")]
        public int petalsPerBurst = 1;
        [Tooltip("광폭 버스트 시의 스프레드 각도 (도)")]
        public float wideSpreadAngle = 75f;
        [Tooltip("협폭 버스트 시의 스프레드 각도 (도)")]
        public float narrowSpreadAngle = 35f;
        [Tooltip("기본 빠른 탄속")]
        public float baseFastSpeed = 10f;
        [Tooltip("버스트마다 증가하는 빠른 탄속")]
        public float fastSpeedIncrease = 0.5f;
        [Tooltip("기본 느린 탄속")]
        public float baseSlowSpeed = 5.5f;
        [Tooltip("버스트마다 증가하는 느린 탄속")]
        public float slowSpeedIncrease = 0.3f;
        [Tooltip("광폭 버스트 시 지연 시간 (초)")]
        public float wideDelay = 0.35f;
        [Tooltip("협폭 버스트 시 지연 시간 (초)")]
        public float narrowDelay = 0.55f;
    }
}

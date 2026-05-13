using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPattern_GateStream : MonoBehaviour
{
    [Header("프리팹 설정")]
    [Tooltip("패턴 시작 전 레이저가 나갈 방향마다 띄울 경고 프리팹 (예: 조준선, 위험 표시기 등)")]
    public GameObject warningPrefab;

    [Tooltip("발사할 총알 프리팹 (DirectMoving 포함)")]
    public GameObject bulletPrefab;

    [Header("패턴 설정")]
    [Tooltip("총알을 쏠 방향(갈래) 개수")]
    public int numberOfArms = 8;

    [Tooltip("가운데 안전지대 크기 (보스 중심으로부터의 거리)")]
    public float spawnRadius = 1.5f;

    [Tooltip("총알(레이저) 위치 오프셋 (발사 위치를 앞으로 밀어낼 때 사용)")]
    public float bulletOffset = 0f;

    [Tooltip("체크 시 바깥에서 안쪽(플레이어 방향)으로 레이저를 쏩니다.")]
    public bool fireInward = false;

    [Header("시간 설정")]
    [Tooltip("총알이 쏟아지기 전 경고 시간 (위험을 알리는 시간)")]
    public float warningTime = 1.0f;

    [Tooltip("총알이 같은 자리에 연속으로 쏟아지는 시간 (초)")]
    public float fireDuration = 3.0f;

    [Tooltip("발사 간격 (짧을수록 빔이나 폭포수처럼 촘촘해짐)")]
    public float fireRate = 0.05f;

    [Tooltip("패턴 1회가 완전히 끝난 후 다음 패턴을 시작할 때까지 대기 시간")]
    public float cooldown = 2.0f;

    [Header("패턴 작동 방식")]
    [Tooltip("체크 시 쿨타임 이후 이 패턴을 무한히 반복합니다. 해제 시 한 번만 쏘고 멈춥니다.")]
    public bool repeatPattern = true;

    [Tooltip("체크 시 레이저가 발사되기 시작할 때 경고 마크가 사라집니다. (해제 시 발사가 끝날 때까지 남아있음)")]
    public bool destroyWarningOnFire = true;

    [Header("애니메이션 (선택사항)")]
    [Tooltip("보스의 애니메이터를 연결하면, 발사 타이밍에 맞춰 애니메이션을 재생합니다.")]
    public Animator bossAnimator;
    
    [Tooltip("총알이 나갈 때 실행할 애니메이션 Trigger 파라미터 이름 (예: Attack)")]
    public string attackAnimTrigger = "Attack";

    // 생성된 워닝들을 관리하는 리스트
    private List<GameObject> activeWarnings = new List<GameObject>();

    private void Start()
    {
        StartCoroutine(GateStreamRoutine());
    }

    IEnumerator GateStreamRoutine()
    {
        while (true)
        {
            // 쏠 때마다 전체 각도를 랜덤하게 비틀어줍니다.
            float baseAngle = Random.Range(0f, 360f);

            // 0. 이번 패턴이 생성될 '중심 위치'를 플레이어의 현재 위치로 설정합니다.
            Vector3 centerPos = transform.position;
            if (Player.instance != null)
            {
                centerPos = Player.instance.transform.position;
            }

            // 1. 레이저가 발사될 모든 방향에 경고(워닝) 생성
            if (warningPrefab != null)
            {
                for (int i = 0; i < numberOfArms; i++)
                {
                    float angle = baseAngle + (i * (360f / numberOfArms));
                    Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
                    
                    Vector3 spawnPos = centerPos + (rotation * Vector3.up * spawnRadius);
                    Vector3 warningPos = spawnPos + (rotation * Vector3.up * bulletOffset);
                    Quaternion warningRot = fireInward ? Quaternion.Euler(0f, 0f, angle + 180f) : rotation;

                    GameObject warning = Instantiate(warningPrefab, warningPos, warningRot);
                    activeWarnings.Add(warning);
                }
            }

            // 2. 경고 시간 대기
            yield return new WaitForSeconds(warningTime);

            // [추가] 실제 총알(레이저) 발사 타이밍에 보스 공격 애니메이션 재생
            if (bossAnimator != null && !string.IsNullOrEmpty(attackAnimTrigger))
            {
                bossAnimator.SetTrigger(attackAnimTrigger);
            }

            // [추가] 발사 직전에 워닝 마크 지우기 옵션
            if (destroyWarningOnFire)
            {
                foreach (GameObject w in activeWarnings)
                {
                    if (w != null) Destroy(w);
                }
                activeWarnings.Clear();
            }

            // 3. 3초간 제자리에서 연속 발사
            float fireTimer = 0f;
            while (fireTimer < fireDuration)
            {
                for (int i = 0; i < numberOfArms; i++)
                {
                    float angle = baseAngle + (i * (360f / numberOfArms));
                    Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
                    
                    Vector3 spawnPos = centerPos + (rotation * Vector3.up * spawnRadius);
                    Vector3 bulletPos = spawnPos + (rotation * Vector3.up * bulletOffset);
                    Quaternion bulletRot = fireInward ? Quaternion.Euler(0f, 0f, angle + 180f) : rotation;

                    Instantiate(bulletPrefab, bulletPos, bulletRot);
                }

                yield return new WaitForSeconds(fireRate);
                fireTimer += fireRate;
            }

            // 4. 발사 종료 후 워닝 마크 지우기 (만약 위에서 안 지웠다면)
            foreach (GameObject w in activeWarnings)
            {
                if (w != null) Destroy(w);
            }
            activeWarnings.Clear();

            // 한 번만 쏘는 설정이면 반복문 탈출
            if (!repeatPattern)
            {
                break;
            }

            // 5. 다음 패턴을 쏘기 전 쿨타임
            yield return new WaitForSeconds(cooldown);
        }
    }

    // 만약 패턴 도중에 보스가 죽으면, 허공에 남은 워닝 지우기
    private void OnDestroy()
    {
        foreach (GameObject w in activeWarnings)
        {
            if (w != null) Destroy(w);
        }
    }
}

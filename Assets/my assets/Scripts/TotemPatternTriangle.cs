using System.Collections;
using UnityEngine;

/// <summary>
/// 삼각형 토템(hodge_트라이앵글) 전용 플레이어 조준 3방향 부채꼴 탄막 컴포넌트입니다.
/// </summary>
public class TotemPatternTriangle : MonoBehaviour, ITotemPattern
{
    [Header("=== 삼각형 조준 탄막 설정 ===")]
    [Tooltip("발사할 적 총알 프리팹")]
    public GameObject bulletPrefab;
    [Tooltip("사격 주기 (초)")]
    public float fireRate = 0.6f;
    [Tooltip("3방향 부채꼴 탄막의 벌어지는 최대 범위 각도")]
    public float coneAngle = 30f;

    private Coroutine shootCoroutine;
    private Transform playerTransform;

    private void Start()
    {
        if (Player.instance != null) playerTransform = Player.instance.transform;
    }

    public void StartPattern()
    {
        if (Player.instance != null) playerTransform = Player.instance.transform;
        
        if (shootCoroutine != null) StopCoroutine(shootCoroutine);
        shootCoroutine = StartCoroutine(ShootRoutine());
    }

    public void StopPattern()
    {
        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
        }
    }

    private IEnumerator ShootRoutine()
    {
        while (true)
        {
            if (playerTransform != null)
            {
                // 플레이어 조준 방향 벡터 및 베이스 각도 계산 (2D 90도 보정 포함)
                Vector2 direction = (playerTransform.position - transform.position).normalized;
                float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

                float angleStep = coneAngle / 2f;
                // 플레이어 조준을 기준으로 삼각 형태의 3방향 발사 (-angleStep, 0, +angleStep)
                SpawnBullet(baseAngle - angleStep);
                SpawnBullet(baseAngle);
                SpawnBullet(baseAngle + angleStep);
            }
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void SpawnBullet(float angle)
    {
        if (bulletPrefab == null) return;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        
        if (PoolingController.instance != null)
        {
            GameObject bullet = PoolingController.instance.GetPoolingObject(bulletPrefab);
            if (bullet != null)
            {
                bullet.transform.position = transform.position;
                bullet.transform.rotation = rotation;
                bullet.SetActive(true);
                return;
            }
        }
        Instantiate(bulletPrefab, transform.position, rotation);
    }
}

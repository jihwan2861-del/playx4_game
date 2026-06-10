using System.Collections;
using UnityEngine;

/// <summary>
/// 네모 토템(hodge_네모) 전용 4방향 십자(Cross) 탄막 컴포넌트입니다.
/// </summary>
public class TotemPatternSquare : MonoBehaviour, ITotemPattern
{
    [Header("=== 네모 십자 탄막 설정 ===")]
    [Tooltip("발사할 적 총알 프리팹")]
    public GameObject bulletPrefab;
    [Tooltip("탄막 발사 주기 (초)")]
    public float fireRate = 0.4f;
    [Tooltip("체크 시 X자 대각선으로 쏘고, 해제 시 십자(+) 방향으로 쏩니다.")]
    public bool isDiagonal = false;

    private Coroutine shootCoroutine;

    public void StartPattern()
    {
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
            float startAngle = isDiagonal ? 45f : 0f;
            // 90도 각도 간격으로 4방향 사출
            for (int i = 0; i < 4; i++)
            {
                SpawnBullet(startAngle + (i * 90f));
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

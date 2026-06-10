using System.Collections;
using UnityEngine;

/// <summary>
/// 원형 토템(hodge_원) 전용 나선 회전(Spiral) 탄막 컴포넌트입니다.
/// </summary>
public class TotemPatternCircle : MonoBehaviour, ITotemPattern
{
    [Header("=== 원형 탄막 설정 ===")]
    [Tooltip("발사할 적 총알 프리팹")]
    public GameObject bulletPrefab;
    [Tooltip("나선형 회오리 탄막의 갈래 수")]
    public int armsCount = 3;
    [Tooltip("사격 간격 (초)")]
    public float fireRate = 0.1f;
    [Tooltip("발사 시마다 누적 회전할 각도")]
    public float rotationSpeed = 15f;

    private Coroutine shootCoroutine;
    private float currentAngle = 0f;

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
            float angleStep = 360f / armsCount;
            for (int i = 0; i < armsCount; i++)
            {
                float targetAngle = (i * angleStep) + currentAngle;
                SpawnBullet(targetAngle);
            }
            currentAngle += rotationSpeed; // 쏠 때마다 누적 회전
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void SpawnBullet(float angle)
    {
        if (bulletPrefab == null) return;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        
        // PoolingController가 씬에 존재하면 오브젝트 풀 우선 활용
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

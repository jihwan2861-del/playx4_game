using UnityEngine;
using System.Collections;

/// <summary>
/// 비행기의 체력과 무적 상태를 관리하는 스크립트입니다.
/// </summary>
public class Player : MonoBehaviour
{
    public GameObject destructionFX;
    
    [Header("Health Settings")]
    public int maxHealth = 10;
    public int health = 10;

    public static Player instance; 
    
    [Header("Invincibility Flags")]
    [HideInInspector] public bool isInvincible = false;      // 대쉬 등 일반 무적
    [HideInInspector] public bool safeZoneInvincible = false; // 세이프존 무적
    public float damageInvincibilityDuration = 1.5f;          // 피격 시 무적 시간

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (instance == null) 
            instance = this;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 데이터 매니저에 저장된 체력 업그레이드 수치 적용 (레벨당 체력 +2)
        if (PlayerDataManager.instance != null)
        {
            maxHealth = 10 + PlayerDataManager.instance.maxHpLevel * 2;
        }
        
        health = maxHealth;
    }

    public void GetDamage(int damage, GameObject source = null)   
    {
        // 패링 활성화 상태라면 무적 여부와 관계없이 패링 성공을 성사시킵니다!
        if (PlayerMoving.instance != null && PlayerMoving.instance.isParryActive)
        {
            if (PlayerMoving.instance.TryTriggerParrySuccess(source))
            {
                return; // 패링 성공하여 데미지 판정 자체를 무시(Block)합니다!
            }
        }

        // 대쉬 중이거나 세이프존 안에 있으면 무시함
        if (isInvincible || safeZoneInvincible) return;
        
        health -= damage;

        // 데미지를 입었을 때 넉백 적용
        if (PlayerMoving.instance != null)
        {
            Vector3 sourcePos = source != null ? source.transform.position : transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
            PlayerMoving.instance.ApplyKnockback(sourcePos);
        }

        // 맞을 때 화면 흔들림 효과 (강도: 0.4, 시간: 0.2초)
        if (Camera.main != null)
        {
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null) camFollow.Shake(0.2f, 0.4f);
        }

        // 플레이어 피격 시 역경직 (0.1초 - 위기감 강조)
        if (HitStop.instance != null)
            HitStop.instance.Do(0.1f);

        if (health <= 0)
        {
            Destruction();
        }
        else
        {
            StartCoroutine(DamageFlash());
        }
    }    

    IEnumerator DamageFlash()
    {
        isInvincible = true;
        if (spriteRenderer != null) spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        
        float elapsedTime = 0f;
        float blinkDuration = damageInvincibilityDuration - 0.2f;
        bool isTransparent = false;

        while (elapsedTime < blinkDuration)
        {
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                if (c == Color.red) c = Color.white;
                c.a = isTransparent ? 1f : 0.5f;
                spriteRenderer.color = c;
            }
            isTransparent = !isTransparent;
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }

        if (spriteRenderer != null && !safeZoneInvincible) 
        {
            spriteRenderer.color = Color.white;
        }
        isInvincible = false;
    }

    public IEnumerator DashInvincibility(float duration)
    {
        isInvincible = true;
        if (spriteRenderer != null) spriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(duration);
        if (spriteRenderer != null && !safeZoneInvincible) 
            spriteRenderer.color = Color.white;
        isInvincible = false;
    }

    public void SetDashInvincible(bool invincible)
    {
        isInvincible = invincible;

        if (spriteRenderer == null || safeZoneInvincible)
        {
            return;
        }

        spriteRenderer.color = invincible ? Color.yellow : Color.white;
    }

    void Destruction()
    {
        if (destructionFX != null)
            Instantiate(destructionFX, transform.position, Quaternion.identity);

        // 죽었을 때 화면이 어두워지고 마을/재시작 메뉴가 뜨는 연출 실행
        if (GameTransitionManager.instance != null)
            GameTransitionManager.instance.OnPlayerDeath();

        Destroy(gameObject);
    }
}

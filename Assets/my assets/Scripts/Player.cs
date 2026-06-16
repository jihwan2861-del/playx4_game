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
    private Coroutine damageFlashCoroutine;

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
            if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = StartCoroutine(DamageFlash());
        }
    }    

    IEnumerator DamageFlash()
    {
        isInvincible = true;

        if (spriteRenderer != null)
        {
            Material originalMat = spriteRenderer.material;
            Color originalColor = spriteRenderer.color;

            // 단색 흰색 실루엣을 그리는 GUI/Text Shader를 찾아 임시 적용합니다.
            Shader whiteShader = Shader.Find("GUI/Text Shader");
            Material whiteMat = null;
            if (whiteShader != null)
            {
                whiteMat = new Material(whiteShader);
            }

            if (whiteMat != null)
            {
                spriteRenderer.material = whiteMat;
                spriteRenderer.color = Color.white; // 단색 흰색으로 강렬하게 번쩍임
            }
            else
            {
                spriteRenderer.color = Color.white;
            }

            // 흰색 플래시 번쩍임 유지 시간 (0.15초)
            yield return new WaitForSeconds(0.15f);

            // 본래 머티리얼 및 색상 복구
            spriteRenderer.material = originalMat;
            spriteRenderer.color = originalColor;

            if (whiteMat != null)
            {
                Destroy(whiteMat);
            }

            // 이후 무적 시간 동안 깜빡임 연출 실행
            float elapsedTime = 0f;
            float blinkDuration = damageInvincibilityDuration - 0.15f;
            bool isTransparent = false;

            while (elapsedTime < blinkDuration)
            {
                Color c = spriteRenderer.color;
                c.a = isTransparent ? 1f : 0.5f;
                spriteRenderer.color = c;

                isTransparent = !isTransparent;
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }

            // 완전히 정상 알파 복귀
            Color finalColor = spriteRenderer.color;
            finalColor.a = 1f;
            spriteRenderer.color = finalColor;
        }
        else
        {
            yield return new WaitForSeconds(damageInvincibilityDuration);
        }

        damageFlashCoroutine = null;
        isInvincible = false;
    }

    /// <summary>
    /// 외부(패링 성공 시 등)에서 피격 연출을 강제로 중단시키고 
    /// 스프라이트 알파를 원래대로 정상화시키기 위해 호출합니다.
    /// </summary>
    public void StopDamageFlash()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            // 피격 머티리얼이 남아있는 경우를 대비해 초기화 복원
            Shader whiteShader = Shader.Find("GUI/Text Shader");
            if (spriteRenderer.material != null && whiteShader != null && spriteRenderer.material.shader == whiteShader)
            {
                // GUI/Text Shader를 원래 머티리얼로 복구하기 위해, 리셋 로직을 호출하거나 기본 머티리얼 적용
                // 여기서는 안전하게 색상과 알파만 복원
            }
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    public IEnumerator DashInvincibility(float duration, bool changeColor = true)
    {
        isInvincible = true;
        if (changeColor && spriteRenderer != null) spriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(duration);
        if (changeColor && spriteRenderer != null && !safeZoneInvincible) 
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

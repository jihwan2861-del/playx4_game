using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script defines 'Enemy's' health and behavior. 
/// </summary>
public class Enemy : MonoBehaviour {

    #region FIELDS
    [Tooltip("Health points in integer")]
    public int health;

    [Tooltip("Enemy's projectile prefab")]
    public GameObject Projectile;

    [Tooltip("VFX prefab generating after destruction")]
    public GameObject destructionVFX;
    public GameObject hitEffect;
    
    [HideInInspector] public int shotChance; //probability of 'Enemy's' shooting during tha path
    [HideInInspector] public float shotTimeMin, shotTimeMax; //max and min time for shooting from the beginning of the path
    
    [Header("Difficulty Tweaks")]
    [Tooltip("총알이 유도탄일 경우, 발사 확률을 몇 배로 줄일지(나눌지) 결정합니다. (3이면 확률 1/3로 감소)")]
    public float homingMissileNerf = 3f;
    [Header("Visual Override (그림 덮어씌우기)")]
    [Tooltip("에디터에서 만든 애니메이션 프리팹을 여기에 넣으면, 기존 우주선 그림을 지우고 이 그림으로 교체됩니다.")]
    public GameObject overrideVisualPrefab;
    [Header("Animation Settings")]
    [Tooltip("총알을 쏠 때 실행할 애니메이션 Trigger 이름 (예: Attack)")]
    public string attackAnimTrigger = "Attack";
    #endregion

    private void Start()
    {
        // 사용자가 새 애니메이션 프리팹을 드래그해서 넣었다면?
        if (overrideVisualPrefab != null)
        {
            // 원래 있던 낡은 이미지는 안 보이게 투명하게 만듭니다.
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            // 방금 넣은 새 애니메이션 프리팹을 내 자식(껍데기)으로 스폰시킵니다!
            GameObject visual = Instantiate(overrideVisualPrefab, transform.position, transform.rotation, transform);
            
            // [긴급 복구] 사용자가 프리팹의 그리기 순서나 투명도를 실수로 잘못 설정했을 경우를 대비하여 
            // 무조건 화면 최상단(Order 50)에, 투명도 없이 강제로 그려지도록 조치합니다.
            SpriteRenderer[] srs = visual.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer s in srs)
            {
                s.sortingLayerName = "Default";
                s.sortingOrder = 50; 
                s.color = new Color(s.color.r, s.color.g, s.color.b, 1f);
                s.enabled = true;
            }
        }

        Invoke("ActivateShooting", Random.Range(shotTimeMin, shotTimeMax));
    }

    //coroutine making a shot
    void ActivateShooting() 
    {
        return; // 사용자의 요청으로 적 총알 발사 기능 비활성화
        float currentChance = shotChance;
        
        // 장착된 총알이 유도탄일 경우 발사 확률을 대폭 깎아서 게임오버를 방지합니다.
        if (Projectile != null)
        {
            var moveScript = Projectile.GetComponent<DirectMoving>();
            if (moveScript != null && moveScript.isHoming)
            {
                currentChance = currentChance / homingMissileNerf;
            }
        }

        if (Random.value < currentChance / 100f)                             //if random value less than shot probability, making a shot
        {                         
            // [애니메이션 추가] 총알 발사 시 공격 애니메이션 실행
            Animator anim = GetComponentInChildren<Animator>(); // 자식으로 들어간 overrideVisualPrefab 등에서 찾음
            if (anim == null) anim = GetComponent<Animator>();

            if (anim != null && !string.IsNullOrEmpty(attackAnimTrigger))
            {
                anim.SetTrigger(attackAnimTrigger);
            }

            GameObject bullet = Instantiate(Projectile, gameObject.transform.position, Quaternion.identity); 

            // 발사한 것이 '레이저' 라면 (LaserBeam 스크립트를 갖고 있다면)
            if (bullet.GetComponent<LaserBeam>() != null)
            {
                // 레이저가 적 오브젝트를 따라다니도록 자식(Child)으로 설정합니다.
                bullet.transform.SetParent(gameObject.transform);
                
                // 레이저의 위치를 적 우주선의 조금 아래쪽에 맞춥니다.
                bullet.transform.localPosition = new Vector3(0, -3f, 0); 
            }             
        }
    }

    private Coroutine flashCoroutine;

    //method of getting damage for the 'Enemy'
    public void GetDamage(int damage) 
    {
        // 보스 오브젝트 여부 판별 (BossPatternController가 있거나 부모 오브젝트 계층에 존재한다면 보스로 확정!)
        BossPatternController bossCtrl = GetComponent<BossPatternController>();
        if (bossCtrl == null) bossCtrl = GetComponentInParent<BossPatternController>();
        
        if (bossCtrl != null)
        {
            // 보스 타격 기믹: 일반 공격 피격 시 보스 생존 타이머(체력)를 대미지당 0.05초씩 단축시켜 주는 획기적 혜택 제공!
            if (LevelController.instance != null && LevelController.instance.isFrenzyPhase && bossCtrl.currentSurvivalTimer > 0)
            {
                float timeReduction = damage * 0.05f;
                bossCtrl.currentSurvivalTimer -= timeReduction;
                if (bossCtrl.currentSurvivalTimer < 0) bossCtrl.currentSurvivalTimer = 0;
            }

            // 피격 이펙트 생성
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity, transform);
            }

            // 피격 시 빨갛게 반짝이는 타격 피드백(Hit Flash) 실행 (보스에게도 최고의 타격감을 유지!)
            if (gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(HitFlashRoutine());
            }

            // 보스는 피격으로 인해 Destruction()이 호출되는 것을 완벽히 방지하여 증발 버그 차단!
            return;
        }

        health -= damage;           //reducing health for damage value, if health is less than 0, starting destruction procedure
        if (health <= 0)
        {
            Destruction();
        }
        else
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity, transform);
            }

            // 피격 시 빨갛게 반짝이는 타격 피드백(Hit Flash) 실행!
            if (gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(HitFlashRoutine());
            }
        }
    }    

    IEnumerator HitFlashRoutine()
    {
        // 1. 모든 SpriteRenderer 수집 (자식 오브젝트 포함하여 Visual Override 대응)
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0) yield break;

        // 2. 원래 색상 저장
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) originalColors[i] = renderers[i].color;
        }

        // 3. 피격용 강렬한 빨간색(Hit Red)으로 변경
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = new Color(1f, 0.2f, 0.2f, 1f); 
            }
        }

        // 4. 아주 짧게 대기 (0.07초 - 아케이드 타격 찰진 프레임)
        yield return new WaitForSeconds(0.07f);

        // 5. 원래 색상으로 복원
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = originalColors[i];
            }
        }
    }

    //if 'Enemy' collides 'Player', 'Player' gets the damage equal to projectile's damage value
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            if (Projectile.GetComponent<Projectile>() != null)
                Player.instance.GetDamage(Projectile.GetComponent<Projectile>().damage, gameObject);
            else
                Player.instance.GetDamage(1, gameObject);
        }
    }

    //method of destroying the 'Enemy'
    void Destruction()                           
    {        
        Instantiate(destructionVFX, transform.position, Quaternion.identity);

        // 적 파괴 시 역경직 (0.08초 - 짧고 찰진 Hit Stop)
        if (HitStop.instance != null)
            HitStop.instance.Do(0.08f);

        Destroy(gameObject);
    }
}

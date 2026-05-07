using UnityEngine;
using System.Collections;

/// <summary>
/// 마을(Village_Scene) 전용 플레이어 이동 스크립트입니다.
/// 수동 이동과 자동 이동(NPC로 걸어가기)을 모두 처리합니다.
/// </summary>
public class PlayerVillageMoving : MonoBehaviour
{
    [Header("이동 설정")]
    public float speed = 5f;
    public float autoWalkSpeed = 4f; // NPC로 걸어갈 때의 속도

    [HideInInspector]
    public bool controlIsActive = true;

    private Animator anim;
    private Rigidbody2D rb;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!controlIsActive)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, vertical, 0).normalized;

        if (anim != null)
        {
            bool isMoving = moveDirection != Vector3.zero;
            anim.SetBool("isMoving", isMoving);
            
            if (isMoving)
            {
                anim.SetFloat("InputX", horizontal);
                anim.SetFloat("InputY", vertical);
            }
        }

        if (rb != null)
        {
            rb.velocity = moveDirection * speed;
        }
        else
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }
    }

    // HubUIManager 등 외부에서 "저 타겟으로 걸어가라!"고 명령할 때 호출
    public void WalkToTarget(Transform target, System.Action onArrived)
    {
        controlIsActive = false; // 유저 조작 막기
        StartCoroutine(AutoWalkRoutine(target, onArrived));
    }

    private IEnumerator AutoWalkRoutine(Transform target, System.Action onArrived)
    {
        if (target == null)
        {
            onArrived?.Invoke();
            yield break; // 타겟 없으면 바로 끝내기
        }

        // 목표 위치 (Z축은 그대로)
        Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z);

        // 도착할 때까지 반복
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            Vector3 direction = (targetPos - transform.position).normalized;

            if (anim != null)
            {
                anim.SetBool("isMoving", true);
                anim.SetFloat("InputX", direction.x);
                anim.SetFloat("InputY", direction.y);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPos, autoWalkSpeed * Time.deltaTime);
            yield return null;
        }

        // 도착!
        if (anim != null) anim.SetBool("isMoving", false);

        // 여운을 위해 아주 살짝 멈췄다가 콜백 실행 (패널 열기 등)
        yield return new WaitForSeconds(0.3f);
        onArrived?.Invoke();
    }
}

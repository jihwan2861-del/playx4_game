using UnityEngine;
using System.Collections;

/// <summary>
/// 똑똑한 적(중간 보스 등)이 화면에 들어와서 멈춘 뒤, 탄막을 쏘고 다시 도망가거나 쫓아오는 AI입니다.
/// </summary>
public class EnemySmartAI : MonoBehaviour
{
    public enum BehaviorState { MoveIn, Attack, MoveOut, ChasePlayer }
    
    [Header("상태 설정")]
    public BehaviorState currentState = BehaviorState.MoveIn;
    public BehaviorState afterAttackState = BehaviorState.MoveOut; // 공격 후 도망갈지 쫓아올지
    
    [Header("이동 및 공격 시간")]
    public float speed = 5f;
    public float attackDuration = 4f; // 제자리에 멈춰서 공격(탄막)을 쏘는 시간
    
    [Header("공격 중 추적 설정")]
    public bool chaseWhileAttacking = true; // 공격 중에도 플레이어를 향해 다가갈지 여부
    public float attackChaseSpeed = 1.5f;   // 공격 중 다가가는 속도 (기본 속도보다 느리게)
    
    private Vector2 targetEntryPos;
    private bool hasArrived = false;
    
    private Animator anim;
    private Vector3 lastPos;

    void Start()
    {
        // 적이 스폰되면, 현재 위치에서 아래쪽으로 조금 내려온 위치를 '도착 지점'으로 삼습니다.
        // 화면 안으로 멋지게 쓱- 들어와서 멈추게 하기 위함입니다.
        targetEntryPos = transform.position + Vector3.down * 4f; 
        
        // 만약 이 스크립트를 쓰면, 기존 직진(DirectMoving) 스크립트와 충돌나므로 꺼버립니다.
        var dm = GetComponent<DirectMoving>();
        if (dm != null) dm.enabled = false;
        
        anim = GetComponentInChildren<Animator>();
        lastPos = transform.position;
    }

    void Update()
    {
        switch (currentState)
        {
            case BehaviorState.MoveIn:
                // 화면 안쪽 목표 지점으로 스르륵 이동
                transform.position = Vector2.MoveTowards(transform.position, targetEntryPos, speed * Time.deltaTime);
                
                // 도착했다면?
                if (Vector2.Distance(transform.position, targetEntryPos) < 0.1f && !hasArrived)
                {
                    hasArrived = true;
                    currentState = BehaviorState.Attack; // 멈춰서 공격 시작!
                    StartCoroutine(AttackWaitRoutine());
                }
                break;
                
            case BehaviorState.Attack:
                // 공격 중일 때 플레이어를 향해 천천히 다가감
                if (chaseWhileAttacking && Player.instance != null)
                {
                    transform.position = Vector2.MoveTowards(transform.position, Player.instance.transform.position, attackChaseSpeed * Time.deltaTime);
                }
                break;
                
            case BehaviorState.ChasePlayer:
                // 플레이어를 끈질기게 쫓아가는 모드
                if (Player.instance != null)
                {
                    transform.position = Vector2.MoveTowards(transform.position, Player.instance.transform.position, (speed * 0.6f) * Time.deltaTime);
                }
                break;
                
            case BehaviorState.MoveOut:
                // 할일 다 했으니 화면 밖(위쪽)으로 도망감
                transform.Translate(Vector3.up * speed * Time.deltaTime);
                break;
        }

        // --- 방향 쳐다보기 및 걷기/대기 애니메이션 로직 ---
        Vector3 moveDelta = transform.position - lastPos;
        
        if (moveDelta.magnitude > 0.001f) // 이동 중일 때
        {
            Vector3 dir = moveDelta.normalized;
            if (anim != null)
            {
                anim.SetFloat("InputX", dir.x);
                anim.SetFloat("InputY", dir.y);
                
                // 이동 중이므로 달리기 애니메이션 켜기
                anim.SetBool("isRunning", true);
            }
        }
        else 
        {
            if (anim != null)
            {
                // 멈췄으므로 달리기 애니메이션 끄기 (Idle로 돌아감)
                anim.SetBool("isRunning", false);

                // 멈춰서 공격 중일 때는 플레이어를 쳐다보게 함
                if (currentState == BehaviorState.Attack && Player.instance != null)
                {
                    Vector3 dirToPlayer = (Player.instance.transform.position - transform.position).normalized;
                    anim.SetFloat("InputX", dirToPlayer.x);
                    anim.SetFloat("InputY", dirToPlayer.y);
                }
            }
        }
        
        lastPos = transform.position;
    }

    IEnumerator AttackWaitRoutine()
    {
        // 정해진 시간(attackDuration)만큼 멈춰서 쏩니다.
        yield return new WaitForSeconds(attackDuration);
        
        // 공격 시간이 끝나면 설정해둔 다음 상태(도망가기 or 쫓아가기)로 바꿉니다.
        currentState = afterAttackState;
    }
}

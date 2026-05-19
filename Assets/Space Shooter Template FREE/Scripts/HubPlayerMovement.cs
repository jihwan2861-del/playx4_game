using UnityEngine;

public class HubPlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    
    [Tooltip("true이면 대각선 2.5D 쿼터뷰 축으로 이동하고, false이면 게임씬처럼 직관적인 상하좌우(WASD) 일반 2D로 이동합니다.")]
    public bool useIsometricAxis = false;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            Debug.Log("ℹ️ [HubPlayerMovement] '주인공' 오브젝트에 Rigidbody2D가 없어 스크립트가 자동으로 추가했습니다.");
        }
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public bool canMove = true;

    void Update()
    {
        if (!canMove) 
        {
            movement = Vector2.zero;
            if (anim != null)
            {
                anim.SetBool("isMoving", false);
            }
            return;
        }
        
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // 애니메이터 변수 동기화 (isMoving, InputX, InputY)
        if (anim != null)
        {
            bool isMoving = movement.magnitude > 0.01f;
            anim.SetBool("isMoving", isMoving);
            
            if (isMoving)
            {
                anim.SetFloat("InputX", movement.x);
                anim.SetFloat("InputY", movement.y);
            }
        }
    }

    void FixedUpdate()
    {
        if (!canMove)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }
        
        if (movement.magnitude > 0.1f)
        {
            Vector2 finalMovement = movement;
            if (useIsometricAxis)
            {
                // 쿼터뷰용 대각선 2.5D 축 변환 적용
                finalMovement = new Vector2(movement.x - movement.y, (movement.x + movement.y) * 0.5f);
            }
            
            Vector2 targetVelocity = finalMovement.normalized * moveSpeed;
            
            if (rb != null)
            {
                rb.velocity = targetVelocity;
            }
            else
            {
                transform.position += (Vector3)targetVelocity * Time.fixedDeltaTime;
            }
        }
        else
        {
            if (rb != null) rb.velocity = Vector2.zero;
        }
    }
}

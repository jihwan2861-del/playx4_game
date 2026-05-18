using UnityEngine;

public class HubPlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public bool canMove = true;

    void Update()
    {
        if (!canMove) 
        {
            movement = Vector2.zero;
            return;
        }
        
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
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
            // 쿼터뷰용 대각선 벡터 변환
            Vector2 isoMovement = new Vector2(movement.x - movement.y, (movement.x + movement.y) * 0.5f);
            rb.velocity = isoMovement.normalized * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }
}

using UnityEngine;

public class Bonus : MonoBehaviour {

    [Header("자석(Magnet) 설정")]
    [Tooltip("이 반경 안에 플레이어가 들어오면 아이템이 빨려 들어갑니다.")]
    public float magnetRadius = 5.0f;
    [Tooltip("빨려 들어갈 때의 이동 속도")]
    public float magnetSpeed = 15.0f;

    private bool isMagnetized = false; // 한 번 끌려오기 시작했는지 여부

    private void Update()
    {
        if (Player.instance != null)
        {
            float dist = Vector3.Distance(transform.position, Player.instance.transform.position);
            
            // 처음으로 자석 반경 안에 들어왔을 때
            if (!isMagnetized && dist <= magnetRadius)
            {
                isMagnetized = true;

                // 기존 이동 스크립트(떨어지는 기능)가 있다면 끄기 (덜덜거림 방지)
                var dm = GetComponent<DirectMoving>();
                if (dm != null) dm.enabled = false;
                
                // 물리(Rigidbody) 이동이 있다면 멈추기
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
            }

            // 한 번 자석에 이끌렸다면, 거리가 멀어져도 끝까지 쫓아갑니다! (동방 스타일)
            if (isMagnetized)
            {
                transform.position = Vector3.MoveTowards(transform.position, Player.instance.transform.position, magnetSpeed * Time.deltaTime);

                // 만약 피탄점이 너무 작아서 충돌 처리가 안 되더라도, 0.5 거리 안으로 겹치면 강제 획득 처리!
                if (dist < 0.5f)
                {
                    if (PlayerMoving.instance != null)
                    {
                        PlayerMoving.instance.AddDashCharge();
                    }
                    Destroy(gameObject);
                }
            }
        }
    }

    //when colliding with another object, if another objct is 'Player', sending command to the 'Player'
    private void OnTriggerEnter2D(Collider2D collision) 
    {
        if (collision.tag == "Player") 
        {
            if (PlayerMoving.instance != null)
            {
                PlayerMoving.instance.AddDashCharge();
            }
            Destroy(gameObject);
        }
    }
}

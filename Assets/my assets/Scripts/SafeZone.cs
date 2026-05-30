using UnityEngine;

/// <summary>
/// 모든 버튼이 활성화되면 나타나는 세이프존 스크립트입니다. 
/// 플레이어가 이 안에 있으면 무적이 되며, 일정 시간 버티면 승리합니다.
/// </summary>
public class SafeZone : MonoBehaviour
{
    public float winDelay = 10.0f; // 세이프존에서 버텨야 하는 시간 (10초로 변경)
    private float timer = 0f;
    private bool isPlayerInside = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            
            // Resources에서 'safezone' 가오름
            Sprite loadedSprite = Resources.Load<Sprite>("safezone");
            if (loadedSprite != null)
            {
                sr.sprite = loadedSprite;
            }
            sr.color = new Color(1, 1, 1, 0.5f); // 이미지가 있으므로 적절한 투명도 설정
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 100; // 가장 앞에 보이도록 설정 (적/플레이어보다 높게)
        }

        // 인스펙터나 Resources에 이미지가 전혀 없다면, 코드로 임시 노란색 사각형을 그려줍니다.
        if (sr.sprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            sr.color = new Color(1f, 0.8f, 0.2f, 0.5f); // 반투명 황금색
            
            // 프리팹 크기를 안 키웠을까봐 강제로 큼직하게 설정
            transform.localScale = new Vector3(5f, 5f, 1f);
        }

        // 기존에 만들어둔 BlinkEffect 컴포넌트 추가
        BlinkEffect blink = gameObject.AddComponent<BlinkEffect>();
        if (blink != null) blink.blinkSpeed = 10f; // 깜빡임 속도 조절

        // 총알을 막지 않도록 'Ignore Raycast' 레이어 설정
        gameObject.layer = 2;

        // 콜라이더가 없으면 원형 콜라이더 추가 (기본 세팅만 수행)
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (Player.instance != null) Player.instance.safeZoneInvincible = true; // 무적 활성화
            Debug.Log("[SafeZone] 플레이어 보호 시작! 클리어 카운트다운 진입!");
            
            // 들어가자마자 바로(혹은 짧은 딜레이 후) 클리어되도록 변경
            TriggerVictory();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
            if (Player.instance != null) Player.instance.safeZoneInvincible = false;
        }
    }

    private void Update()
    {
        // 기존의 버티기 로직은 제거 (들어가면 바로 클리어되므로)
    }

    void TriggerVictory()
    {
        isPlayerInside = false; // 중복 호출 방지
        Debug.Log("🏆 [VICTORY] 스테이지 클리어! 곧 마을로 귀환합니다.");
        
        if (PlayerUI.instance != null)
        {
            PlayerUI.instance.ShowVictory();
        }

        // 클리어 연출: 시간이 아예 멈추지 않고 영화처럼 1/10 속도로 아주 느리게 흘러감
        Time.timeScale = 0.1f; 

        // 3초 뒤에 마을로 돌아가는 코루틴 실행
        StartCoroutine(ReturnToVillageRoutine());
    }

    private System.Collections.IEnumerator ReturnToVillageRoutine()
    {
        // 현실 시간 기준으로 3초 대기 (게임 내 시간은 느려졌지만 현실 시간은 그대로 감)
        yield return new WaitForSecondsRealtime(3f);
        
        // 씬을 넘어가기 전에 무조건 시간을 원래대로 되돌려야 함
        Time.timeScale = 1f; 
        
        // 마을 씬으로 자동 귀환 (Village_Scene -> Hub_Scene으로 업데이트)
        UnityEngine.SceneManagement.SceneManager.LoadScene("Hub_Scene");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script defines the size of the ‘Boundary’ depending on Viewport. When objects go beyond the ‘Boundary’, they are destroyed or deactivated.
/// </summary>
public class Boundary : MonoBehaviour {

    BoxCollider2D boundareCollider;

    //receiving collider's component and changing boundary borders
    private void Start()
    {
        boundareCollider = GetComponent<BoxCollider2D>();
        ResizeCollider();
    }

    //changing the collider's size up to Viewport's size multiply 1.5
    void ResizeCollider() 
    {        
        // 카메라의 위치와 무관하게 정확한 화면 크기를 구하도록 수정
        Vector2 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 topRight = Camera.main.ViewportToWorldPoint(new Vector2(1, 1));
        Vector2 viewportSize = topRight - bottomLeft;
        
        viewportSize.x *= 1.5f;
        viewportSize.y *= 1.5f;
        boundareCollider.size = viewportSize;
    }

    private void LateUpdate()
    {
        // 카메라가 이동할 때마다 Boundary도 카메라를 따라다니도록 위치 업데이트
        if (Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
        }
    }

    //when another object leaves collider
    private void OnTriggerExit2D(Collider2D collision) 
    {        
        if (collision.tag == "Projectile")
        {
            collision.gameObject.SetActive(false); // 풀링을 위해 비활성화
        }
        else if (collision.tag == "Bonus") 
        {
            Destroy(collision.gameObject);
        }
        else
        {
            // 태그가 없더라도 Projectile 컴포넌트가 있으면 풀링 반환 (코드 생성 탄환 대응)
            Projectile proj = collision.GetComponent<Projectile>();
            if (proj != null)
            {
                collision.gameObject.SetActive(false);
            }
        }
    }

}

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
        Vector2 viewportSize = Camera.main.ViewportToWorldPoint(new Vector2(1, 1)) * 2;
        viewportSize.x *= 1.5f;
        viewportSize.y *= 1.5f;
        boundareCollider.size = viewportSize;
    }

    private void Update()
    {
        if (Camera.main != null)
        {
            // 바운더리가 항상 카메라(화면 중앙)를 따라다니도록 위치 업데이트
            transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, transform.position.z);
        }
    }

    //when another object leaves collider
    private void OnTriggerExit2D(Collider2D collision) 
    {        
        if (collision.tag == "Projectile")
        {
            collision.gameObject.SetActive(false); // 풀링을 위해 파괴 대신 비활성화
        }
        else if (collision.tag == "Bonus") 
            Destroy(collision.gameObject); 
    }

}

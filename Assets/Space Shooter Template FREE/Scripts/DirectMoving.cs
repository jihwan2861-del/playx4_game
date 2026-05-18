using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script moves the attached object along the Y-axis with the defined speed
/// </summary>
public class DirectMoving : MonoBehaviour {

    [Tooltip("이동 속도 (음수면 아래로 이동)")]
    public float speed;

    [Header("Homing Settings")]
    [Tooltip("체크 시 플레이어를 따라가는 유도탄이 됩니다")]
    public bool isHoming = false;
    
    [Tooltip("유도 회전 속도 (클수록 더 예리하게 꺾임)")]
    public float homingRotSpeed = 120f;

    [Tooltip("유도 기능이 유지되는 시간(초)")]
    public float homingDuration = 2f;
    private float homingTimer = 0f;

    [Tooltip("스프라이트가 향하는 방향과 실제 이동 방향이 다를 때의 시각적 보정 각도 (예: 수리검 90 또는 -90)")]
    public float visualAngleOffset = 0f;

    [Header("조준 설정 (Aim)")]
    [Tooltip("체크 시 스폰되자마자 플레이어가 있는 곳을 조준해서 날아갑니다 (직선)")]
    public bool aimAtPlayerOnStart = false;

    private void Start()
    {
        homingTimer = homingDuration;

        // 태어나자마자 플레이어 쪽으로 한 번 각도를 확 틀어버리기 (저격)
        if (aimAtPlayerOnStart && Player.instance != null)
        {
            Vector2 direction = (Vector2)Player.instance.transform.position - (Vector2)transform.position;
            direction.Normalize();
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f + visualAngleOffset;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // 음수 속도라면 양수로 바꿔서 앞쪽으로 제대로 날아가게 보정
            if (speed < 0) speed = Mathf.Abs(speed);
        }
        else if (isHoming && speed < 0)
        {
            // 기존 호밍 기능 보정
            speed = Mathf.Abs(speed);
            transform.Rotate(0, 0, 180f);
        }
    }

    //moving the object with the defined speed
    private void Update()
    {
        // 타이머가 0보다 클 때만 유도 기능 작동
        if (isHoming && Player.instance != null && homingTimer > 0f)
        {
            homingTimer -= Time.deltaTime; // 남은 시간 틱다운

            Vector2 direction = (Vector2)Player.instance.transform.position - (Vector2)transform.position;
            direction.Normalize();
            
            // 스프라이트는 머리(위쪽)를 향하고 있다고 가정 (-90 보정)
            // visualAngleOffset을 더해서 눈에 보이는 스프라이트의 각도를 보정합니다.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f + visualAngleOffset;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            
            // 서서히 목표 각도로 회전
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, homingRotSpeed * Time.deltaTime);
        }

        // 시각적 보정 각도(visualAngleOffset)를 제외한 실제 '앞(Up)' 방향을 계산하여 이동
        float moveAngle = transform.eulerAngles.z - visualAngleOffset;
        Vector3 moveDir = new Vector3(Mathf.Cos((moveAngle + 90f) * Mathf.Deg2Rad), Mathf.Sin((moveAngle + 90f) * Mathf.Deg2Rad), 0);
        
        transform.position += moveDir * speed * Time.deltaTime; 
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Truck : MonoBehaviour
{
    [SerializeField] private float moveSpeed;               // 기본 이동 속도
    [SerializeField] private float decreaseSpeedPerMonster; // 몬스터당 속도 감소 량

    private Rigidbody2D rb;

    private float currentSpeed;     // 현재 속도
    private float finalSpeed;       // 최종 속도

    public float SpeedRatio => Mathf.Clamp01(currentSpeed / moveSpeed);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        currentSpeed = moveSpeed;   // 현재 속도 초기화
    }

    private void FixedUpdate()
    {
        // currentSpeed가 0이하면 
        if (currentSpeed <= 0)
            finalSpeed = 0;             // 최종 속도를 0으로 고정
        else
            finalSpeed = currentSpeed;  // 최종 속도를 currentSpeed로 설정

        // 최종 속도에 따라 오른쪽으로 이동
        rb.velocity = Vector3.right * finalSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 몬스터가 트리거에 들어오면 currentSpeed를 감소
        if (other.CompareTag(Define.MonsterTag))
            currentSpeed -= decreaseSpeedPerMonster;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 몬스터가 트리거에서 벗어나면 currentSpeed를 증가
        if (other.CompareTag(Define.MonsterTag))
            currentSpeed += decreaseSpeedPerMonster;
    }
}

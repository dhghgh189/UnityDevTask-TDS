using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

public class Monster : MonoBehaviour
{
    // 몬스터의 상태 정의
    public enum EMonsterState { Move, Attack, Dead }

    [SerializeField] private float moveSpeed;           // 이동 속도
    [SerializeField] private float raycastRange;        // 레이캐스트 사거리
    [SerializeField] private LayerMask whatIsTarget;    // 레이캐스트 타겟을 구분하기 위한 layerMask 
    [SerializeField] private Transform rayOrigin;       // 레이캐스트 시작점 트랜스폼
    [SerializeField] private float jumpInterval;        // 점프 쿨타임
    [SerializeField] private float jumpForce;           // 점프 힘
    private EMonsterState curState;                     // 현재 상태
    private Animator anim;                              // Animator 캐시
    private Rigidbody2D rb;                             // Rigidbody2D 캐시
    private CapsuleCollider2D coll;                     // CapsuleCollider2D 캐시

    private int isAttackingHash = Animator.StringToHash("IsAttacking");
    private int isDeadHash = Animator.StringToHash("IsDead");

    private Vector3 leftRayOrigin;                      // 왼쪽에 위치한 물체를 레이캐스트하기 위한 원점
    private Vector3 groundRayOrigin;                    // 아래에 위치한 물체를 레이캐스트하기 위한 원점
    private RaycastHit2D hit;
    private RaycastHit2D groundHit;

    private bool isJump;
    private float nextJumpTime;

    [SerializeField] private bool isGrounded;

    private void Awake()
    {
        // 상태 초기화
        curState = EMonsterState.Move;
        // Animator 컴포넌트 캐시
        anim = GetComponent<Animator>();
        // Rigidbody2D 컴포넌트 캐시
        rb = GetComponent<Rigidbody2D>();
        // CapsuleCollider2D 컴포넌트 캐시
        coll = GetComponent<CapsuleCollider2D>();

        leftRayOrigin = Vector3.left * (coll.size.x * 0.55f);
        groundRayOrigin = Vector3.down * (coll.size.y * 0.501f);
    }

    private void FixedUpdate()
    {
        //if (curState == EMonsterState.Move)
        //{
        //    // 고정된 주기마다 왼쪽으로 이동
        //    //rb.velocity = Vector2.left* moveSpeed;
        //    rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
        //}
        //else
        //{
        //    rb.velocity = new Vector2(0, rb.velocity.y);
        //}

        if (hit.collider == null)
        {
            rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
        }
        else
        {
            if (!hit.collider.CompareTag("Monster") || hit.collider.gameObject.layer == gameObject.layer)
                rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    void Update()
    {
        // 상태 패턴 처리
        switch (curState)
        {
            case EMonsterState.Move:    // 이동 상태
                UpdateMove();
                break;
            case EMonsterState.Attack:  // 공격 상태
                UpdateAttack();
                break;
        }

        // 애니메이션 상태 관리
        UpdateAnim();

        // 땅 체크
        CheckGround();
    }

    protected virtual void UpdateAnim()
    {
        anim.SetBool(isAttackingHash, curState == EMonsterState.Attack);
        anim.SetBool(isDeadHash, curState == EMonsterState.Dead);
    }

    protected virtual void UpdateMove()
    {
        // 감지되는 물체가 있으면 Attack 상태로 변경 (임시)
        Debug.DrawRay(rayOrigin.position + leftRayOrigin, Vector2.left * raycastRange, Color.red);
        hit = Physics2D.Raycast(rayOrigin.position + leftRayOrigin, Vector2.left, raycastRange, whatIsTarget);

        if (hit.collider == null)
            return;

        if (!hit.collider.CompareTag("Monster"))
        {
            curState = EMonsterState.Attack;
            return;
        }

        if (hit.collider.gameObject.layer == gameObject.layer 
            && isGrounded
            && Time.time >= nextJumpTime)
        {
            Jump();
        }
    }

    protected virtual void UpdateAttack()
    {
        // 감지되는 물체가 없으면 Move 상태로 변경
        Debug.DrawRay(rayOrigin.position + leftRayOrigin, Vector2.left * raycastRange, Color.red);
        hit = Physics2D.Raycast(rayOrigin.position + leftRayOrigin, Vector2.left, raycastRange, whatIsTarget);
        if (hit.collider == null)
        {
            curState = EMonsterState.Move;
            return;
        }
    }

    private void Jump()
    {
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        nextJumpTime = Time.time + jumpInterval;
    }

    private void CheckGround()
    {
        Debug.DrawRay(rayOrigin.position + groundRayOrigin, Vector2.down * raycastRange, Color.blue);
        groundHit = Physics2D.Raycast(rayOrigin.position + groundRayOrigin, Vector2.down, raycastRange, 1 << gameObject.layer);
        isGrounded = groundHit.collider != null;
    }

    protected virtual void OnAttack()
    {

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"{gameObject.name}가 {collision.gameObject.name}에 부딪힘");
    }
}

using System.Collections;
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

    private Vector3 leftRayOffset;                      // 왼쪽에 위치한 물체를 레이캐스트하기 위한 원점 offset
    private Vector3 groundRayOffset;                    // 아래에 위치한 물체를 레이캐스트하기 위한 원점 offset

    private RaycastHit2D hit;
    private RaycastHit2D groundHit;

    private float nextJumpTime;

    [SerializeField] private bool isGrounded;

    private void Awake()
    {
        // 초기 상태 = move
        curState = EMonsterState.Move;

        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CapsuleCollider2D>();

        // 각 방향의 레이캐스트 진행을 위한 원점 offset
        leftRayOffset = Vector3.left * (coll.size.x * 0.55f);
        groundRayOffset = Vector3.down * (coll.size.y * 0.501f);
    }

    private void FixedUpdate()
    {
        // 점프 시간 중 이동 처리 x
        // 점프 도중 이동하여 아래 몬스터의 경계면에 비벼지면서 가속을 받는 문제 방지 
        if (Time.time < nextJumpTime)
            return;

        if (hit.collider == null)
            rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
        else
            rb.velocity = new Vector2(0, rb.velocity.y);
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
        Debug.DrawRay(rayOrigin.position + leftRayOffset, Vector2.left * raycastRange, Color.red);
        hit = Physics2D.Raycast(rayOrigin.position + leftRayOffset, Vector2.left, raycastRange, whatIsTarget);

        if (hit.collider == null)
            return;

        if (!hit.collider.CompareTag("Monster"))
        {
            curState = EMonsterState.Attack;
            return;
        }

        if (isGrounded && Time.time >= nextJumpTime)
        {
            Jump();
        }
    }

    protected virtual void UpdateAttack()
    {
        // 감지되는 물체가 없으면 Move 상태로 변경
        Debug.DrawRay(rayOrigin.position + leftRayOffset, Vector2.left * raycastRange, Color.red);
        hit = Physics2D.Raycast(rayOrigin.position + leftRayOffset, Vector2.left, raycastRange, whatIsTarget);
        if (hit.collider == null)
        {
            curState = EMonsterState.Move;
            return;
        }
    }

    private void Jump()
    {
        // 점프 직전 속도를 초기화한다.
        // 다른 몬스터의 움직임과 겹치는 경우에도 정상적으로 점프 할 수 있도록 하기 위함
        rb.velocity = Vector2.zero;

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        nextJumpTime = Time.time + jumpInterval;
    }

    private void CheckGround()
    {
        Debug.DrawRay(rayOrigin.position + groundRayOffset, Vector2.down * raycastRange, Color.blue);
        groundHit = Physics2D.Raycast(rayOrigin.position + groundRayOffset, Vector2.down, raycastRange, 1 << gameObject.layer);
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

using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class Monster : MonoBehaviour
{
    // 몬스터의 상태 정의
    public enum EMonsterState { Move, Attack, Dead }

    [SerializeField] private float moveSpeed;           // 이동 속도
    [SerializeField] private float raycastRange;        // 레이캐스트 사거리
    [SerializeField] private LayerMask whatIsTarget;    // 레이캐스트 타겟을 구분하기 위한 layerMask 
    [SerializeField] private Transform rayOrigin;       // 레이캐스트 시작점 트랜스폼
    private EMonsterState curState;                     // 현재 상태
    private Animator anim;                              // Animator 캐시
    private Rigidbody2D rb;                             // Rigidbody2D 캐시

    private int isAttackingHash = Animator.StringToHash("IsAttacking");
    private int isDeadHash = Animator.StringToHash("IsDead");

    private RaycastHit2D hit;

    private void Awake()
    {
        // 상태 초기화
        curState = EMonsterState.Move;
        // Animator 컴포넌트 캐시
        anim = GetComponent<Animator>();
        // Rigidbody2D 컴포넌트 캐시
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (curState == EMonsterState.Move)
        {
            // 고정된 주기마다 왼쪽으로 이동
            rb.velocity = Vector2.left * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
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
    }

    protected virtual void UpdateAnim()
    {
        anim.SetBool(isAttackingHash, curState == EMonsterState.Attack);
        anim.SetBool(isDeadHash, curState == EMonsterState.Dead);
    }

    protected virtual void UpdateMove()
    {
        // 감지되는 물체가 있으면 Attack 상태로 변경 (임시)
        Debug.DrawRay(rayOrigin.position, Vector2.left * raycastRange, Color.red);
        hit = Physics2D.Raycast(rayOrigin.position, Vector2.left, raycastRange, whatIsTarget);
        if (hit.collider != null)
        {
            curState = EMonsterState.Attack;
            return;
        }
    }

    protected virtual void UpdateAttack()
    {
        // 감지되는 물체가 없으면 Move 상태로 변경
        Debug.DrawRay(rayOrigin.position, Vector2.left * raycastRange, Color.red);
        hit = Physics2D.Raycast(rayOrigin.position, Vector2.left, raycastRange, whatIsTarget);
        if (hit.collider == null)
        {
            curState = EMonsterState.Move;
            return;
        }
    }

    protected virtual void OnAttack()
    {

    }
}

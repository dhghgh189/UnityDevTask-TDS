using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class Monster : MonoBehaviour
{
    // 몬스터의 상태 정의
    public enum EMonsterState { Move, Attack, Dead }

    [SerializeField] private float moveSpeed;       // 이동 속도
    private EMonsterState curState;                 // 현재 상태
    private Animator anim;                          // Animator 캐시

    private int isAttackingHash = Animator.StringToHash("IsAttacking");
    private int isDeadHash = Animator.StringToHash("IsDead");

    private void Awake()
    {
        // 상태 초기화
        curState = EMonsterState.Move;
        // Animator 컴포넌트 캐시
        anim = GetComponent<Animator>();
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
        // 매 프레임 왼쪽으로 이동 
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
    }

    protected virtual void UpdateAttack()
    {

    }
}

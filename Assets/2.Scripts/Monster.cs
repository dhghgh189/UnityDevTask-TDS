using System.Collections;
using UnityEngine;

public class Monster : MonoBehaviour
{
    // 몬스터의 상태 정의
    public enum EMonsterState { Move, Attack, Dead }

    [SerializeField] private float moveSpeed;           // 이동 속도
    [SerializeField] private float raycastRange;        // 레이캐스트 사거리
    [SerializeField] private float headCheckRange;      // 머리 체크 사거리
    [SerializeField] private LayerMask whatIsTarget;    // 레이캐스트 타겟을 구분하기 위한 layerMask 
    [SerializeField] private Transform rayOrigin;       // 레이캐스트 시작점 트랜스폼
    [SerializeField] private float jumpInterval;        // 점프 쿨타임
    [SerializeField] private float jumpForce;           // 점프 힘
    [SerializeField] private float backstepTime;        // 백 스텝 진행시간
    [SerializeField] private float backstepSpeed;       // 백 스텝 이동 속도

    private EMonsterState curState;                     // 현재 상태
    private Animator anim;                              // Animator 캐시
    private Rigidbody2D rb;                             // Rigidbody2D 캐시
    private CapsuleCollider2D coll;                     // CapsuleCollider2D 캐시
    private SpriteRenderer[] renderers;                 // Sorting Layer 설정을 위한 Sprite Renderer 캐시

    // 애니메이션 파라미터 ID
    private int isAttackingHash = Animator.StringToHash("IsAttacking");
    private int isDeadHash = Animator.StringToHash("IsDead");

    private Vector3 leftRayOffset;                      // 왼쪽에 위치한 물체를 레이캐스트하기 위한 원점 offset
    private Vector3 groundRayOffset;                    // 아래에 위치한 물체를 레이캐스트하기 위한 원점 offset
    private Vector3 headRayOffset;                      // 머리에 위치한 물체를 레이캐스트하기 위한 원점 offset

    private RaycastHit2D leftHit;
    private RaycastHit2D groundHit;
    private RaycastHit2D headHit;

    private float nextJumpTime;                         // 다음 점프 가능 시간

    [SerializeField] private bool isGrounded;

    private Coroutine backstepRoutine;                  // 백스텝 실행 여부를 확인할 코루틴 변수
    private WaitForSeconds wsBackstepReadyTime;         // 백스텝 시작 전 대기 시간
    private WaitForSeconds wsBackstepTime;              // 백스텝 진행 시간

    private Pool<Monster> returnPool;            // 반납을 위한 풀 참조
    public Pool<Monster> ReturnPool { set { returnPool = value; } }

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<CapsuleCollider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();

        // 각 방향의 레이캐스트 진행을 위한 원점 offset
        leftRayOffset = Vector3.left * (coll.size.x * 0.501f);
        groundRayOffset = Vector3.down * (coll.size.y * 0.501f);
        headRayOffset = Vector3.up * (coll.size.y * 0.501f);

        // 코루틴에서 사용할 WaitForSeconds 객체
        wsBackstepReadyTime = new WaitForSeconds(0.1f);
        wsBackstepTime = new WaitForSeconds(backstepTime);
    }

    private void OnEnable()
    {
        // 초기 상태 = move
        curState = EMonsterState.Move;
        nextJumpTime = 0;
    }

    private void OnDisable()
    {
        if (backstepRoutine != null)
            backstepRoutine = null;
    }

    private void FixedUpdate()
    {
        // 백 스텝 중 이동 처리 x
        if (backstepRoutine != null)
            return;

        // 점프 시간 중 이동 처리 x
        // 점프 도중 이동하여 아래 몬스터의 경계면에 비벼지면서 가속을 받는 문제 방지 
        if (Time.time < nextJumpTime)
            return;

        if (leftHit.collider == null)
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

        // 전방 체크
        CheckLeft();

        // 자신이 맨 앞 오브젝트인 경우에만 머리 체크
        if (leftHit.collider != null && !leftHit.collider.CompareTag(Define.MonsterTag))
        {
            // 머리 체크
            CheckHead();
        }

        // 땅 체크
        CheckGround();
    }

    protected virtual void UpdateAnim()
    {
        // 상태에 따른 애니메이션 파라미터 설정
        anim.SetBool(isAttackingHash, curState == EMonsterState.Attack);
        anim.SetBool(isDeadHash, curState == EMonsterState.Dead);
    }

    protected virtual void UpdateMove()
    {
        if (leftHit.collider == null)
            return;

        // 몬스터가 아닌 물체가 감지되면 공격
        if (!leftHit.collider.CompareTag(Define.MonsterTag))
        {
            curState = EMonsterState.Attack;
            return;
        }

        // 현재 땅에 있는 경우 점프 가능 상태라면
        if (isGrounded && Time.time >= nextJumpTime)
        {
            // 확률로 점프
            if (Random.value < 0.5f)
                Jump();

            // 다음 점프 가능 시간 갱신
            nextJumpTime = Time.time + jumpInterval;
        }
    }

    protected virtual void UpdateAttack()
    {
        // 공격 대상이 감지되지 않으면 Move 상태로 변경
        if (leftHit.collider == null || leftHit.collider.CompareTag(Define.MonsterTag))
        {
            curState = EMonsterState.Move;
            return;
        }
    }

    // 몬스터의 Lane 설정 및 필요한 초기화 수행
    public void SetLane(int iLane)
    {
        // 레이어 설정
        gameObject.layer = LayerMask.NameToLayer($"Lane{iLane}");
        // target layermask 설정
        whatIsTarget = (1 << LayerMask.NameToLayer("Box")) | (1 << gameObject.layer);
        // sorting layer 설정
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingLayerID = Define.LaneSortingLayerID[iLane];
    }

    private void Jump()
    {
        // 점프 직전 속도를 초기화한다.
        // 다른 몬스터의 움직임과 겹치는 경우에도 정상적으로 점프 할 수 있도록 하기 위함
        rb.velocity = Vector2.zero;

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void CheckLeft()
    {
#if UNITY_EDITOR
        Debug.DrawRay(rayOrigin.position + leftRayOffset, Vector2.left * raycastRange, Color.red);
#endif
        leftHit = Physics2D.Raycast(rayOrigin.position + leftRayOffset, Vector2.left, raycastRange, whatIsTarget);
    }

    private void CheckHead()
    {
#if UNITY_EDITOR
        Debug.DrawRay(rayOrigin.position + headRayOffset, Vector2.up * headCheckRange, Color.cyan);
#endif
        headHit = Physics2D.Raycast(rayOrigin.position + headRayOffset, Vector2.up, headCheckRange, 1 << gameObject.layer);

        if (headHit.collider == null || !headHit.collider.CompareTag(Define.MonsterTag))
            return;

        // 백스텝 중이 아니라면
        if (backstepRoutine == null)
        {
            backstepRoutine = StartCoroutine(BackstepRoutine());    // 백스텝 코루틴 실행
        }
    }

    private IEnumerator BackstepRoutine()
    {
        // 위에 있는 몬스터가 완전히 착지하기 까지 여유시간을 준다.
        yield return wsBackstepReadyTime;
        rb.velocity = new Vector2(backstepSpeed, rb.velocity.y);    // 백 스텝

        // 백스텝 진행 시간동안 대기
        yield return wsBackstepTime;
        rb.velocity = new Vector2(0, rb.velocity.y);    // 속도 초기화

        // 백스텝 종료 후 한번 더 대기 
        yield return wsBackstepTime;

        // 코루틴 끝
        backstepRoutine = null;
    }

    private void CheckGround()
    {
#if UNITY_EDITOR
        Debug.DrawRay(rayOrigin.position + groundRayOffset, Vector2.down * raycastRange, Color.blue);
#endif
        groundHit = Physics2D.Raycast(rayOrigin.position + groundRayOffset, Vector2.down, raycastRange, 1 << gameObject.layer);
        isGrounded = groundHit.collider != null;
    }

    protected virtual void OnAttack()
    {

    }
}

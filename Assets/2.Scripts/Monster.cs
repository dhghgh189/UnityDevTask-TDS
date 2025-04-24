using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

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

    private EMonsterState curState;                     // 현재 상태
    private Animator anim;                              // Animator 캐시
    private Rigidbody2D rb;                             // Rigidbody2D 캐시
    private CapsuleCollider2D coll;                     // CapsuleCollider2D 캐시
    private SpriteRenderer[] renderers;                 // Sorting Layer 설정을 위한 Sprite Renderer 캐시

    private int isAttackingHash = Animator.StringToHash("IsAttacking");
    private int isDeadHash = Animator.StringToHash("IsDead");

    private Vector3 leftRayOffset;                      // 왼쪽에 위치한 물체를 레이캐스트하기 위한 원점 offset
    private Vector3 groundRayOffset;                    // 아래에 위치한 물체를 레이캐스트하기 위한 원점 offset
    private Vector3 headRayOffset;                      // 머리에 위치한 물체를 레이캐스트하기 위한 원점 offset

    private RaycastHit2D hit;
    private RaycastHit2D groundHit;
    private RaycastHit2D headHit;

    private float nextJumpTime;

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
        leftRayOffset = Vector3.left * (coll.size.x * 0.55f);
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

        // 머리 체크
        CheckHead();

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
        nextJumpTime = Time.time + jumpInterval;
    }

    private void CheckHead()
    {
        Debug.DrawRay(rayOrigin.position + headRayOffset, Vector2.up * headCheckRange, Color.cyan);
        headHit = Physics2D.Raycast(rayOrigin.position + headRayOffset, Vector2.up, headCheckRange, 1 << gameObject.layer);

        if (headHit.collider == null)
            return;

        // 머리 위에 몬스터가 있고 백스텝 중이 아니라면 
        if (headHit.collider.CompareTag("Monster") && backstepRoutine == null)
            backstepRoutine = StartCoroutine(BackstepRoutine());    // 백스텝 코루틴 실행
    }

    private IEnumerator BackstepRoutine()
    {
        // 위에 있는 몬스터가 완전히 착지하기 까지 여유시간을 준다.
        yield return wsBackstepReadyTime;
        rb.velocity = new Vector2(moveSpeed, rb.velocity.y);    // 백 스텝

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
        Debug.DrawRay(rayOrigin.position + groundRayOffset, Vector2.down * raycastRange, Color.blue);
        groundHit = Physics2D.Raycast(rayOrigin.position + groundRayOffset, Vector2.down, raycastRange, 1 << gameObject.layer);
        isGrounded = groundHit.collider != null;
    }

    protected virtual void OnAttack()
    {

    }
}

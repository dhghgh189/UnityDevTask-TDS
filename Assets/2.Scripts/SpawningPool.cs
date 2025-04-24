using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawningPool : MonoBehaviour
{
    [SerializeField] private Monster monsterPrefab;     // 몬스터 프리팹
    [SerializeField] private float spawnInterval;       // 몬스터 스폰 간격
    [SerializeField] private int maxCount;              // 최대 스폰 수
    [SerializeField] private float xOffset;             // truck으로부터 xOffset 만큼 떨어진 위치에 몬스터를 스폰
    [SerializeField] private float[] yPosPerLane;       // 각 Lane별 y 스폰 위치 지정

    private Pool<Monster> monsterPool;                  // 몬스터 인스턴스를 저장하는 풀
    private int monsterCount;                           // 현재 존재하는 몬스터 수

    private Coroutine spawnRoutine;
    private WaitForSeconds wsSpawnInterval;             // 코루틴에서 사용할 WaitForSecons

    private Transform truckTransform;                   // 플레이어 트럭 위치 확인용

    void Awake()
    {
        monsterPool = new Pool<Monster>(monsterPrefab);
        wsSpawnInterval = new WaitForSeconds(spawnInterval);
    }

    void Start()
    {
        truckTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // 몬스터 스폰 코루틴 시작
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        int iLane;

        while (true)
        {
            // 몬스터 수가 최대로 스폰된 경우 
            if (monsterCount >= maxCount)
            {
                yield return null;
            }
            else
            {
                // Spawn할 Lane 선정
                iLane = Random.Range(0, (int)Define.ELane.Length);

                // 스폰 위치 선정
                Vector3 spawnPos = new Vector3(truckTransform.position.x + xOffset, yPosPerLane[iLane], 0);

                // 몬스터 스폰
                Monster monster = monsterPool.Get();
                monster.SetLane(iLane);                     // Lane 설정
                monster.transform.position = spawnPos;      // 위치 설정

                // 몬스터 카운트 증가
                monsterCount++;

                // 생성 완료 후 대기
                yield return wsSpawnInterval;
            }
        }
    }
}

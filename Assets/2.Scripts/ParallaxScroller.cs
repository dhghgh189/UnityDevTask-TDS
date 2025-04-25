using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxScroller : MonoBehaviour
{
    [SerializeField] private List<ScrollInfo> scrollInfoList;   // 스크롤 할 오브젝트의 정보들
    [SerializeField] private float gap;     // camera가 layer 오브젝트를 벗어나는 경우의 간격
    [SerializeField] private float offset;  // layer 오브젝트를 앞당기기 위한 offset

    private Camera mainCam;     // 카메라 컴포넌트 캐시
    private float currentGap;   // layer 오브젝트와 카메라 간의 간격 저장

    private Truck truck;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Start()
    {
        truck = GameObject.FindGameObjectWithTag(Define.TruckTag).GetComponent<Truck>();
    }

    private void Update()
    {
        // 스크롤 정보 순회
        foreach (var info in scrollInfoList)
        {
            // 현재 layer 오브젝트를 왼쪽으로 이동 (스크롤)
            info.layer.position += Vector3.left * (info.scrollSpeed * truck.SpeedRatio) * Time.deltaTime;

            // layer 오브젝트와 카메라의 x값 차이를 확인
            currentGap = Mathf.Abs(mainCam.transform.position.x - info.layer.position.x);

            // layer 오브젝트와 카메라의 x값 차이가 gap 보다 크다면
            if (currentGap > gap)
                info.layer.position += Vector3.right * offset; // 오른쪽으로 offset 만큼 당긴다.
        }
    }
}

// 스크롤 대상이 되는 layer 오브젝트와 스크롤 속도를 설정
// (직렬화하여 인스펙터에서 조작)
[System.Serializable]
public class ScrollInfo
{
    public Transform layer;
    public float scrollSpeed;
}
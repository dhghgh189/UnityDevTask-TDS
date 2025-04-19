using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Truck : MonoBehaviour
{
    [SerializeField] private float moveSpeed;

    private void Update()
    {
        // 매 프레임 오른쪽으로 이동  
        transform.position += Vector3.right * moveSpeed * Time.deltaTime;
    }
}

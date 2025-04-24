using UnityEngine;
using UnityEngine.Pool;

public class Pool<T> where T : Component
{
    private T original;             // 원본 오브젝트
    private IObjectPool<T> pool;    // Unity에서 제공하는 Object Pool 타입

    public Pool(T original)
    {
        // 원본 참조
        this.original = original;
        // Object Pool 생성 후 참조 (콜백함수 등록)
        pool = new ObjectPool<T>(OnCreate, OnGet, OnRelease, OnDestroy);
    }

    // 오브젝트를 풀에서 꺼낸다
    public T Get()
    {
        return pool.Get();
    }

    // 오브젝트를 풀로 반납
    public void Release(T instance)
    {
        pool.Release(instance);
    }

    #region Callbacks
    // 오브젝트 생성
    private T OnCreate()
    {
        T instance = Object.Instantiate(original);
        return instance;
    }

    // 오브젝트 꺼낼 시
    private void OnGet(T instance)
    {
        instance.gameObject.SetActive(true);
    }

    // 오브젝트 반납 시
    private void OnRelease(T instance)
    {
        instance.gameObject.SetActive(false);
    }

    // 오브젝트 파괴 시
    private void OnDestroy(T instance)
    {
        Object.Destroy(instance.gameObject);
    }
    #endregion
}

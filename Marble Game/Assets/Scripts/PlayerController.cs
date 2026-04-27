using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    
    // GameManager의 이동 이벤트를 구독하여 이동 시작
    void Start()
    {
        GameManager.Instance.OnPlayerMoved += MoveToTarget;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerMoved -= MoveToTarget;
    }

    void MoveToTarget(int pIdx, int tileIndex)
    {
        // 내 인덱스인지 확인
        if (pIdx != GetComponent<PlayerIdentity>().index) return;

        Vector3 targetPos = BoardManager.Instance.GetTilePosition(tileIndex);
        StartCoroutine(MoveRoutine(targetPos));
    }

    IEnumerator MoveRoutine(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target; // 오차 보정
    }
}
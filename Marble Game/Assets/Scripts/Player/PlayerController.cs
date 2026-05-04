using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public int playerIndex = 0;
    public float moveSpeed = 12f;    
    public float hopHeight = 0.6f;    
    public float baseY = 0.4f;

    private bool isMoving = false;
    private Queue<int> _tileIndexQueue = new Queue<int>(); 
    private Transform[] _tileCache;    

    void Start()
    {
        // 게임 시작시 타일들 저장
        CacheTiles(); 
        StartCoroutine(SubscribeWhenReady());
    }

    // 씬에 있는 타일 찾아서 한번만 저장 (성능 최적화)
    void CacheTiles()
    {
        _tileCache = new Transform[40]; // 전체 타일 수
        for (int i = 0; i < 40; i++)
        {
            string tName = "Tile_" + i.ToString("D2");
            GameObject go = GameObject.Find(tName);
            if (go == null) go = GameObject.Find("Tile_" + i);
            
            if (go != null) _tileCache[i] = go.transform;
        }
    }

    IEnumerator SubscribeWhenReady()
    {
        while (GameManager.Instance == null) yield return null;
        GameManager.Instance.OnPlayerMoved += OnPlayerMoved;
    }

    private void OnPlayerMoved(int pIdx, int tileIndex)
    {
        if (pIdx != playerIndex) return;

        _tileIndexQueue.Enqueue(tileIndex);

        if (!isMoving)
        {
            StartCoroutine(ProcessMoveQueue());
        }
    }

    private IEnumerator ProcessMoveQueue()
    {
        isMoving = true;

        while (_tileIndexQueue.Count > 0)
        {
            int nextTileIdx = _tileIndexQueue.Dequeue();
            Vector3 targetPos = CalculateTargetPos(nextTileIdx);
            
            // 한 칸 이동 완료될 때까지 대기
            yield return StartCoroutine(HopTo(targetPos));
        }

        isMoving = false;
    }

    private IEnumerator HopTo(Vector3 destination)
    {
        Vector3 startPos = transform.position;
        float dist = Vector3.Distance(startPos, destination);
        
        float duration = Mathf.Clamp(dist / moveSpeed, 0.1f, 0.25f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 수평 이동
            Vector3 currentPos = Vector3.Lerp(startPos, destination, t);
            // 수직 점프 (포물선)
            currentPos.y += hopHeight * Mathf.Sin(t * Mathf.PI);

            transform.position = currentPos;
            yield return null;
        }

        transform.position = destination;
    }

    private Vector3 CalculateTargetPos(int tileIdx)
    {
        if (_tileCache[tileIdx] != null)
        {
            Vector3 offset = GetPlayerOffset(playerIndex);
            return _tileCache[tileIdx].position + new Vector3(offset.x, baseY, offset.z);
        }
        return transform.position;
    }

    private Vector3 GetPlayerOffset(int idx)
    {
        switch (idx)
        {
            case 0: return new Vector3(-0.2f, 0f, -0.2f);
            case 1: return new Vector3(0.2f, 0f, -0.2f);
            case 2: return new Vector3(-0.2f, 0f, 0.2f);
            case 3: return new Vector3(0.2f, 0f, 0.2f);
            default: return Vector3.zero;
        }
    }
}
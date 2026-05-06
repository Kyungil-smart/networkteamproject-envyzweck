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

    public void Setup(int index)
    {
        playerIndex = index;
    }

    void Start()
    {
        CacheTiles(); 
        StartCoroutine(SubscribeWhenReady());
    }

    void CacheTiles()
    {
        _tileCache = new Transform[40]; 
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

        if (!isMoving) StartCoroutine(ProcessMoveQueue());
    }

    private IEnumerator ProcessMoveQueue()
    {
        isMoving = true;
        while (_tileIndexQueue.Count > 0)
        {
            int nextTileIdx = _tileIndexQueue.Dequeue();
            Vector3 targetPos = CalculateTargetPos(nextTileIdx);
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
            Vector3 currentPos = Vector3.Lerp(startPos, destination, t);
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

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerMoved -= OnPlayerMoved;
    }
}
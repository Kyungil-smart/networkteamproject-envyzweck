using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [SerializeField] List<TileData> tiles = new List<TileData>();
    [SerializeField] List<Transform> tilePositions = new List<Transform>();

    // 타일 주인 관리: -1은 주인 없음, 그 외는 플레이어 인덱스
    private int[] tileOwners = new int[40];

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < tileOwners.Length; i++) tileOwners[i] = -1;
    }

    public void HandleTileLanding(int pIdx, int tileIndex)
    {
        TileData currentTile = tiles[tileIndex];
        
        switch (currentTile.type)
        {
            case TileType.City:
                ProcessCity(pIdx, tileIndex, currentTile);
                break;

            case TileType.Jail:
                GameManager.Instance.SendToJail(pIdx);
                GameManager.Instance.FinishTurn();
                break;

            default:
                GameManager.Instance.FinishTurn();
                break;
        }
    }

    void ProcessCity(int pIdx, int tileIndex, TileData data)
    {
        int owner = tileOwners[tileIndex];

        // 1. 주인이 없는 경우: 구매 의사 묻기
        if (owner == -1)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPurchaseDialog(data, tileIndex);
            }
            else
            {
                Debug.LogWarning("UIManager 인스턴스를 찾을 수 없습니다!");
                GameManager.Instance.FinishTurn();
            }
        }
        // 2. 내 땅인 경우
        else if (owner == pIdx)
        {
            Debug.Log("내 땅입니다. 통행료를 받지 않습니다.");
            GameManager.Instance.FinishTurn();
        }
        // 3. 남의 땅인 경우: 통행료 지불
        else
        {
            PayToll(pIdx, owner, data.toll);
            GameManager.Instance.FinishTurn();
        }
    }
    
    public void BuyTile(int pIdx, int tileIndex)
    {
        // 실제 구매 처리: 돈 차감 및 소유권 등록
        if (GameManager.Instance.PayMoney(pIdx, tiles[tileIndex].price)) return; 
        
        tileOwners[tileIndex] = pIdx;
        Debug.Log($"{pIdx}번 플레이어 {tiles[tileIndex].tileName} 구매 완료!");
        GameManager.Instance.FinishTurn();
    }

    void PayToll(int pIdx, int ownerIdx, int toll)
    {
        bool isBankrupt = GameManager.Instance.PayMoney(pIdx, toll);
        if (!isBankrupt)
        {
            GameManager.Instance.ReceiveMoney(ownerIdx, toll);
            Debug.Log($"통행료 {toll}원 지불 완료");
        }
    }

    public Vector3 GetTilePosition(int index) => tilePositions[index].position;
}
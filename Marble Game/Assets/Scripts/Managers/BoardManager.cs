using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Board Config (ScriptableObject)")]
    public BoardConfigSO boardConfig;

    public TileDataSO[] Tiles { get; private set; }

    private TileDisplay[] tileDisplays;

    [System.Serializable]
    public class EventCard
    {
        public string eventTitle;
        public string description;
        public int    moneyDelta;
        public bool   goToJail;
        public int    moveSteps;
    }
    [SerializeField] List<EventCard> eventCards = new List<EventCard>();

    void Start()
    {
        InitTiles();
        // InitDefaultEventCards();
        // 씬 내의 모든 타일 디스플레이 찾기
        tileDisplays = Object.FindObjectsByType<TileDisplay>(FindObjectsSortMode.None);
    }

    void InitTiles()
    {
        if (boardConfig == null) return;

        Tiles = new TileDataSO[boardConfig.tiles.Length];
        for (int i = 0; i < boardConfig.tiles.Length; i++)
        {
            Tiles[i] = boardConfig.tiles[i];
            if (Tiles[i] != null) Tiles[i].ResetRuntimeState();
        }
    }

    public void HandleTileLanding(int playerIndex, int tileIndex)
    {
        if (Tiles == null || tileIndex >= Tiles.Length) return;
        var tile = Tiles[tileIndex];
        if (tile == null) return;

        switch (tile.type)
        {
            case TileDataSO.TileType.Start: OnLandStart(playerIndex); break;
            case TileDataSO.TileType.Property: OnLandProperty(playerIndex, tile); break;
            case TileDataSO.TileType.Event: OnLandEvent(playerIndex); break;
            case TileDataSO.TileType.Jail: GameManager.Instance.SendToJail(playerIndex); break;
            case TileDataSO.TileType.FreeParking: GameManager.Instance.FinishTurn(); break;
            case TileDataSO.TileType.Tax: OnLandTax(playerIndex, tile); break;
        }
    }

    void OnLandStart(int pIdx)
    {
        GameManager.Instance.ReceiveMoney(pIdx, 100000);
        GameManager.Instance.FinishTurn();
    }

    void OnLandProperty(int pIdx, TileDataSO tile)
    {
        var gm = GameManager.Instance;
        var player = gm.Players[pIdx];

        if (tile.ownerIndex < 0)
        {
            if (player.money >= tile.price)
            {
                if (TileLandingUI.Instance != null) TileLandingUI.Instance.ShowBuyPanel(pIdx, tile);
                else { BuyProperty(pIdx, tile); gm.FinishTurn(); }
            }
            else gm.FinishTurn();
        }
        else if (tile.ownerIndex == pIdx)
        {
            if (tile.CanBuild() && player.money >= tile.buildingCost)
            {
                if (TileLandingUI.Instance != null) TileLandingUI.Instance.ShowBuildPanel(pIdx, tile);
                else gm.FinishTurn();
            }
            else gm.FinishTurn();
        }
        else
        {
            if (TileLandingUI.Instance != null) TileLandingUI.Instance.ShowRentPanel(pIdx, tile);
            else 
            { 
                int rent = tile.GetCurrentRent();
                gm.PayMoney(pIdx, rent); 
                gm.ReceiveMoney(tile.ownerIndex, rent); 
                gm.FinishTurn(); 
            }
        }
    }

    void OnLandEvent(int pIdx)
    {
        var gm = GameManager.Instance;
        var card = eventCards[Random.Range(0, eventCards.Count)];

        if (TileLandingUI.Instance != null)
        {
            TileLandingUI.Instance.ShowEventPanel(pIdx, card);
        }
        else
        {
            if (card.goToJail) { gm.SendToJail(pIdx); return; }
            if (card.moneyDelta > 0) gm.ReceiveMoney(pIdx, card.moneyDelta);
            else if (card.moneyDelta < 0) gm.PayMoney(pIdx, -card.moneyDelta);

            if (card.moveSteps != 0) gm.RequestMovePlayer(pIdx, card.moveSteps);
            else gm.FinishTurn();
        }
    }

    void OnLandTax(int pIdx, TileDataSO tile)
    {
        var gm = GameManager.Instance;
        int tax = tile.taxAmount > 0 ? tile.taxAmount : (int)(gm.Players[pIdx].money * tile.taxRate);
        bool bankrupt = gm.PayMoney(pIdx, tax);
        if (!bankrupt) gm.FinishTurn();
    }

    public void BuyProperty(int pIdx, TileDataSO tile)
    {
        GameManager.Instance.PayMoney(pIdx, tile.price);
        tile.ownerIndex = pIdx;
        GameManager.Instance.Players[pIdx].ownedTiles.Add(tile.tileIndex);

        RefreshTileUI(tile.tileIndex);
    }

    public bool BuildOnTile(int pIdx, int tileIndex)
    {
        var tile = Tiles[tileIndex];
        if (!tile.CanBuild() || tile.ownerIndex != pIdx) return false;
        var gm = GameManager.Instance;
        if (gm.Players[pIdx].money < tile.buildingCost) return false;
        gm.PayMoney(pIdx, tile.buildingCost);
        if (tile.buildingCount >= 4) tile.hasHotel = true;
        else tile.buildingCount++;

        RefreshTileUI(tileIndex);
        return true;
    }

    // 특정 타일의 UI만 갱신
    private void RefreshTileUI(int index)
    {
        foreach (var disp in tileDisplays)
        {
            if (disp.tileIndex == index)
            {
                disp.UpdateDisplay();
                break;
            }
        }
    }
}
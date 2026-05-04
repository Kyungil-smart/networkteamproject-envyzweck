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

    [System.Serializable]
    public class EventCard
    {
        public string description;
        public int    moneyDelta;
        public bool   goToJail;
        public int    moveSteps;
    }
    [SerializeField] List<EventCard> eventCards = new List<EventCard>();

    void Start()
    {
        InitTiles();
        InitDefaultEventCards();
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

    void InitDefaultEventCards()
    {
        if (eventCards.Count > 0) return;

        eventCards.Add(new EventCard { description = "복권 당첨! +500,000",          moneyDelta = 500000 });
        eventCards.Add(new EventCard { description = "세금 환급! +200,000",          moneyDelta = 200000 });
        eventCards.Add(new EventCard { description = "건물 수리비 발생! -300,000",    moneyDelta = -300000 });
        eventCards.Add(new EventCard { description = "해외 여행 비용 지출! -400,000", moneyDelta = -400000 });
        eventCards.Add(new EventCard { description = "특별 보너스 지급! +1,000,000",  moneyDelta = 1000000 });
        eventCards.Add(new EventCard { description = "과속 적발! 무인도로 이동",       goToJail = true });
        eventCards.Add(new EventCard { description = "순풍을 타고! 3칸 앞으로",       moveSteps = 3 });
        eventCards.Add(new EventCard { description = "강한 역풍 발생! 2칸 뒤로",       moveSteps = -2 });
        eventCards.Add(new EventCard { description = "주식 투자 성공! +800,000",       moneyDelta = 800000 });
        eventCards.Add(new EventCard { description = "갑작스러운 병원비! -500,000",    moneyDelta = -500000 });
    }

    public void HandleTileLanding(int playerIndex, int tileIndex)
    {
        if (Tiles == null || tileIndex >= Tiles.Length) return;
        var tile = Tiles[tileIndex];
        if (tile == null) return;

        switch (tile.type)
        {
            case TileDataSO.TileType.Start:       OnLandStart(playerIndex);           break;
            case TileDataSO.TileType.Property:    OnLandProperty(playerIndex, tile); break;
            case TileDataSO.TileType.Event:       OnLandEvent(playerIndex);           break;
            case TileDataSO.TileType.Jail:        GameManager.Instance.SendToJail(playerIndex); break;
            case TileDataSO.TileType.FreeParking: GameManager.Instance.FinishTurn(); break;
            case TileDataSO.TileType.Tax:         OnLandTax(playerIndex, tile);      break;
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

        // 땅주인 없는 경우
        if (tile.ownerIndex < 0)
        {
            if (player.money >= tile.price)
            {
                if (TileLandingUI.Instance != null) TileLandingUI.Instance.ShowBuyPanel(pIdx, tile);
                else { BuyProperty(pIdx, tile); gm.FinishTurn(); }
            }
            else gm.FinishTurn();
        }
        // 내 땅인 경우
        else if (tile.ownerIndex == pIdx)
        {
            if (tile.CanBuild() && player.money >= tile.buildingCost)
            {
                if (TileLandingUI.Instance != null) TileLandingUI.Instance.ShowBuildPanel(pIdx, tile);
                else gm.FinishTurn();
            }
            else gm.FinishTurn();
        }
        // 남의 땅일 때
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
        return true;
    }
}
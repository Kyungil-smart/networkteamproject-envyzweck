using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TileLandingUI : MonoBehaviour
{
    public static TileLandingUI Instance { get; private set; }
    void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; }

    [Header("AI Settings")]
    public float aiDelay = 1.5f;

    [Header("Panels")]
    public GameObject buyPanel;
    public GameObject rentPanel;
    public GameObject buildPanel;
    public GameObject eventPanel;

    [Header("Buy Panel")]
    public Text buyTileName; public Text buyPrice; public Text buyBaseRent;
    public Button buyConfirmBtn; public Button buySkipBtn;

    [Header("Rent Panel")]
    public Text rentTileName; public Text rentOwnerName; public Text rentAmount;
    public Button rentConfirmBtn;

    [Header("Build Panel")]
    public Text buildTileName; public Text buildCurrentBuildings; public Text buildCost; public Text buildNewRent;
    public Button buildConfirmBtn; public Button buildSkipBtn;

    [Header("Event Panel")]
    public Text eventTitle; public Text eventDescription; public Text eventEffect;
    public Button eventConfirmBtn;

    int _pendingPlayerIndex = -1;
    BoardManager.EventCard _pendingEvent;
    TileDataSO _pendingTile;

    void Start()
    {
        HideAll();
        buyConfirmBtn.onClick.AddListener(OnBuyConfirm);
        buySkipBtn.onClick.AddListener(OnBuySkip);
        rentConfirmBtn.onClick.AddListener(OnRentConfirm);
        buildConfirmBtn.onClick.AddListener(OnBuildConfirm);
        buildSkipBtn.onClick.AddListener(OnBuildSkip);
        eventConfirmBtn.onClick.AddListener(OnEventConfirm);
    }

    public void HideAll()
    {
        StopAllCoroutines();
        buyPanel.SetActive(false);
        rentPanel.SetActive(false);
        buildPanel.SetActive(false);
        eventPanel.SetActive(false);
        SetButtonsInteractable(true); 
    }

    private void SetButtonsInteractable(bool state)
    {
        buyConfirmBtn.interactable = state; buySkipBtn.interactable = state;
        rentConfirmBtn.interactable = state;
        buildConfirmBtn.interactable = state; buildSkipBtn.interactable = state;
        eventConfirmBtn.interactable = state;
    }

    IEnumerator AIClickRoutine(System.Action action)
    {
        SetButtonsInteractable(false); 
        yield return new WaitForSeconds(aiDelay);
        action.Invoke();
        // AI 클릭이 끝나면 버튼을 다시 활성화합니다.
        SetButtonsInteractable(true); 
    }

    public void ShowBuyPanel(int playerIndex, TileDataSO tile)
    {
        // 패널을 띄우기 전 항상 버튼 상태를 초기화합니다.
        SetButtonsInteractable(true); 
        _pendingPlayerIndex = playerIndex;
        _pendingTile = tile;
        buyTileName.text = tile.tileName;
        buyPrice.text = "₩" + tile.price.ToString("N0");
        buyBaseRent.text = "₩" + tile.baseRent.ToString("N0");
        buyPanel.SetActive(true);

        if (GameManager.Instance.Players[playerIndex].isAI)
        {
            StartCoroutine(AIClickRoutine(() => {
                if (GameManager.Instance.Players[playerIndex].money >= tile.price * 1.2f) OnBuyConfirm();
                else OnBuySkip();
            }));
        }
    }

    public void ShowRentPanel(int playerIndex, TileDataSO tile)
    {
        SetButtonsInteractable(true);
        _pendingPlayerIndex = playerIndex;
        _pendingTile = tile;
        var gm = GameManager.Instance;
        rentTileName.text = tile.tileName;
        rentOwnerName.text = gm.Players[tile.ownerIndex].playerName;
        rentAmount.text = "₩" + tile.GetCurrentRent().ToString("N0");
        rentPanel.SetActive(true);

        if (gm.Players[playerIndex].isAI)
            StartCoroutine(AIClickRoutine(OnRentConfirm));
    }

    public void ShowBuildPanel(int playerIndex, TileDataSO tile)
    {
        SetButtonsInteractable(true);
        _pendingPlayerIndex = playerIndex;
        _pendingTile = tile;
        string buildings = tile.hasHotel ? "호텔" : tile.buildingCount + "채";
        buildTileName.text = tile.tileName;
        buildCurrentBuildings.text = "현재 " + buildings;
        buildCost.text = "₩" + tile.buildingCost.ToString("N0");
        buildPanel.SetActive(true);

        if (GameManager.Instance.Players[playerIndex].isAI)
            StartCoroutine(AIClickRoutine(OnBuildConfirm));
    }

    public void ShowEventPanel(int playerIndex, BoardManager.EventCard card)
    {
        SetButtonsInteractable(true);
        _pendingPlayerIndex = playerIndex;
        _pendingEvent = card;
        eventDescription.text = card.description;
        eventPanel.SetActive(true);

        if (GameManager.Instance.Players[playerIndex].isAI)
            StartCoroutine(AIClickRoutine(OnEventConfirm));
    }

    void OnBuyConfirm() {
        buyPanel.SetActive(false);
        BoardManager.Instance.BuyProperty(_pendingPlayerIndex, _pendingTile);
        GameManager.Instance.FinishTurn();
    }
    void OnBuySkip() { buyPanel.SetActive(false); GameManager.Instance.FinishTurn(); }

    void OnRentConfirm() {
        rentPanel.SetActive(false);
        int rent = _pendingTile.GetCurrentRent();
        bool bankrupt = GameManager.Instance.PayMoney(_pendingPlayerIndex, rent);
        if (!bankrupt) {
            GameManager.Instance.ReceiveMoney(_pendingTile.ownerIndex, rent);
            GameManager.Instance.FinishTurn();
        }
    }

    void OnBuildConfirm() {
        buildPanel.SetActive(false);
        BoardManager.Instance.BuildOnTile(_pendingPlayerIndex, _pendingTile.tileIndex);
        GameManager.Instance.FinishTurn();
    }
    void OnBuildSkip() { buildPanel.SetActive(false); GameManager.Instance.FinishTurn(); }

    void OnEventConfirm() {
        eventPanel.SetActive(false);
        var gm = GameManager.Instance;
        var card = _pendingEvent;
        if (card.goToJail) { gm.SendToJail(_pendingPlayerIndex); return; }
        if (card.moneyDelta > 0) gm.ReceiveMoney(_pendingPlayerIndex, card.moneyDelta);
        else if (card.moneyDelta < 0) gm.PayMoney(_pendingPlayerIndex, -card.moneyDelta);
        
        if (card.moveSteps != 0) gm.RequestMovePlayer(_pendingPlayerIndex, card.moveSteps);
        else gm.FinishTurn();
    }
}
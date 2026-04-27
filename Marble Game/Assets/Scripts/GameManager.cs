using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public enum GameState { WaitingToStart, PlayerTurn, Rolling, Moving, LandedOnTile, GameOver }

    [System.Serializable]
    public class PlayerData
    {
        public int       playerIndex;
        public string    playerName;
        public int       money;
        public int       currentTileIndex;
        public bool      isInJail;
        public int       jailTurns;
        public bool      isBankrupt;
        public bool      isAI;
        public List<int> ownedTiles;

        public PlayerData(int idx, string name, int startMoney, bool ai = false)
        {
            playerIndex = idx; playerName = name; money = startMoney;
            currentTileIndex = 0; isInJail = false; jailTurns = 0;
            isBankrupt = false; isAI = ai; ownedTiles = new List<int>();
        }
    }

    [Header("게임 설정")]
    [SerializeField] int startMoney    = 3000000;
    [SerializeField] int salaryOnStart = 200000;
    [SerializeField] int totalTiles    = 40;

    [Header("플레이어 구성")]
    [SerializeField] int humanCount = 1;
    [SerializeField] int aiCount    = 1;

    public GameState        CurrentState  { get; private set; }
    public List<PlayerData> Players       { get; private set; }
    public int              CurrentIndex  { get; private set; }
    public PlayerData       CurrentPlayer => Players[CurrentIndex];
    public int              LastDice      { get; private set; }

    public event System.Action<GameState> OnStateChanged;
    public event System.Action<int>       OnTurnChanged;
    public event System.Action<int, int>  OnDiceRolled;
    public event System.Action<int, int>  OnPlayerMoved;
    public event System.Action<int>       OnPlayerBankrupt;
    public event System.Action<int>       OnGameOver;

    void Start() { InitGame(); }

    public void InitGame()
    {
        Players = new List<PlayerData>();
        for (int i = 0; i < humanCount; i++)
            Players.Add(new PlayerData(i, "Player " + (i + 1), startMoney, false));
        for (int i = 0; i < aiCount; i++)
            Players.Add(new PlayerData(humanCount + i, "BOT " + (i + 1), startMoney, true));
        CurrentIndex = 0;
        ApplyState(GameState.PlayerTurn);
        Debug.Log("[GameManager] 게임 시작! 총 " + Players.Count + "명");
    }

    void ApplyState(GameState s)
    {
        CurrentState = s;
        OnStateChanged?.Invoke(s);
        Debug.Log("[GameManager] 상태=" + s + "  턴=" + CurrentPlayer.playerName);
        if (s == GameState.PlayerTurn && CurrentPlayer.isAI)
            StartCoroutine(AITurn());
    }

    public void RollDice()
    {
        if (CurrentState != GameState.PlayerTurn || CurrentPlayer.isAI) return;
        StartCoroutine(DiceCoroutine(false));
    }

    IEnumerator DiceCoroutine(bool isAI)
    {
        if (isAI) yield return new WaitForSeconds(1f);
        ApplyState(GameState.Rolling);
        int d1 = Random.Range(1, 7), d2 = Random.Range(1, 7);
        LastDice = d1 + d2;
        Debug.Log("[GameManager] " + CurrentPlayer.playerName + (isAI ? "(AI)" : "") + " 주사위 " + d1 + "+" + d2 + "=" + LastDice);
        OnDiceRolled?.Invoke(d1, d2);
        yield return new WaitForSeconds(0.5f);
        if (CurrentPlayer.isInJail) { ProcessJail(d1, d2); yield break; }
        yield return StartCoroutine(MoveCoroutine(CurrentIndex, LastDice));
    }

    IEnumerator AITurn() { yield return StartCoroutine(DiceCoroutine(true)); }

    IEnumerator MoveCoroutine(int pIdx, int steps)
    {
        ApplyState(GameState.Moving);
        PlayerData p = Players[pIdx];
        int from = p.currentTileIndex;
        for (int i = 0; i < steps; i++)
        {
            p.currentTileIndex = (p.currentTileIndex + 1) % totalTiles;
            if (p.currentTileIndex == 0)
            {
                p.money += salaryOnStart;
                Debug.Log("[GameManager] " + p.playerName + " 출발 통과! +" + salaryOnStart + "원");
            }
            OnPlayerMoved?.Invoke(pIdx, p.currentTileIndex);
            yield return new WaitForSeconds(0.2f);
        }
        Debug.Log("[GameManager] " + p.playerName + " " + from + "→" + p.currentTileIndex);
        ApplyState(GameState.LandedOnTile);
        BoardManager.Instance?.HandleTileLanding(pIdx, p.currentTileIndex);
    }

    void ProcessJail(int d1, int d2)
    {
        PlayerData p = CurrentPlayer;
        if (d1 == d2)
        {
            p.isInJail = false; p.jailTurns = 0;
            Debug.Log("[GameManager] " + p.playerName + " 더블 탈출!");
            StartCoroutine(MoveCoroutine(CurrentIndex, d1 + d2));
        }
        else
        {
            p.jailTurns--;
            if (p.jailTurns <= 0) { p.isInJail = false; p.jailTurns = 0; }
            Debug.Log("[GameManager] " + p.playerName + " 감옥 " + p.jailTurns + "턴 남음");
            FinishTurn();
        }
    }

    public void FinishTurn()
    {
        if (CurrentState == GameState.GameOver) return;
        int next = (CurrentIndex + 1) % Players.Count;
        for (int i = 0; i < Players.Count; i++) { if (!Players[next].isBankrupt) break; next = (next + 1) % Players.Count; }
        CurrentIndex = next;
        OnTurnChanged?.Invoke(next);
        ApplyState(GameState.PlayerTurn);
    }

    public bool PayMoney(int pIdx, int amount)
    {
        Players[pIdx].money -= amount;
        if (Players[pIdx].money < 0) { HandleBankrupt(pIdx); return true; }
        return false;
    }

    public void ReceiveMoney(int pIdx, int amount) { Players[pIdx].money += amount; }

    public void SendToJail(int pIdx)
    {
        Players[pIdx].isInJail = true; Players[pIdx].jailTurns = 3; Players[pIdx].currentTileIndex = 10;
        OnPlayerMoved?.Invoke(pIdx, 10);
        Debug.Log("[GameManager] " + Players[pIdx].playerName + " 감옥!");
    }

    void HandleBankrupt(int pIdx)
    {
        Players[pIdx].isBankrupt = true; Players[pIdx].money = 0;
        Debug.Log("[GameManager] " + Players[pIdx].playerName + " 파산!");
        OnPlayerBankrupt?.Invoke(pIdx);
        int active = 0, winner = -1;
        for (int i = 0; i < Players.Count; i++) if (!Players[i].isBankrupt) { active++; winner = i; }
        if (active <= 1)
        {
            ApplyState(GameState.GameOver);
            OnGameOver?.Invoke(winner);
            Debug.Log("[GameManager] 게임 종료! 승자: " + (winner >= 0 ? Players[winner].playerName : "없음"));
        }
        else FinishTurn();
    }

    [ContextMenu("플레이어 현황")]
    public void PrintStatus()
    {
        foreach (var p in Players)
            Debug.Log("[" + p.playerName + "] 자금=" + p.money + " 위치=" + p.currentTileIndex + " 감옥=" + p.isInJail);
    }
}
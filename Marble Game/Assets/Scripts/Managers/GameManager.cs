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
        
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public enum GameState { WaitingToStart, PlayerTurn, Rolling, Moving, LandedOnTile, GameOver }

    [System.Serializable]
    public class PlayerData
    {
        public int playerIndex;
        public string playerName;
        public int money;
        public int currentTileIndex;
        public bool isInJail;
        public int jailTurns;
        public bool isBankrupt;
        public bool isAI;
        public List<int> ownedTiles;

        public PlayerData(int idx, string name, int startMoney, bool ai = false)
        {
            playerIndex = idx; playerName = name; money = startMoney;
            currentTileIndex = 0; isInJail = false; jailTurns = 0;
            isBankrupt = false; isAI = ai; ownedTiles = new List<int>();
        }
    }

    [Header("게임 설정")]
    [SerializeField] int startMoney = 3000000;
    [SerializeField] int salaryOnStart = 200000;
    [SerializeField] int totalTiles = 40;

    public GameState CurrentState { get; private set; }
    public List<PlayerData> Players { get; private set; }
    public int CurrentIndex { get; private set; }
    public PlayerData CurrentPlayer => Players[CurrentIndex];
    public int LastDice { get; private set; }

    private bool _isDouble = false;
    private int _doubleCount = 0;

    public event System.Action<GameState> OnStateChanged;
    public event System.Action<int> OnTurnChanged;
    public event System.Action<int, int> OnDiceRolled;
    public event System.Action<int, int> OnPlayerMoved;
    public event System.Action<int> OnPlayerBankrupt;
    public event System.Action<int> OnGameOver;

    void Start()
    {
        StartCoroutine(InitGameDelayed());
    }

    IEnumerator InitGameDelayed()
    {
        yield return null; 
        InitGame();
    }

    public void InitGame()
    {
        Players = new List<PlayerData>();
        
        int h = SceneLoader.HumanPlayerCount > 0 ? SceneLoader.HumanPlayerCount : 1;
        int a = SceneLoader.AIPlayerCount >= 0 ? SceneLoader.AIPlayerCount : 1;

        for (int i = 0; i < h; i++)
            Players.Add(new PlayerData(i, "Player " + (i + 1), startMoney, false));
        for (int i = 0; i < a; i++)
            Players.Add(new PlayerData(h + i, "BOT " + (i + 1), startMoney, true));
        
        CurrentIndex = 0;
        ApplyState(GameState.PlayerTurn);
    }

    public void ApplyState(GameState s)
    {
        CurrentState = s;
        OnStateChanged?.Invoke(s);
        Debug.Log($"[GameManager] 상태 전환: {s}");

        if (s == GameState.PlayerTurn && CurrentPlayer.isAI)
        {
            StartCoroutine(AITurnRoutine());
        }
    }

    // ── 주사위 및 턴 로직 ──

    public void NotifyDiceStart()
    {
        if (CurrentState != GameState.PlayerTurn) return;
        ApplyState(GameState.Rolling);
    }

    public void ApplyDiceResult(int d1, int d2)
    {
        LastDice = d1 + d2;
        
        if (d1 == d2)
        {
            _isDouble = true;
            _doubleCount++;
        }
        else
        {
            _isDouble = false;
            _doubleCount = 0;
        }

        OnDiceRolled?.Invoke(d1, d2);
        StartCoroutine(ProcessMoveAfterDice(d1, d2));
    }

    IEnumerator ProcessMoveAfterDice(int d1, int d2)
    {
        yield return new WaitForSeconds(0.6f);

        if (_doubleCount >= 3)
        {
            Debug.Log("<color=red>3회 연속 더블! 무인도행</color>");
            _isDouble = false;
            _doubleCount = 0;
            SendToJail(CurrentIndex);
            yield break;
        }

        if (CurrentPlayer.isInJail)
        {
            ProcessJail(d1, d2);
        }
        else
        {
            yield return StartCoroutine(MoveCoroutine(CurrentIndex, LastDice));
        }
    }

    IEnumerator AITurnRoutine()
    {
        yield return new WaitForSeconds(1.2f);
        NotifyDiceStart();
        
        var roller = Object.FindFirstObjectByType<DiceRoller>();
        if (roller != null) roller.Roll();
    }

    // 이동

    public void RequestMovePlayer(int pIdx, int steps)
    {
        // 이벤트 카드 등 외부요청이 있을때 사용
        StartCoroutine(MoveCoroutine(pIdx, steps));
    }

    public IEnumerator MoveCoroutine(int pIdx, int steps)
    {
        ApplyState(GameState.Moving); 
        PlayerData p = Players[pIdx];

        int direction = steps > 0 ? 1 : -1;
        int absSteps = Mathf.Abs(steps);

        for (int i = 0; i < absSteps; i++)
        {
            // 인덱스 순환 계산
            p.currentTileIndex = (p.currentTileIndex + direction + totalTiles) % totalTiles;
            
            // 앞으로 이동 시 시작점 통과 체크
            if (direction > 0 && p.currentTileIndex == 0) p.money += salaryOnStart;

            // 시각적 이동 이벤트 발생
            OnPlayerMoved?.Invoke(pIdx, p.currentTileIndex);
            yield return new WaitForSeconds(0.3f); 
        }

        ApplyState(GameState.LandedOnTile);
        
        if (BoardManager.Instance != null)
            BoardManager.Instance.HandleTileLanding(pIdx, p.currentTileIndex);
        else
            FinishTurn();
    }

    // 게임 규칙

    void ProcessJail(int d1, int d2)
    {
        PlayerData p = CurrentPlayer;
        if (d1 == d2)
        {
            p.isInJail = false; p.jailTurns = 0;
            _isDouble = false;
            Debug.Log($"{p.playerName} 탈출 성공!");
            StartCoroutine(MoveCoroutine(CurrentIndex, d1 + d2));
        }
        else
        {
            p.jailTurns--;
            if (p.jailTurns <= 0) p.isInJail = false;
            FinishTurn();
        }
    }

    public void FinishTurn()
    {
        if (CurrentState == GameState.GameOver) return;

        if (_isDouble && !CurrentPlayer.isBankrupt && !CurrentPlayer.isInJail)
        {
            _isDouble = false;
            ApplyState(GameState.PlayerTurn); 
            return;
        }

        _doubleCount = 0;
        _isDouble = false;

        int next = (CurrentIndex + 1) % Players.Count;
        while (Players[next].isBankrupt) {
            next = (next + 1) % Players.Count;
        }

        CurrentIndex = next;
        ApplyState(GameState.PlayerTurn); 
        OnTurnChanged?.Invoke(next);
    }

    public bool PayMoney(int pIdx, int amount)
    {
        Players[pIdx].money -= amount;
        if (Players[pIdx].money < 0) { HandleBankrupt(pIdx); return true; }
        return false;
    }

    public void ReceiveMoney(int pIdx, int amount) => Players[pIdx].money += amount;

    public void SendToJail(int pIdx)
    {
        PlayerData p = Players[pIdx];
        p.isInJail = true; p.jailTurns = 3;
        p.currentTileIndex = 10; 
        OnPlayerMoved?.Invoke(pIdx, 10); // 즉시 무인도로 이동
        
        _isDouble = false; _doubleCount = 0;
        FinishTurn();
    }

    void HandleBankrupt(int pIdx)
    {
        Players[pIdx].isBankrupt = true;
        Players[pIdx].money = 0;
        OnPlayerBankrupt?.Invoke(pIdx);

        int activeCount = 0;
        int winnerIdx = -1;
        for (int i = 0; i < Players.Count; i++)
        {
            if (!Players[i].isBankrupt) { activeCount++; winnerIdx = i; }
        }

        if (activeCount <= 1)
        {
            ApplyState(GameState.GameOver);
            OnGameOver?.Invoke(winnerIdx);
        }
        else FinishTurn();
    }
}
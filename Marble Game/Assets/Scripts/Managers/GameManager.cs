using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

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
        public GameObject playerObject; // 생성된 캐릭터 오브젝트 참조

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

    [Header("캐릭터 배치 설정")]
    [SerializeField] private Transform[] startPoints; 
    [SerializeField] private float playerOffset = 0.4f; // 캐릭터 간격 조절

    // 플레이어 고유 색상 설정
    [SerializeField] private Color[] playerColors = { Color.red, Color.blue, Color.green, Color.yellow };

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
        
        int selectedCharIdx = SceneLoader.SelectedCharacterIndex; 
        int aiCount = SceneLoader.AIPlayerCount >= 0 ? SceneLoader.AIPlayerCount : 1;

        // 플레이어 캐릭터 추가
        Players.Add(new PlayerData(selectedCharIdx, "Player 1", startMoney, false));

        // AI 캐릭터 추가
        int aiAssigned = 0;
        for (int i = 0; i < 4; i++)
        {
            if (aiAssigned >= aiCount) break;
            if (i == selectedCharIdx) continue; 

            Players.Add(new PlayerData(i, "BOT " + (aiAssigned + 1), startMoney, true));
            aiAssigned++;
        }
        
        SpawnCharacters(); 

        CurrentIndex = 0;
        ApplyState(GameState.PlayerTurn);
    }

    // 캐릭터 모델 소환 및 자리 배치

    private void SpawnCharacters()
    {
        for (int seatIndex = 0; seatIndex < Players.Count; seatIndex++)
        {
            PlayerData p = Players[seatIndex];

            GameObject prefab = Resources.Load<GameObject>($"Characters/Character_{p.playerIndex}");
            
            if (prefab != null)
            {
                Vector3 spawnPos = (startPoints != null && startPoints.Length > seatIndex) 
                                   ? startPoints[seatIndex].position : Vector3.zero;
                
                Vector3 offset = GetPlayerOffset(seatIndex); 
                
                GameObject go = Instantiate(prefab, spawnPos + offset, Quaternion.identity);
                p.playerObject = go;

                // 캐릭터 컨트롤러에 seatIndex'를 주입
                var controller = go.GetComponent<PlayerController>(); 
                if (controller != null) 
                {
                    controller.Setup(seatIndex); 
                }

                OnPlayerMoved?.Invoke(seatIndex, 0);
            }
            else Debug.LogWarning($"[GameManager] Characters/Character_{p.playerIndex} 프리팹 누락");
        }
    }

    public Vector3 GetPlayerOffset(int index)
    {
        float x = (index % 2 == 0) ? -playerOffset : playerOffset;
        float z = (index < 2) ? playerOffset : -playerOffset;
        return new Vector3(x, 0, z);
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

    public Color GetPlayerColor(int index)
    {
        if (index < 0 || index >= playerColors.Length) return Color.white;
        return playerColors[index];
    }

    // 주사위 및 턴 로직

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
            _isDouble = false;
            _doubleCount = 0;
            SendToJail(CurrentIndex);
            yield break;
        }

        if (CurrentPlayer.isInJail) ProcessJail(d1, d2);
        else yield return StartCoroutine(MoveCoroutine(CurrentIndex, LastDice));
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
            p.currentTileIndex = (p.currentTileIndex + direction + totalTiles) % totalTiles;
            if (direction > 0 && p.currentTileIndex == 0) p.money += salaryOnStart;

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
        OnPlayerMoved?.Invoke(pIdx, 10); 
        
        _isDouble = false; _doubleCount = 0;
        FinishTurn();
    }

    void HandleBankrupt(int pIdx)
    {
        Players[pIdx].isBankrupt = true;
        Players[pIdx].money = 0;
        OnPlayerBankrupt?.Invoke(pIdx);

        if (Players[pIdx].playerObject != null) Players[pIdx].playerObject.SetActive(false);

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
            StartCoroutine(TransitionToResultScene(winnerIdx));
        }
        else FinishTurn();
    }

    IEnumerator TransitionToResultScene(int winnerIdx)
    {
        yield return new WaitForSeconds(2.5f);
        PlayerPrefs.SetString("WinnerName", Players[winnerIdx].playerName);
        PlayerPrefs.SetInt("LastHumanCount", SceneLoader.HumanPlayerCount);
        PlayerPrefs.SetInt("LastAICount", SceneLoader.AIPlayerCount);
        PlayerPrefs.SetString("WinnerPrefabName", "Character_" + Players[winnerIdx].playerIndex);
        SceneManager.LoadScene("ResultScene");
    }
}
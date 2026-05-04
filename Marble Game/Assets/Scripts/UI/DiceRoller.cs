using UnityEngine;

public class DiceRoller : MonoBehaviour
{
    public static DiceRoller Instance { get; private set; }

    [Header("주사위")]
    public Dice3D dice1;
    public Dice3D dice2;

    public bool IsRolling { get; private set; }
    public event System.Action<int,int> OnRollComplete;

    int _r1, _r2;
    bool _d1Done, _d2Done;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (dice1 != null) dice1.OnRollComplete += OnDice1Complete;
        if (dice2 != null) dice2.OnRollComplete += OnDice2Complete;
    }

    public void Roll()
    {
        if (IsRolling) return;
        IsRolling = true;
        dice1?.StartRoll();
        dice2?.StartRoll();
    }

    void OnDice1Complete(int r)
    {
        _r1 = r; _d1Done = true;
        CheckBothComplete();
    }

    void OnDice2Complete(int r)
    {
        _r2 = r; _d2Done = true;
        CheckBothComplete();
    }

    void CheckBothComplete()
    {
        if (_d1Done && _d2Done)
        {
            IsRolling = false;
            OnRollComplete?.Invoke(_r1, _r2);
            _d1Done = _d2Done = false;
        }
    }
}

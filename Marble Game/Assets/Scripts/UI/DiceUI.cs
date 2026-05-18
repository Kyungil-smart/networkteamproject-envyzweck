using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class DiceUI : MonoBehaviour
{
    private Rigidbody rb;
    
    [Header("UI 참조")]
    public TMP_Text totalText;  
    public TMP_Text turnText;  
    public TMP_Text resultText; 
    public Button rollButton;

    [Header("3D 주사위 (자동 탐색)")]
    public DiceRoller diceRoller;

    bool _isRolling = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Start()
    {
        if (diceRoller == null)
            diceRoller = Object.FindFirstObjectByType<DiceRoller>();

        if (diceRoller != null)
            diceRoller.OnRollComplete += OnRollComplete;

        if (rollButton != null)
            rollButton.onClick.AddListener(OnRollClicked);

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += UpdateUIByState;
    }

    void OnDestroy()
    {
        if (diceRoller != null) diceRoller.OnRollComplete -= OnRollComplete;
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= UpdateUIByState;
    }

    void UpdateUIByState(GameManager.GameState state)
    {
        if (_isRolling)
        {
            SetRollButtonActive(false);
            return;
        }

        var gp = GameManager.Instance;
        if (gp == null) return;

        bool isMyTurn = (state == GameManager.GameState.PlayerTurn);
        bool isNotAI = !gp.CurrentPlayer.isAI;

        SetRollButtonActive(isMyTurn && isNotAI);
        
        if (turnText != null)
            turnText.text = $"{gp.CurrentPlayer.playerName}의 턴";
    }

    void OnRollClicked()
    {
        SoundManager.Instance.PlayDiceRoll();

        if (_isRolling) return;
        _isRolling = true;
        
        
        SetRollButtonActive(false); // 굴리기 시작하면 버튼 끔
        totalText.text = "-";
        resultText.text = "굴리는 중...";
        
        diceRoller?.Roll();
        
    }

    void OnRollComplete(int d1, int d2)
    {
        Debug.Log($"<color=yellow>[DiceUI]</color> 주사위 굴리기 완료! 결과: {d1}, {d2}");
        _isRolling = false;
        
        totalText.text = (d1 + d2).ToString();
        resultText.text = $"{d1} + {d2} = {d1+d2}칸!";
        
        if (GameManager.Instance != null)
        {
            Debug.Log("<color=yellow>[DiceUI]</color> GameManager에 결과 전달 시도...");
            GameManager.Instance.ApplyDiceResult(d1, d2);
        }
        else
        {
            Debug.LogError("<color=red>[DiceUI] 에러: GameManager 인스턴스를 찾을 수 없습니다!</color>");
        }
    }

    void SetRollButtonActive(bool on)
    {
        if (rollButton == null) return;
        rollButton.interactable = on;
        ColorBlock c = rollButton.colors;
        c.normalColor = on ? new Color(0.2f, 0.7f, 0.2f) : new Color(0.4f, 0.4f, 0.4f);
        rollButton.colors = c;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. 부딪힌 상대방의 태그가 "Floor" 혹은 "Board"인지 확인 (원하는 대로 설정)
        if (collision.gameObject.CompareTag("Floor") || collision.gameObject.CompareTag("Board"))
        {
            // 2. 부딪히는 속도(강도)를 계산
            // 속도가 빠를수록 세게 부딪힌 것이므로 소리가 나야 함
            float impactForce = collision.relativeVelocity.magnitude;

            // 3. 너무 미세하게 구르는 소리는 무시 (임계값 0.5f 정도)
            if (impactForce > 0.5f)
            {
                // 강도에 따라 볼륨을 조절하면 더 리얼합니다 (0.1 ~ 1.0 사이)
                float volume = Mathf.Clamp(impactForce / 10f, 0.1f, 1.0f);
                
                // 사운드 매니저 호출!
                SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxDiceRoll, volume, Random.Range(0.95f, 1.05f));
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayerHUDManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerHUD
    {
        public GameObject root;
        public TMP_Text   nameText;  
        public TMP_Text   moneyText; 
        public TMP_Text   ownText;   
        public Image      colorBar;
        [Header("Bankrupt UI")]
        public GameObject bankruptOverlay; 
        public RectTransform bankruptStamp; 
    }

    public PlayerHUD[] huds = new PlayerHUD[4];

    static readonly Color[] PCOLORS = {
        new Color(1.0f,0.2f,0.2f),
        new Color(0.2f,0.4f,1.0f),
        new Color(0.2f,0.8f,0.2f),
        new Color(1.0f,0.8f,0.0f),
    };

    void Start()
    {
        // 시작하자마자 모든 오버레이 강제 비활성화 (잔상 제거)
        foreach(var h in huds) {
            if(h.bankruptOverlay != null) h.bankruptOverlay.SetActive(false);
        }
        StartCoroutine(SubscribeWhenReady());
    }

    IEnumerator SubscribeWhenReady()
    {
        float t = 3f;
        while (GameManager.Instance == null && t > 0f) { yield return null; t -= Time.deltaTime; }
        if (GameManager.Instance == null) { Debug.LogError("[PlayerHUDManager] GameManager를 찾을 수 없습니다!"); yield break; }

        // 이벤트 구독 (중복 방지를 위해 -= 후 +=)
        GameManager.Instance.OnStateChanged   -= HandleRefresh;
        GameManager.Instance.OnStateChanged   += HandleRefresh;
        GameManager.Instance.OnTurnChanged    -= HandleRefreshInt;
        GameManager.Instance.OnTurnChanged    += HandleRefreshInt;
        GameManager.Instance.OnPlayerMoved    -= HandleRefreshTwo;
        GameManager.Instance.OnPlayerMoved    += HandleRefreshTwo;
        
        // 파산 이벤트 디버그 포함
        GameManager.Instance.OnPlayerBankrupt -= OnBankruptEventTriggered;
        GameManager.Instance.OnPlayerBankrupt += OnBankruptEventTriggered;

        Debug.Log("<color=cyan>[PlayerHUDManager] 모든 이벤트 구독 완료</color>");
        Refresh();
    }

    // 이벤트 래퍼 함수들
    void HandleRefresh(GameManager.GameState s) => Refresh();
    void HandleRefreshInt(int i) => Refresh();
    void HandleRefreshTwo(int i, int j) => Refresh();

    void OnBankruptEventTriggered(int pIdx)
    {
        Debug.Log($"<color=red>[BANKRUPT EVENT] 이벤트 발생! 파산 인덱스: {pIdx}</color>");
        StartCoroutine(PlayBankruptAnimation(pIdx));
    }

    IEnumerator PlayBankruptAnimation(int pIdx)
    {
        if (pIdx < 0 || pIdx >= huds.Length || huds[pIdx] == null) {
            Debug.LogError($"[PlayBankruptAnimation] 잘못된 인덱스 접근: {pIdx}");
            yield break;
        }

        Debug.Log($"[PlayBankruptAnimation] {pIdx}번 플레이어 애니메이션 시작");
        var hud = huds[pIdx];
        hud.bankruptOverlay.SetActive(true);
        
        if (hud.bankruptStamp != null)
        {
            Vector2 targetPos = hud.bankruptStamp.anchoredPosition;
            Vector2 startPos = new Vector2(Screen.width, targetPos.y);
            float duration = 0.5f;
            float elapsed = 0f;

            hud.bankruptStamp.anchoredPosition = startPos;
            hud.bankruptStamp.localScale = Vector3.one * 3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float curve = elapsed / duration;
                hud.bankruptStamp.anchoredPosition = Vector2.Lerp(startPos, targetPos, curve);
                hud.bankruptStamp.localScale = Vector3.Lerp(Vector3.one * 3f, Vector3.one, curve);
                yield return null;
            }
        }
        
        Refresh();
    }

    public void Refresh()
    {
        if (GameManager.Instance == null) return;
        var players = GameManager.Instance.Players;
        if (players == null) return;

        for (int i = 0; i < huds.Length; i++)
        {
            if (huds[i] == null || huds[i].root == null) continue;
            if (i >= players.Count) { huds[i].root.SetActive(false); continue; }

            var p = players[i];
            
            // 실시간 데이터 디버그 로그 (Player 1이 자꾸 파산으로 뜨는지 확인용)
            if (i == 0) {
                // Debug.Log($"[Refresh] Player 0 ({p.playerName}) - Money: {p.money}, Bankrupt: {p.isBankrupt}");
            }

            // 파산 오버레이 처리
            if (huds[i].bankruptOverlay != null)
            {
                // 만약 데이터는 False인데 UI가 켜져 있다면 강제로 끔
                if (huds[i].bankruptOverlay.activeSelf != p.isBankrupt)
                {
                    Debug.Log($"[UI Sync] {p.playerName}의 파산 UI 상태를 {p.isBankrupt}로 변경합니다.");
                    huds[i].bankruptOverlay.SetActive(p.isBankrupt);
                }
            }

            if (huds[i].nameText  != null) huds[i].nameText.text  = p.playerName + (p.isAI ? " (AI)" : "");
            if (huds[i].moneyText != null) huds[i].moneyText.text = "₩ " + p.money.ToString("N0");
            if (huds[i].ownText   != null) huds[i].ownText.text   = p.ownedTiles.Count + " 개";

            bool isCurrent = (GameManager.Instance.CurrentIndex == i);
            var bg = huds[i].root.GetComponent<Image>();
            
            if (bg != null) bg.color = isCurrent ? new Color(0.08f,0.08f,0.08f,0.95f) : new Color(0.04f,0.04f,0.04f,0.80f);
            if (huds[i].colorBar != null)
            {
                int colorIdx = p.playerIndex;
                if(colorIdx >= 0 && colorIdx < PCOLORS.Length)
                    huds[i].colorBar.color = isCurrent ? PCOLORS[colorIdx] : new Color(PCOLORS[colorIdx].r*0.5f, PCOLORS[colorIdx].g*0.5f, PCOLORS[colorIdx].b*0.5f);
            }
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged   -= HandleRefresh;
            GameManager.Instance.OnTurnChanged    -= HandleRefreshInt;
            GameManager.Instance.OnPlayerMoved    -= HandleRefreshTwo;
            GameManager.Instance.OnPlayerBankrupt -= OnBankruptEventTriggered;
        }
    }
}
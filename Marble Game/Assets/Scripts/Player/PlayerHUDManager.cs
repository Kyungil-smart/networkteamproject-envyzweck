using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
        StartCoroutine(SubscribeWhenReady());
    }

    IEnumerator SubscribeWhenReady()
    {
        float t = 3f;
        while (GameManager.Instance == null && t > 0f) { yield return null; t -= Time.deltaTime; }
        if (GameManager.Instance == null) { Debug.LogWarning("[PlayerHUDManager] GameManager 없음"); yield break; }

        GameManager.Instance.OnStateChanged   += _ => Refresh();
        GameManager.Instance.OnTurnChanged    += _ => Refresh();
        GameManager.Instance.OnPlayerMoved    += (_,__) => Refresh();
        GameManager.Instance.OnPlayerBankrupt += (idx) => StartCoroutine(PlayBankruptAnimation(idx)); 
        Refresh();
    }

    // 파산 도장 애니메이션 코루틴 추가
    IEnumerator PlayBankruptAnimation(int pIdx)
    {
        if (pIdx >= huds.Length || huds[pIdx] == null || huds[pIdx].bankruptStamp == null) yield break;

        var hud = huds[pIdx];
        hud.bankruptOverlay.SetActive(true);
        
        Vector2 targetPos = hud.bankruptStamp.anchoredPosition; // 원래 찍혀야 할 위치
        Vector2 startPos = new Vector2(Screen.width, targetPos.y); 
        
        float duration = 0.5f;
        float elapsed = 0f;

        hud.bankruptStamp.anchoredPosition = startPos;
        hud.bankruptStamp.localScale = Vector3.one * 3f; // 처음에 크게 시작

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float curve = elapsed / duration;
            hud.bankruptStamp.anchoredPosition = Vector2.Lerp(startPos, targetPos, curve);
            hud.bankruptStamp.localScale = Vector3.Lerp(Vector3.one * 3f, Vector3.one, curve);
            yield return null;
        }

        hud.bankruptStamp.anchoredPosition = targetPos;
        hud.bankruptStamp.localScale = Vector3.one;
        
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
            // 파산해도 HUD는 보이되 오버레이로 덮음
            
            // 파산 시 오버레이 활성화
            if (huds[i].bankruptOverlay != null) 
                huds[i].bankruptOverlay.SetActive(p.isBankrupt);

            if (huds[i].nameText  != null) huds[i].nameText.text  = p.playerName + (p.isAI ? " (AI)" : "");
            if (huds[i].moneyText != null) huds[i].moneyText.text = "₩ " + p.money.ToString("N0");
            if (huds[i].ownText   != null) huds[i].ownText.text   = p.ownedTiles.Count + " 개";

            bool isCurrent = (GameManager.Instance.CurrentIndex == i);
            var bg = huds[i].root.GetComponent<Image>();
            if (bg != null) bg.color = isCurrent ? new Color(0.08f,0.08f,0.08f,0.95f) : new Color(0.04f,0.04f,0.04f,0.80f);
            if (huds[i].colorBar != null)
                huds[i].colorBar.color = isCurrent ? PCOLORS[i] : new Color(PCOLORS[i].r*0.5f, PCOLORS[i].g*0.5f, PCOLORS[i].b*0.5f);
        }
    }
}
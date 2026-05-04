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
        GameManager.Instance.OnPlayerBankrupt += _ => Refresh();
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
            huds[i].root.SetActive(!p.isBankrupt);

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
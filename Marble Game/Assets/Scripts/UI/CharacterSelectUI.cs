using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelectUI : MonoBehaviour
{
    static readonly string[] CHAR_NAMES  = { "레드","블루","그린","옐로" };
    static readonly Color[]  CHAR_COLORS = {
        new Color(1f,0.25f,0.25f),
        new Color(0.25f,0.45f,1f),
        new Color(0.25f,0.8f,0.25f),
        new Color(1f,0.8f,0f)
    };

    int selectedIndex = 0;
    int aiCount       = 1;

    Text aiCountText;
    GameObject[] selectedMarks = new GameObject[4];
    GameObject[] slots         = new GameObject[4];

    void Start()
    {
        // 슬롯 & 선택 마크 탐색
        for (int i = 0; i < 4; i++)
        {
            var slotGO = FindDeep(transform, "Slot_" + i);
            if (slotGO != null)
            {
                slots[i] = slotGO;
                selectedMarks[i] = FindDeep(slotGO.transform, "SelectedMark");

                int idx = i;
                var btn = FindDeep(slotGO.transform, "SelectBtn")?.GetComponent<Button>();
                if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => Select(idx)); }
            }
        }

        aiCountText = FindDeep(transform, "AIValue")?.GetComponent<Text>();

        // AI 버튼
        var aiUpBtn   = FindDeep(transform, "AIUpBtn")?.GetComponent<Button>();
        var aiDownBtn = FindDeep(transform, "AIDownBtn")?.GetComponent<Button>();
        if (aiUpBtn   != null) aiUpBtn.onClick.AddListener(()   => ChangeAI(1));
        if (aiDownBtn != null) aiDownBtn.onClick.AddListener(() => ChangeAI(-1));

        // 시작/뒤로 버튼
        var startBtn = FindDeep(transform, "StartBtn")?.GetComponent<Button>();
        var backBtn  = FindDeep(transform, "BackBtn")?.GetComponent<Button>();
        if (startBtn != null) { startBtn.onClick.RemoveAllListeners(); startBtn.onClick.AddListener(OnStart); }
        if (backBtn  != null) { backBtn.onClick.RemoveAllListeners();  backBtn.onClick.AddListener(OnBack);  }

        Select(0);
        UpdateAIText();
        Debug.Log("[CharacterSelectUI] 초기화 완료");
    }

    public void Select(int index)
    {
        selectedIndex = index;
        for (int i = 0; i < 4; i++)
        {
            if (selectedMarks[i] != null) selectedMarks[i].SetActive(i == index);
            if (slots[i] != null)
            {
                var img = slots[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == index)
                        ? new Color(0.14f, 0.16f, 0.26f, 0.98f)
                        : new Color(0.08f, 0.09f, 0.16f, 0.95f);
            }
        }
        Debug.Log("[CharacterSelectUI] 선택: " + CHAR_NAMES[index]);
    }

    void ChangeAI(int delta)
    {
        aiCount = Mathf.Clamp(aiCount + delta, 0, 3);
        UpdateAIText();
    }

    void UpdateAIText()
    {
        if (aiCountText) aiCountText.text = "AI : " + aiCount + "명";
    }

    public void OnStart()
    {
        Debug.Log("[CharacterSelectUI] 게임 시작! 캐릭터=" + CHAR_NAMES[selectedIndex] + " AI=" + aiCount);
        SceneLoader.SelectedCharacterIndex = selectedIndex;
        SceneLoader.AIPlayerCount = aiCount;
        SceneLoader.HumanPlayerCount = 1;
        SceneManager.LoadScene(2); 
    }

    public void OnBack()
    {
        SceneManager.LoadScene(0); // MainMenu
    }

    GameObject FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent.gameObject;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindDeep(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
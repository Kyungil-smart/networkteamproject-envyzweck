using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject settingsPanel;

    [Header("Settings")]
    public Slider  bgmSlider;
    public Slider  sfxSlider;
    public Toggle  fullscreenToggle;
    public Text    bgmValueText;
    public Text    sfxValueText;

    float bgmVolume = 0.8f;
    float sfxVolume = 0.8f;

    void Start()
    {
        // 버튼 자동 탐색 & 연결
        BindButton("SinglePlayBtn",  OnSinglePlay);
        BindButton("NetworkPlayBtn", OnNetworkPlay);
        BindButton("SettingsBtn",    OnSettings);
        BindButton("QuitBtn",        OnQuit);
        BindButton("CloseBtn",       OnCloseSettings);

        // 설정값 불러오기
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        if (bgmSlider)        { bgmSlider.value = bgmVolume;   bgmSlider.onValueChanged.AddListener(v => { bgmVolume=v; UpdateVolumeTexts(); }); }
        if (sfxSlider)        { sfxSlider.value = sfxVolume;   sfxSlider.onValueChanged.AddListener(v => { sfxVolume=v; UpdateVolumeTexts(); }); }
        if (fullscreenToggle) { fullscreenToggle.isOn = Screen.fullScreen; fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v); }

        UpdateVolumeTexts();
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    // 씬 전체에서 이름으로 버튼 찾아 연결
    void BindButton(string btnName, UnityEngine.Events.UnityAction action)
    {
        var go = FindDeep(transform, btnName);
        if (go == null) { Debug.LogWarning("[MainMenuUI] 버튼 없음: " + btnName); return; }
        var btn = go.GetComponent<Button>();
        if (btn == null) { Debug.LogWarning("[MainMenuUI] Button 컴포넌트 없음: " + btnName); return; }
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
        Debug.Log("[MainMenuUI] 버튼 연결: " + btnName);
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

    // 버튼 콜백
    public void OnSinglePlay()
    {
        Debug.Log("[MainMenuUI] 싱글 플레이 클릭!");
        SceneLoader.IsSinglePlay     = true;
        SceneLoader.HumanPlayerCount = 1;
        SceneManager.LoadScene(1); // CharacterSelect
    }

    public void OnNetworkPlay()
    {
        Debug.Log("[MainMenuUI] 네트워크 플레이 클릭!");
        SceneLoader.IsSinglePlay = false;
        SceneManager.LoadScene(1);
    }

    public void OnSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OnQuit()
    {
        Debug.Log("[MainMenuUI] 게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnCloseSettings()
    {
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    void UpdateVolumeTexts()
    {
        if (bgmValueText) bgmValueText.text = Mathf.RoundToInt(bgmVolume * 100) + "%";
        if (sfxValueText) sfxValueText.text = Mathf.RoundToInt(sfxVolume * 100) + "%";
    }
}
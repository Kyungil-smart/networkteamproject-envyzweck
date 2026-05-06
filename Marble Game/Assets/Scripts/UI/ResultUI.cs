using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class ResultUI : MonoBehaviour
{
    [Header("카메라 설정")]
    [SerializeField] private Camera resultCamera; 

    [Header("우승자 정보 UI")]
    [SerializeField] private TMP_Text winnerNameText;
    [SerializeField] private Transform winnerSpawnPoint;

    [System.Serializable]
    public struct CharacterColor
    {
        public string prefabName; 
        public Color bgColor;
    }
    
    [Header("캐릭터별 배경색 리스트")]
    [SerializeField] private CharacterColor[] characterColors;

    void Start()
    {
        DisplayWinner();
    }

    private void DisplayWinner()
    {
        string winnerName = PlayerPrefs.GetString("WinnerName", "Unknown");
        string prefabName = PlayerPrefs.GetString("WinnerPrefabName", "");

        if (winnerNameText != null)
        {
            winnerNameText.text = winnerName;
        }

        ApplyBackgroundColor(prefabName);

        if (winnerSpawnPoint != null && !string.IsNullOrEmpty(prefabName))
        {
            GameObject prefab = Resources.Load<GameObject>("Characters/" + prefabName);
            
            if (prefab != null)
            {
                Instantiate(prefab, winnerSpawnPoint.position, winnerSpawnPoint.rotation, winnerSpawnPoint);
            }
            else
            {
                Debug.LogWarning($"[ResultUI] {prefabName} 프리팹을 찾을 수 없습니다.");
            }
        }
    }

    private void ApplyBackgroundColor(string prefabName)
    {
        if (resultCamera == null || characterColors == null || characterColors.Length == 0) return;

        resultCamera.clearFlags = CameraClearFlags.SolidColor;

        foreach (var item in characterColors)
        {
            if (item.prefabName == prefabName)
            {
                StopAllCoroutines(); 
                // 우승자 색상을 기반으로 채도 순환 시작
                StartCoroutine(CycleSaturation(item.bgColor));
                return;
            }
        }

        resultCamera.backgroundColor = Color.gray;
    }

    // 우승자 색상의 채도만 미세하게 순환 변경
    private IEnumerator CycleSaturation(Color baseColor)
    {
        resultCamera.backgroundColor = baseColor;

        float h, s, v;
        Color.RGBToHSV(baseColor, out h, out s, out v);

        while (true)
        {
            float wave = Mathf.Sin(Time.time * 1.5f) * 0.1f; 
            
            float newS = Mathf.Clamp01(s + wave);
            
            resultCamera.backgroundColor = Color.HSVToRGB(h, newS, v);
            yield return null;
        }
    }

        public void OnClickRestart()
        {
            SceneLoader.HumanPlayerCount = PlayerPrefs.GetInt("LastHumanCount", 1);
            SceneLoader.AIPlayerCount = PlayerPrefs.GetInt("LastAICount", 1);
            SceneManager.LoadScene("GameScene");
        }

    public void OnClickMainTitle()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
using UnityEngine;
using TMPro;

public class TileDisplay : MonoBehaviour
{
    [Header("Tile Data")]
    public int tileIndex; 

    [Header("3D UI Components (No Canvas)")]

    public TextMeshPro nameText; 
    public TextMeshPro priceText;

    public SpriteRenderer backgroundRenderer; 

    private void Start()
    {
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (BoardManager.Instance == null || BoardManager.Instance.Tiles == null) return;
        if (tileIndex < 0 || tileIndex >= BoardManager.Instance.Tiles.Length) return;

        var data = BoardManager.Instance.Tiles[tileIndex];
        if (data == null) return;

        // 이름 및 가격 표시
        if (nameText != null) nameText.text = data.tileName;
        if (priceText != null)
        {
            if (data.type == TileDataSO.TileType.Property)
                priceText.text = GetFormattedPrice(data.price);
            else
                priceText.text = ""; 
        }

        // 소유주에 따른 색상 변경
        UpdateOwnerColor(data);
    }

    // 금액 '만'단위 변환
    private string GetFormattedPrice(int price)
    {
        if (price >= 10000)
        {
            float simplified = price / 10000f;
            return simplified.ToString("G29") + "만";
        }
        
        // 1만 원 미만은 그냥 숫자로 표시
        return price.ToString("N0");
    }

    public void UpdateOwnerColor(TileDataSO data)
    {
        if (backgroundRenderer == null) return;

        if (data.ownerIndex >= 0)
        {
            backgroundRenderer.color = GameManager.Instance.GetPlayerColor(data.ownerIndex);
        }
        else
        {
            backgroundRenderer.color = (data.type == TileDataSO.TileType.Property) ? data.tileColor : Color.white;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [SerializeField] GameObject purchasePanel;
    [SerializeField] Text panelText;
    
    // 구매할 타일의 번호를 기억할 변수 추가
    private int _currentBuyingTileIndex; 

    void Awake() => Instance = this;
    
    public void ShowPurchaseDialog(TileData data, int tileIndex) 
    {
        _currentBuyingTileIndex = tileIndex; // 넘겨받은 인덱스를 저장
        purchasePanel.SetActive(true);
        panelText.text = $"{data.tileName}을(를) {data.price}원에 구매하시겠습니까?";
    }
    
    public void OnClickBuy()
    {
        // GameManager에서 현재 턴인 플레이어 인덱스를 가져옴
        int pIdx = GameManager.Instance.CurrentIndex;
        
        // 저장해둔 인덱스를 사용하여 구매 처리
        BoardManager.Instance.BuyTile(pIdx, _currentBuyingTileIndex);
        purchasePanel.SetActive(false);
    }
}
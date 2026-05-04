using UnityEngine;

[CreateAssetMenu(fileName = "BoardConfig_New", menuName = "BoardGame/Board Config")]
public class BoardConfigSO : ScriptableObject
{
    [Header("보드 이름")]
    public string boardName = "Korea Map";

    [Header("40칸 타일 데이터")]
    public TileDataSO[] tiles = new TileDataSO[40];

    public TileDataSO GetTile(int index)
    {
        if (index < 0 || index >= tiles.Length) return null;
        return tiles[index];
    }
}
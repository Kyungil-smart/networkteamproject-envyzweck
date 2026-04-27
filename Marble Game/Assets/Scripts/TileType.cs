using UnityEngine;

public enum TileType { Empty, City, GoldKey, Jail, Start }

[CreateAssetMenu(fileName = "NewTile", menuName = "Board/Tile")]
public class TileData : ScriptableObject
{
    public string tileName;
    public TileType type;
    public int price;         // 땅값
    public int toll;          // 통행료
}
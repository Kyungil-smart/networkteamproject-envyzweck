using UnityEngine;

[CreateAssetMenu(fileName = "Tile_New", menuName = "BoardGame/Tile Data")]
public class TileDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public int      tileIndex;
    public string   tileName;
    public TileType type;

    [Header("땅 칸 전용")]
    public int      price;
    public int      baseRent;
    public int[]    buildingRent = new int[4]; // 건물 1~4채
    public int      hotelRent;
    public int      buildingCost;
    public Color    tileColor = Color.white;

    [Header("세금 칸 전용")]
    public int      taxAmount;
    [Range(0f, 1f)]
    public float    taxRate;

    [System.NonSerialized] public int  ownerIndex    = -1;
    [System.NonSerialized] public int  buildingCount = 0;
    [System.NonSerialized] public bool hasHotel      = false;

    public enum TileType { Start, Property, Event, Jail, FreeParking, Tax }

    public int GetCurrentRent()
    {
        if (type != TileType.Property || ownerIndex < 0) return 0;
        if (hasHotel)          return hotelRent;
        if (buildingCount > 0) return buildingRent[Mathf.Clamp(buildingCount - 1, 0, 3)];
        return baseRent;
    }

    public bool CanBuild() =>
        type == TileType.Property && ownerIndex >= 0 && !hasHotel && buildingCount < 4;

    // 런타임 상태 초기화
    public void ResetRuntimeState()
    {
        ownerIndex    = -1;
        buildingCount = 0;
        hasHotel      = false;
    }
}
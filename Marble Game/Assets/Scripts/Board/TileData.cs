using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TileData
{
    public enum TileType { Start, Property, Event, Jail, FreeParking, Tax }

    public int      tileIndex;
    public string   tileName;
    public TileType type;
    public int      price;
    public int      baseRent;
    public int[]    buildingRent;
    public int      hotelRent;
    public int      buildingCost;
    public int      ownerIndex;
    public int      buildingCount;
    public bool     hasHotel;
    public Color    tileColor;
    public int      taxAmount;
    public float    taxRate;

    public TileData(int index, string name, TileType tileType)
    {
        tileIndex = index; tileName = name; type = tileType;
        ownerIndex = -1; buildingCount = 0; hasHotel = false;
        buildingRent = new int[4];
    }

    public int GetCurrentRent()
    {
        if (type != TileType.Property || ownerIndex < 0) return 0;
        if (hasHotel) return hotelRent;
        if (buildingCount > 0) return buildingRent[buildingCount - 1];
        return baseRent;
    }

    public bool CanBuild() => type == TileType.Property && ownerIndex >= 0 && !hasHotel && buildingCount < 4;
}
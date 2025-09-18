using UnityEngine;

public enum TileType
{
    Walkable,    // �̵� ����
    Blocked,     // ���� Ÿ��
    Start,       // ������
    Goal         // ������
}

[CreateAssetMenu(fileName = "New Tile Data", menuName = "PathPath/Tile Data")]
public class TileData : ScriptableObject
{
    [Header("Tile Settings")]
    public TileType tileType;
    public string tileName;
    public Sprite tileSprite;
    public Color tileColor = Color.white;

    [Header("Gameplay Properties")]
    public bool isWalkable = true;
    public bool blocksMovement = false;

    [Header("Visual")]
    public Material tileMaterial;

    public bool CanWalkOn()
    {
        return isWalkable && !blocksMovement;
    }
}
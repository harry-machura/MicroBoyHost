using MicroBoyCart.Sample.Maps;
using MicroBoyCart.Sample.Tiles;

namespace MicroBoyCart.Sample.Gameplay;

public class PlayerState
{
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int PixelX { get; set; }
    public int PixelY { get; set; }
    public int TargetPixelX { get; set; }
    public int TargetPixelY { get; set; }
    public bool IsMoving { get; set; }
    public int StepSpeedPx { get; set; } = 2;
    public int Direction { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }
    public double DamageCooldownTimer { get; set; }
    public bool HasSurfAbility { get; set; }
    public WarpPoint? PendingWarp { get; set; }
    public double TotalPlayTime { get; set; }

    public void SnapToTile(int tileX, int tileY)
    {
        TileX = tileX;
        TileY = tileY;
        PixelX = tileX * Tileset.TileWidth;
        PixelY = tileY * Tileset.TileHeight;
        TargetPixelX = PixelX;
        TargetPixelY = PixelY;
        IsMoving = false;
    }
}

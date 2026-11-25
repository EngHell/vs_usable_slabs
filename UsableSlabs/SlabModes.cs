using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace UsableSlabs;

/// <summary>
/// Tool-mode layout of the F menu and the mapping from a locked side to a block <c>rot</c> variant.
/// </summary>
public static class SlabModes
{
    /// <summary>Vanilla attribute on the item stack: 0 auto, 1 horizontal only, 2 vertical only.</summary>
    public const string VanillaModeAttr = "slabPlaceMode";

    /// <summary>Our attribute on the item stack: locked side index (see <see cref="Side"/>). Absent = vanilla behaviour.</summary>
    public const string LockedSideAttr = "usableslabsSide";

    // F menu indices
    public const int Auto = 0, Horizontal = 1, Vertical = 2;
    public const int FirstSide = 3;          // 3..8 → MenuOrder[0..5]
    public const int SideCount = 6;
    public const int ToggleRelative = FirstSide + SideCount; // 9
    public const int Count = ToggleRelative + 1;

    /// <summary>
    /// Locked sides. Horizontal ones are clockwise: in compass mode North/East/South/West,
    /// in relative mode Far/Right/Near/Left (Far = the side of the block away from the player).
    /// </summary>
    public enum Side { Down = 0, Up = 1, H0 = 2, H1 = 3, H2 = 4, H3 = 5 }

    /// <summary>
    /// Order of the locked sides in the F menu: opposite sides next to each other.
    /// Compass: Down, Up, North, South, East, West. Relative: Down, Up, Far, Near, Right, Left.
    /// The same slot means the same stored side in both modes, so client and server always agree.
    /// </summary>
    private static readonly Side[] MenuOrder = { Side.Down, Side.Up, Side.H0, Side.H2, Side.H1, Side.H3 };

    /// <summary>Side for an F-menu index in [FirstSide, ToggleRelative).</summary>
    public static Side SideAtMenu(int toolMode) => MenuOrder[toolMode - FirstSide];

    public static int MenuIndexOf(Side side) => FirstSide + Array.IndexOf(MenuOrder, side);

    private static readonly BlockFacing[] Clockwise = { BlockFacing.NORTH, BlockFacing.EAST, BlockFacing.SOUTH, BlockFacing.WEST };

    public static Side? GetLockedSide(ItemStack? stack)
    {
        if (stack?.Attributes == null || !stack.Attributes.HasAttribute(LockedSideAttr)) return null;
        int v = stack.Attributes.GetInt(LockedSideAttr, -1);
        return v >= 0 && v < SideCount ? (Side)v : null;
    }

    /// <summary>The facing the slab should be placed against, i.e. the value of its <c>rot</c> variant.</summary>
    public static BlockFacing Resolve(Side side, bool relative, IPlayer? player)
    {
        switch (side)
        {
            case Side.Down: return BlockFacing.DOWN;
            case Side.Up: return BlockFacing.UP;
        }

        int k = (int)side - (int)Side.H0;
        if (!relative || player?.Entity == null) return Clockwise[k];

        int far = Array.IndexOf(Clockwise, LookFacing(player.Entity.Pos.Yaw));
        return Clockwise[(far + k) % 4];
    }

    /// <summary>Horizontal facing closest to where the player is looking.</summary>
    /// <remarks>
    /// Uses the game's own yaw convention. Deriving it from EntityPos.GetViewVector gave the opposite
    /// direction for the player's Pos.Yaw (far/near and left/right came out swapped in game).
    /// </remarks>
    public static BlockFacing LookFacing(float yaw) => BlockFacing.HorizontalFromYaw(yaw);

    /// <summary>Lang key (without domain) for a locked side's name.</summary>
    public static string SideLangKey(Side side, bool relative) => side switch
    {
        Side.Down => "side-down",
        Side.Up => "side-up",
        _ => (relative ? RelativeKeys : CompassKeys)[(int)side - (int)Side.H0]
    };

    private static readonly string[] CompassKeys = { "side-north", "side-east", "side-south", "side-west" };
    private static readonly string[] RelativeKeys = { "side-far", "side-right", "side-near", "side-left" };
}

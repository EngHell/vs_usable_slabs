using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace UsableSlabs;

/// <summary>
/// Added in front of OmniRotatable on slab blocks. Supplies the F (tool mode) menu and, when a side is
/// locked on the held stack, places the slab with exactly that <c>rot</c> regardless of where you click.
/// Without a locked side it passes through, so vanilla placement runs unchanged.
/// </summary>
public class SlabPlacementBehavior : BlockBehavior
{
    public const string ClassName = "usableslabs.SlabPlacement";

    public SlabPlacementBehavior(Block block) : base(block) { }

    private static UsableSlabsModSystem? Sys(ICoreAPI? api) => api?.ModLoader.GetModSystem<UsableSlabsModSystem>();

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
    {
        var side = SlabModes.GetLockedSide(itemstack);
        if (side == null) return false;

        bool relative = Sys(world.Api)?.IsRelative(byPlayer) ?? true;
        var facing = SlabModes.Resolve(side.Value, relative, byPlayer);
        var target = world.GetBlock(block.CodeWithVariant("rot", facing.Code));
        if (target == null || target.Id == 0) return false; // no such variant: let vanilla handle it

        handling = EnumHandling.PreventSubsequent;
        if (!target.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;
        world.BlockAccessor.SetBlock(target.BlockId, blockSel.Position);
        return true;
    }

    public override SkillItem[]? GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        return Sys(forPlayer.Entity?.Api)?.GetToolModeItems(forPlayer);
    }

    public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection)
    {
        var stack = slot.Itemstack;
        var side = SlabModes.GetLockedSide(stack);
        if (side != null) return SlabModes.MenuIndexOf(side.Value);
        return stack?.Attributes?.GetInt(SlabModes.VanillaModeAttr, 0) ?? 0;
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
    {
        var stack = slot.Itemstack;
        if (stack == null) return;

        if (toolMode == SlabModes.ToggleRelative)
        {
            // Preference, not a stack property. The client flips its config and tells the server.
            if (byPlayer.Entity?.Api is ICoreClientAPI) Sys(byPlayer.Entity.Api)?.ToggleRelative();
            return;
        }

        var attrs = stack.Attributes;
        if (toolMode >= SlabModes.FirstSide && toolMode < SlabModes.ToggleRelative)
        {
            var side = SlabModes.SideAtMenu(toolMode);
            attrs.SetInt(SlabModes.LockedSideAttr, (int)side);
            // Keep the vanilla attribute in step so the held-item tilt matches.
            bool flat = side is SlabModes.Side.Down or SlabModes.Side.Up;
            attrs.SetInt(SlabModes.VanillaModeAttr, flat ? 1 : 2);
        }
        else if (toolMode >= SlabModes.Auto && toolMode <= SlabModes.Vertical)
        {
            attrs.RemoveAttribute(SlabModes.LockedSideAttr);
            if (toolMode == SlabModes.Auto) attrs.RemoveAttribute(SlabModes.VanillaModeAttr);
            else attrs.SetInt(SlabModes.VanillaModeAttr, toolMode);
        }
        slot.MarkDirty();
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe, ref EnumHandling handled)
    {
        // Cycling the mode in the crafting grid goes back to the vanilla modes.
        outputSlot.Itemstack?.Attributes.RemoveAttribute(SlabModes.LockedSideAttr);
        base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe, ref handled);
    }

    public override string GetHeldBlockInfo(IWorldAccessor world, ItemSlot inSlot)
    {
        var side = SlabModes.GetLockedSide(inSlot.Itemstack);
        if (side == null) return base.GetHeldBlockInfo(world, inSlot);
        bool relative = Sys(world.Api)?.IsRelative(null) ?? true;
        return Lang.Get("usableslabs:heldinfo-locked", Lang.Get("usableslabs:" + SlabModes.SideLangKey(side.Value, relative))) + "\n";
    }
}

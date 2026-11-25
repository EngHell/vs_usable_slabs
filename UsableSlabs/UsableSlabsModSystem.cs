using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace UsableSlabs;

[ProtoContract]
public class RelativePrefMessage
{
    [ProtoMember(1)] public bool Relative;
}

public class UsableSlabsModSystem : ModSystem
{
    private const string ChannelName = "usableslabs";

    private ICoreClientAPI? capi;
    private IClientNetworkChannel? clientChannel;
    private UsableSlabsConfig? config;
    private SkillItem[]? relativeModes;
    /// <summary>Compass-mode icon sets, one per view direction (0 north … 3 west), so the icons match where you look.</summary>
    private readonly SkillItem[]?[] compassModes = new SkillItem[4][];

    /// <summary>Server side: each player's relative/compass preference, by UID. Unknown = relative.</summary>
    private readonly Dictionary<string, bool> serverPrefs = new();

    private static readonly FieldInfo? RotateSidesField =
        typeof(BlockBehaviorOmniRotatable).GetField("rotateSides", BindingFlags.Instance | BindingFlags.NonPublic);

    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockBehaviorClass(SlabPlacementBehavior.ClassName, typeof(SlabPlacementBehavior));
        api.Network.RegisterChannel(ChannelName).RegisterMessageType<RelativePrefMessage>();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Network.GetChannel(ChannelName).SetMessageHandler<RelativePrefMessage>((player, msg) => serverPrefs[player.PlayerUID] = msg.Relative);
        api.Event.PlayerDisconnect += player => serverPrefs.Remove(player.PlayerUID);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        config = UsableSlabsConfig.Load(api);
        clientChannel = api.Network.GetChannel(ChannelName);
        api.Event.LevelFinalize += SendPref;
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        int count = 0;
        foreach (var block in api.World.Blocks)
        {
            if (block?.Code == null || !IsSlab(api, block)) continue;
            if (block.HasBehavior<SlabPlacementBehavior>()) { count++; continue; } // already sent by the server

            var bh = new SlabPlacementBehavior(block);
            bh.Initialize(new JsonObject(new Newtonsoft.Json.Linq.JObject()));
            block.BlockBehaviors = block.BlockBehaviors.Prepend(bh).ToArray();
            block.CollectibleBehaviors = block.CollectibleBehaviors.Prepend(bh).ToArray();
            count++;
        }
        api.Logger.Notification("[usableslabs] Slab placement modes enabled on {0} blocks ({1}).", count, api.Side);
    }

    /// <summary>OmniRotatable with rotateSides, and all six <c>rot</c> variants exist.</summary>
    private static bool IsSlab(ICoreAPI api, Block block)
    {
        var omni = block.GetBehavior<BlockBehaviorOmniRotatable>();
        if (omni == null || block.Variant?["rot"] == null) return false;
        if (RotateSidesField?.GetValue(omni) is false) return false;
        foreach (var f in BlockFacing.ALLFACES)
        {
            if (api.World.GetBlock(block.CodeWithVariant("rot", f.Code)) == null) return false;
        }
        return true;
    }

    /// <summary>Whether locked horizontal sides are relative to <paramref name="player"/>'s view.</summary>
    public bool IsRelative(IPlayer? player)
    {
        if (capi != null) return config?.RelativeDirections ?? true;
        return player == null || !serverPrefs.TryGetValue(player.PlayerUID, out bool rel) || rel;
    }

    public void ToggleRelative()
    {
        if (capi == null || config == null) return;
        config.RelativeDirections = !config.RelativeDirections;
        config.Save(capi);
        SendPref();
        capi.ShowChatMessage(Lang.Get(config.RelativeDirections ? "usableslabs:msg-now-relative" : "usableslabs:msg-now-compass"));
    }

    private void SendPref()
    {
        if (clientChannel?.Connected == true && config != null)
        {
            clientChannel.SendPacket(new RelativePrefMessage { Relative = config.RelativeDirections });
        }
    }

    public SkillItem[]? GetToolModeItems(IPlayer forPlayer)
    {
        if (capi == null) return null;
        bool rel = config?.RelativeDirections ?? true;
        if (rel) return relativeModes ??= BuildModes(capi, true, 0);

        // BlockFacing indices for horizontals are north 0, east 1, south 2, west 3.
        int q = forPlayer.Entity == null ? 0 : SlabModes.LookFacing(forPlayer.Entity.Pos.Yaw).Index;
        return compassModes[q] ??= BuildModes(capi, false, q);
    }

    private static SkillItem[] BuildModes(ICoreClientAPI capi, bool relative, int quarterTurns)
    {
        var items = new SkillItem[SlabModes.Count];
        for (int i = 0; i < SlabModes.Count; i++)
        {
            string key = i switch
            {
                SlabModes.Auto => "mode-auto",
                SlabModes.Horizontal => "mode-horizontal",
                SlabModes.Vertical => "mode-vertical",
                SlabModes.ToggleRelative => relative ? "mode-toggle-to-compass" : "mode-toggle-to-relative",
                _ => SlabModes.SideLangKey(SlabModes.SideAtMenu(i), relative)
            };
            items[i] = new SkillItem
            {
                Code = new AssetLocation("usableslabs", key),
                Name = Lang.Get("usableslabs:" + key),
                Linebreak = i == SlabModes.FirstSide || i == SlabModes.ToggleRelative
            }.WithIcon(capi, SlabModeIcons.For(i, relative, quarterTurns));
        }
        return items;
    }

    public override void Dispose()
    {
        foreach (var set in compassModes.Append(relativeModes))
        {
            foreach (var item in set ?? Array.Empty<SkillItem>()) item.Dispose();
        }
        relativeModes = null;
        Array.Clear(compassModes);
        if (capi != null) capi.Event.LevelFinalize -= SendPref;
    }
}

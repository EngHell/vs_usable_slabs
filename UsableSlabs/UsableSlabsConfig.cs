using System;
using Vintagestory.API.Client;

namespace UsableSlabs;

/// <summary>Client settings, saved to ModConfig/usableslabs.json.</summary>
public class UsableSlabsConfig
{
    public const string FileName = "usableslabs.json";

    /// <summary>
    /// true: locked horizontal sides are Far/Right/Near/Left relative to where you look.
    /// false: they are compass directions North/East/South/West.
    /// </summary>
    public bool RelativeDirections { get; set; } = true;

    public static UsableSlabsConfig Load(ICoreClientAPI capi)
    {
        try
        {
            var cfg = capi.LoadModConfig<UsableSlabsConfig>(FileName);
            if (cfg != null) return cfg;
        }
        catch (Exception e)
        {
            capi.Logger.Warning("[usableslabs] Could not read {0}, using defaults: {1}", FileName, e.Message);
        }
        var fresh = new UsableSlabsConfig();
        fresh.Save(capi);
        return fresh;
    }

    public void Save(ICoreClientAPI capi)
    {
        try { capi.StoreModConfig(this, FileName); }
        catch (Exception e) { capi.Logger.Warning("[usableslabs] Could not save {0}: {1}", FileName, e.Message); }
    }
}

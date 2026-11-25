using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace UsableSlabs;

public class UsableSlabsModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.Logger.Notification("[usableslabs] Loaded.");
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
    }
}

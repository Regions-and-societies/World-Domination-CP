using RegionsAndSocieties.Integration;
using Verse;

namespace RegionsAndSocieties.WorldDominationCP
{
    /// <summary>
    /// Mod entry. Loads after Regions and Societies core (whose constructor initialises the adapter
    /// registry), so registering here merges this patch's typed adapter into the priority-ordered
    /// set. Core carries no World Domination knowledge of its own — this registration is the whole
    /// integration.
    /// </summary>
    public class WorldDominationCPMod : Mod
    {
        public WorldDominationCPMod(ModContentPack content) : base(content)
        {
            WorldObjectAdapterRegistry.Register(new WorldDominationAdapter());
            Log.Message("[RegionsAndSocieties.WorldDominationCP] Registered the World Domination 2.0 adapter (priority 130).");
        }
    }
}

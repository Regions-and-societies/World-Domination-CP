using System;
using System.Runtime.CompilerServices;
using RegionsAndSocieties.Integration;
using Verse;

namespace RegionsAndSocieties.WorldDominationCP
{
    /// <summary>
    /// Mod entry. Loads after Regions and Societies core (whose constructor initialises the adapter
    /// registry), so registering here merges this patch's typed adapter into the priority-ordered
    /// set. Core carries no World Domination knowledge of its own — this registration is the whole
    /// integration.
    ///
    /// About.xml declares no hard dependency on a core edition: the two editions
    /// (<c>RegionsAndSocieties.Core</c> on Map Mode Framework, <c>RegionsAndSocieties.CoreRP2</c> on
    /// Realistic Planets 2) are mutually exclusive, and modDependencies cannot express "either of" —
    /// declaring one falsely flags the other edition's users. So core presence is checked here
    /// instead, and a missing core degrades to a warning rather than a type-load error.
    /// </summary>
    public class WorldDominationCPMod : Mod
    {
        public WorldDominationCPMod(ModContentPack content) : base(content)
        {
            if (!CoreLoaded())
            {
                Log.Warning("[RegionsAndSocieties.WorldDominationCP] Regions and Societies is not loaded — check your mod list to ensure the Regions and Societies (Realistic Planets 2) or the standard Map Mode Framework edition is active. The World Domination 2.0 adapter was not registered.");
                return;
            }

            Register();
        }

        // Both editions ship the same "RegionsAndSocieties" assembly with an identical public API,
        // and RimWorld loads every active mod's assemblies before constructing any Mod class — so
        // scanning the domain detects whichever edition is present, regardless of load order or of
        // packageId suffixes that ModsConfig.IsActive would miss on local copies.
        private static bool CoreLoaded()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "RegionsAndSocieties") return true;
            }

            return false;
        }

        // NoInlining keeps the RegionsAndSocieties type references out of the constructor's JIT
        // scope, so a missing core reaches the warning above instead of a TypeLoadException.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Register()
        {
            WorldObjectAdapterRegistry.Register(new WorldDominationAdapter());
            Log.Message("[RegionsAndSocieties.WorldDominationCP] Registered the World Domination 2.0 adapter (priority 130).");
        }
    }
}

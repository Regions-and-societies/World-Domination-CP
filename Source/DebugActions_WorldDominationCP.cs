using System.Text;
using LudeonTK;
using RimWorld.Planet;
using RegionsAndSocieties.Integration;
using Verse;

namespace RegionsAndSocieties.WorldDominationCP
{
    /// <summary>
    /// Debug validation for the patch (per the workspace debug-command gate): dump every World
    /// Domination world object with its resolved kind, population and owning faction, so parity
    /// against the old reflection path is verifiable headlessly via run_debug_action.
    /// </summary>
    public static class DebugActions_WorldDominationCP
    {
        [DebugAction("Regions and Societies", "R&S WD-CP: world-object dump", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap | AllowedGameStates.PlayingOnWorld)]
        private static void WorldDominationObjectDump()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- WD-CP world-object dump ---");
            int count = 0;
            var objects = Find.WorldObjects != null ? Find.WorldObjects.AllWorldObjects : null;
            if (objects != null)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    WorldObject obj = objects[i];
                    if (obj == null || obj.GetType().Namespace != "TSA_WorldDomination") continue;

                    count++;
                    WorldObjectKind kind;
                    WorldObjectAdapterRegistry.TryClassify(obj, out kind);
                    int pop;
                    bool hasPop = WorldObjectAdapterRegistry.TryGetPopulation(obj, out pop);
                    sb.AppendLine($"  {obj.GetType().Name,-40} tile={obj.Tile,-7} kind={kind,-10} pop={(hasPop ? pop.ToString() : "-"),-5} faction={obj.Faction?.Name ?? "none"}");
                }
            }
            sb.AppendLine($"{count} World Domination world object(s).");
            Log.Message(sb.ToString());
        }
    }
}

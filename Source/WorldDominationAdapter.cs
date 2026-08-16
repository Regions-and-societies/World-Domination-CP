using RimWorld.Planet;
using RegionsAndSocieties.Integration;
using TSA_WorldDomination;
using Verse;

namespace RegionsAndSocieties.WorldDominationCP
{
    /// <summary>
    /// Typed adapter for World Domination 2.0 (packageId <c>TSA.WorldDominationExperimental</c>,
    /// assembly and namespace <c>TSA_WorldDomination</c>). Replaces core's string-declared
    /// reflection profile: this assembly hard-references the real one, so a wrong type or member
    /// name is a compile error here instead of a silent zero in-game (the R&T #30 failure class).
    ///
    /// <para>Findings carried over from the core profile, all re-verified against the shipped
    /// assembly at compile time:</para>
    /// <list type="bullet">
    /// <item><b>Travelers are matched first.</b> Roughly half of this mod's world objects are
    /// <see cref="WorldObject_Traveler"/> subclasses — raids, drop pods, road builders, purchase
    /// parties. They move; they are caravans, not holdings. The typed <c>is</c> check covers every
    /// subclass, which the old name-contains rule only approximated.</item>
    /// <item><b>There is no level member, and that is a finding rather than a gap.</b> Settlement
    /// grade is encoded in def names (<c>TSA_Generic_T1_Farming</c>…<c>T4_Citadel</c>) on vanilla
    /// <c>Settlement</c> objects, and upgrades are separate purchases with per-line tiers. No scalar
    /// on the world object describes that, so <c>TryGetLevel</c> stays unimplemented.</item>
    /// <item><b>Its faction bases are vanilla <c>Settlement</c>s with modded defs</b>, already
    /// classified by the vanilla adapter; this adapter covers only the types the mod introduces.</item>
    /// <item><b><c>WorldObject_WD_Outpost</c> does not derive from <c>Outposts.Outpost</c></b> —
    /// probed by compile: the shared member names (<c>PawnCount</c>) are a coincidence, exactly as
    /// the old profile suspected. Population reads the type's own member, typed.</item>
    /// </list>
    ///
    /// <para>Two classifications the reflection profile could not express, now deliberate:</para>
    /// <list type="bullet">
    /// <item><see cref="WorldObject_AT_Turret"/> → <see cref="WorldObjectKind.Military"/>: an armed
    /// emplacement is a military installation, and military installations hold ground under the 0.7
    /// governance rules. The old namespace fallback flattened it to Outpost.</item>
    /// <item><see cref="WorldObject_WdSettlementRuin"/> → <see cref="WorldObjectKind.Site"/>: a ruin
    /// is scenery, not a territorial holding. Previously flattened to Outpost, which made destroyed
    /// settlements keep claiming territory.</item>
    /// </list>
    /// </summary>
    public class WorldDominationAdapter : WorldObjectAdapterBase
    {
        public override string AdapterId { get { return "worlddomination"; } }

        public override string DisplayName { get { return "World Domination 2.0"; } }

        /// <summary>Same slot as core's old reflection profile: after VOE (110) and VFE (120/121).</summary>
        public override int Priority { get { return 130; } }

        // If this assembly resolved at all, the WD assembly is loaded — a hard dependency that is
        // missing fails loudly at mod-load instead of silently here. The ModsConfig check guards
        // the residual case of a stale load order where the DLL exists but the mod is disabled.
        private static readonly bool Active = ModsConfig.IsActive("TSA.WorldDominationExperimental");

        public override bool IsActive { get { return Active; } }

        public override bool TryClassify(WorldObject obj, out WorldObjectKind kind)
        {
            kind = WorldObjectKind.Unknown;
            if (obj == null) return false;

            // Order is load-bearing: WorldObject_Traveler_Outpost_* subclasses contain both words;
            // travelling to an outpost is not being one.
            if (obj is WorldObject_Traveler) { kind = WorldObjectKind.Caravan; return true; }
            if (obj is WorldObject_AT_Turret) { kind = WorldObjectKind.Military; return true; }
            if (obj is WorldObject_WdSettlementRuin) { kind = WorldObjectKind.Site; return true; }
            if (obj is WorldObject_WD_Outpost) { kind = WorldObjectKind.Outpost; return true; }

            // Anything this mod introduces later: an outpost is the safer default than a settlement,
            // since outposts carry less territorial weight than settlements do.
            if (obj.GetType().Namespace == "TSA_WorldDomination") { kind = WorldObjectKind.Outpost; return true; }

            return false;
        }

        public override bool TryGetPopulation(WorldObject obj, out int population)
        {
            population = 0;
            var outpost = obj as WorldObject_WD_Outpost;
            if (outpost == null) return false;

            population = outpost.PawnCount;
            return population >= 0;
        }
    }
}

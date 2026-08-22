using System;
using RimWorld;
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
    /// <item><b>Settlement grade is a live typed field, and this adapter now reports it.</b> The
    /// <c>TSA_Generic_T1_Farming</c>…<c>T4_Citadel</c> names are <c>KCSG.SettlementLayoutDef</c>s used
    /// at map generation, not the world object's def — WD's faction bases are vanilla
    /// <c>Settlement</c>s. Grade lives on a <see cref="CompViralSpread"/> world-object comp that WD
    /// patches onto the vanilla <c>Settlement</c> def, in the <c>tier</c> field
    /// (<see cref="SettlementTier"/> T1…T4), and upgrades mutate it in place — so reading it always
    /// yields the current tier. <see cref="TryGetLevel"/> maps that onto R&amp;S sizing; see its notes.</item>
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

            // WD's faction bases are vanilla Settlements — the vanilla adapter would classify them
            // identically. We take ownership only of the WD-managed ones so that core will consult
            // this adapter's TryGetLevel for them: core reads a level only from the adapter that
            // also classifies the object (WorldObjectAdapterRegistry's SafeRecognises gate). Every
            // other settlement still falls through to the vanilla adapter, unchanged.
            CompViralSpread ignored;
            if (TryGetWdManagedSettlement(obj, out ignored)) { kind = WorldObjectKind.Settlement; return true; }

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

        // WD's four grades, T1…T4, live as the SettlementTier enum. Counting its members rather than
        // hardcoding 4 keeps the mapping proportional if WD ever adds a grade: maxLevel widens and
        // core's FromLevel still spreads the tiers across R&S sizes by fraction. Cached — Enum
        // reflection allocates.
        private static readonly int TierCount = Enum.GetValues(typeof(SettlementTier)).Length;

        /// <summary>
        /// Reports a WD faction settlement's grade as a 1-based level over WD's own maximum, so core's
        /// <c>SettlementSizeEvaluator.FromLevel</c> maps it onto an R&amp;S tier: T1→Village, T2→Town,
        /// T3→City, T4 Citadel→MajorCity. Metropolis is out of reach by design — core caps any
        /// settlement at MajorCity and reserves Metropolis for its own faction-capital economy — so
        /// WD's four grades sit 1:1 on R&amp;S tiers 1–4 with nothing squeezed. Level only ever raises a
        /// settlement's size (core takes the max of headcount and level), never shrinks it.
        /// </summary>
        public override bool TryGetLevel(WorldObject obj, out int level, out int maxLevel)
        {
            level = 0;
            maxLevel = 0;

            CompViralSpread comp;
            if (!TryGetWdManagedSettlement(obj, out comp)) return false;

            level = (int)comp.tier + 1; // SettlementTier.T1 == 0
            maxLevel = TierCount;
            return true;
        }

        /// <summary>
        /// A WD-managed NPC settlement: a vanilla <c>Settlement</c> carrying the <see cref="CompViralSpread"/>
        /// comp with a live, WD-assigned grade. WD patches that comp onto the vanilla <c>Settlement</c>
        /// def, so <em>every</em> settlement has it — comp presence alone is not enough. We mirror WD's
        /// own tier-management predicate (<c>WorldActions_Utils.ApplyRandomTier</c> / the tier-label
        /// component): a real, non-excluded NPC faction (player, Traders' Guild and hidden factions are
        /// excluded), a WD-surface tile, not an outpost, and not a specially-flagged "Excluded" object.
        /// Matching WD exactly means we own and grade precisely the settlements WD grades, and leave
        /// the rest to the vanilla adapter.
        /// </summary>
        private static bool TryGetWdManagedSettlement(WorldObject obj, out CompViralSpread comp)
        {
            comp = null;
            if (!(obj is Settlement)) return false;

            Faction faction = obj.Faction;
            if (faction == null || faction.defeated) return false;
            if (WorldActions_Utils.IsExcludedFaction(faction)) return false;
            if (!WorldActions_Utils.IsWdSurfaceWorldObject(obj)) return false;

            comp = obj.GetComponent<CompViralSpread>();
            if (comp == null || comp.IsOutpost || comp.subType == "Excluded")
            {
                comp = null;
                return false;
            }

            return true;
        }
    }
}

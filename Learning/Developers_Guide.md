# Developer's Guide

**Audience:** mod developers. For in-game behaviour, see the [Player's Guide](Players_Guide).

This patch is a single typed adapter that plugs World Domination 2.0 (packageId `TSA.WorldDominationExperimental`, assembly `TSA_WorldDomination`) into the Regions and Societies world-object adapter registry. It hard-references the World Domination assembly, so a renamed type or member over there is a **compile error here**, not a silent misclassification in-game.

## Architecture

```
RegionsAndSocieties.Core          — owns WorldObjectAdapterRegistry (priority-ordered)
TSA.WorldDominationExperimental   — owns the world-object types being classified
RegionsAndSocieties.WorldDominationCP (this mod)
  └── WorldDominationAdapter : WorldObjectAdapterBase — registered at mod construction
```

Core carries no World Domination knowledge of its own. The whole integration is one registration call in the mod's constructor:

```csharp
public WorldDominationCPMod(ModContentPack content) : base(content)
{
    WorldObjectAdapterRegistry.Register(new WorldDominationAdapter());
}
```

The mod must `loadAfter` Regions and Societies core (whose constructor initialises the registry) and World Domination 2.0 (whose assembly this one binds). Both orderings are declared in `About.xml`.

## `WorldDominationAdapter` reference

`class WorldDominationAdapter : RegionsAndSocieties.Integration.WorldObjectAdapterBase`

| Member | Value / signature | Notes |
|---|---|---|
| `AdapterId` | `"worlddomination"` | Stable registry key. |
| `DisplayName` | `"World Domination 2.0"` | Shown in diagnostics. |
| `Priority` | `130` | Same slot as core's retired reflection profile: after VOE (110) and VFE (120/121). |
| `IsActive` | `ModsConfig.IsActive("TSA.WorldDominationExperimental")`, cached at class load | Guards the residual case of a stale load order where the DLL exists but the mod is disabled. |
| `TryClassify` | `bool TryClassify(WorldObject obj, out WorldObjectKind kind)` | See the classification table below. |
| `TryGetPopulation` | `bool TryGetPopulation(WorldObject obj, out int population)` | See below. |
| `TryGetLevel` | `bool TryGetLevel(WorldObject obj, out int level, out int maxLevel)` | Reports a WD faction settlement's grade — see "Settlement grade → level" below. |

### Classification table

`TryClassify` checks are ordered, and the order is load-bearing: `WorldObject_Traveler_Outpost_*` subclasses contain both words, and travelling *to* an outpost is not *being* one.

| Check (in order) | Result `WorldObjectKind` | Rationale |
|---|---|---|
| `obj is WorldObject_Traveler` | `Caravan` | Raids, drop pods, road builders, purchase parties — they move; they are caravans, not holdings. The typed `is` check covers every subclass. |
| `obj is WorldObject_AT_Turret` | `Military` | An armed emplacement is a military installation, and military installations hold ground under the 0.7 governance rules. |
| `obj is WorldObject_WdSettlementRuin` | `Site` | A ruin is scenery, not a territorial holding — destroyed settlements stop claiming their region. |
| `obj is WorldObject_WD_Outpost` | `Outpost` | Holds ground with its real pawn count as population. |
| WD-managed `Settlement` (see below) | `Settlement` | A vanilla `Settlement` WD grades. We take ownership only so core will read our `TryGetLevel` for it; the kind is identical to what the vanilla adapter would return. |
| `obj.GetType().Namespace == "TSA_WorldDomination"` | `Outpost` | Forward-compatibility fallback for types the mod introduces later; an outpost is the safer default because it carries less territorial weight than a settlement. |
| anything else | returns `false` | Not this adapter's object. |

World Domination's **faction bases are vanilla `Settlement`s** (the `TSA_*_T*` names are `KCSG.SettlementLayoutDef`s used at map generation, not the world object's def). Until 0.2.0 this adapter left them entirely to the vanilla adapter. It now **takes ownership of the WD-managed ones** — and only those — so core will consult this adapter's `TryGetLevel` for them: core reads a level only from the adapter that also *classifies* the object (`WorldObjectAdapterRegistry`'s `SafeRecognises` gate). The classification result is the same `Settlement` kind the vanilla adapter gives, so nothing else changes; every other settlement still falls through to vanilla.

### Population

```csharp
bool TryGetPopulation(WorldObject obj, out int population)
```

Implemented only for `WorldObject_WD_Outpost`: returns the type's own `PawnCount` member, read typed. Note that `WorldObject_WD_Outpost` does **not** derive from `Outposts.Outpost` (Vanilla Outposts Expanded) — the shared member name is a coincidence, verified by compile.

### Settlement grade → level

World Domination grades its faction settlements T1–T4. That grade is **not** in the settlement's def — it is a live field on a `CompViralSpread` world-object comp that WD patches onto the vanilla `Settlement` def (`Patches/Settlement_Patch.xml`), in `comp.tier` (`enum SettlementTier { T1, T2, T3, T4 }`). Upgrades — faction investment, promotion — mutate that field in place, so reading it always returns the *current* grade. (The `TSA_Generic_T1_Farming` … `T4_Citadel` names are `KCSG.SettlementLayoutDef`s consumed at map generation, a separate concern.)

**Which settlements.** WD adds the comp to the vanilla `Settlement` def, so *every* settlement carries it — presence alone proves nothing. `TryGetLevel` reports a grade only for a **WD-managed** settlement, mirroring WD's own tier predicate (`WorldActions_Utils.ApplyRandomTier` and its tier-label component): a real, non-excluded NPC faction (`WorldActions_Utils.IsExcludedFaction` rules out the player, the Traders' Guild and hidden factions), a WD-surface tile (`IsWdSurfaceWorldObject`), not an outpost (`comp.IsOutpost`), and not a `subType == "Excluded"` object. Every gate is a typed call into the WD assembly.

**The mapping.** `level = (int)comp.tier + 1` over `maxLevel = number of SettlementTier members` (counted, not hardcoded, so a future T5 widens the range automatically). Core's `SettlementSizeEvaluator.FromLevel(level, maxLevel)` then spreads the grades proportionally:

| WD grade | R&S tier |
|---|---|
| T1 (Farming/Logging/Mining) | Village |
| T2 (Production/Slavery) | Town |
| T3 (Fortress) | City |
| T4 (Citadel) | MajorCity |

R&S has a fifth tier, **Metropolis**, but it is deliberately out of reach here: core caps *any* settlement at MajorCity (`SettlementSizeEvaluator.MaxTierFor`) and reserves Metropolis for its own faction-capital economy, never an individual settlement's grade. So WD's four grades sit 1:1 on R&S tiers 1–4 with nothing squeezed. And because core takes the **max** of headcount-tier and level-tier, reporting a grade can only ever *raise* a settlement's size, never shrink it.

## Debug validation

Dev mode → debug actions → **Regions and Societies** → `R&S WD-CP: world-object dump`. Dumps every World Domination world object — plus every vanilla `Settlement` this adapter grades — with its resolved kind, population, **level** (`level/maxLevel`, or `-`) and owning faction to the log, so classification and grade parity are verifiable headlessly (e.g. via `run_debug_action`). Available while playing on a map or on the world.

## Extending or replacing this adapter

Register your own `WorldObjectAdapterBase` with the core registry. The registry is priority-ordered: an adapter registered with a priority below 130 is consulted before this one for the same objects. Classification stops at the first adapter whose `TryClassify` returns `true`.

## Source

- [`Source/WorldDominationAdapter.cs`](https://github.com/Regions-and-societies/World-Domination-CP/blob/main/Source/WorldDominationAdapter.cs) — the adapter and its full design notes
- [`Source/WorldDominationCPMod.cs`](https://github.com/Regions-and-societies/World-Domination-CP/blob/main/Source/WorldDominationCPMod.cs) — registration
- [`Source/DebugActions_WorldDominationCP.cs`](https://github.com/Regions-and-societies/World-Domination-CP/blob/main/Source/DebugActions_WorldDominationCP.cs) — debug dump

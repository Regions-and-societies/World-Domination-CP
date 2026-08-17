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
| `TryGetLevel` | *not implemented* | Deliberate — see "Why there is no level" below. |

### Classification table

`TryClassify` checks are ordered, and the order is load-bearing: `WorldObject_Traveler_Outpost_*` subclasses contain both words, and travelling *to* an outpost is not *being* one.

| Check (in order) | Result `WorldObjectKind` | Rationale |
|---|---|---|
| `obj is WorldObject_Traveler` | `Caravan` | Raids, drop pods, road builders, purchase parties — they move; they are caravans, not holdings. The typed `is` check covers every subclass. |
| `obj is WorldObject_AT_Turret` | `Military` | An armed emplacement is a military installation, and military installations hold ground under the 0.7 governance rules. |
| `obj is WorldObject_WdSettlementRuin` | `Site` | A ruin is scenery, not a territorial holding — destroyed settlements stop claiming their region. |
| `obj is WorldObject_WD_Outpost` | `Outpost` | Holds ground with its real pawn count as population. |
| `obj.GetType().Namespace == "TSA_WorldDomination"` | `Outpost` | Forward-compatibility fallback for types the mod introduces later; an outpost is the safer default because it carries less territorial weight than a settlement. |
| anything else | returns `false` | Not this adapter's object. |

World Domination's **faction bases are vanilla `Settlement`s with modded defs** — they are already classified by the vanilla adapter; this adapter covers only the types the mod introduces.

### Population

```csharp
bool TryGetPopulation(WorldObject obj, out int population)
```

Implemented only for `WorldObject_WD_Outpost`: returns the type's own `PawnCount` member, read typed. Note that `WorldObject_WD_Outpost` does **not** derive from `Outposts.Outpost` (Vanilla Outposts Expanded) — the shared member name is a coincidence, verified by compile.

### Why there is no level

Settlement grade in World Domination is encoded in **def names** (`TSA_Generic_T1_Farming` … `T4_Citadel`) on vanilla `Settlement` objects, and upgrades are separate purchases with per-line tiers. No scalar on the world object describes grade, so `TryGetLevel` stays unimplemented. This is a finding, not a gap.

## Debug validation

Dev mode → debug actions → **Regions and Societies** → `R&S WD-CP: world-object dump`. Dumps every World Domination world object with its resolved kind, population and owning faction to the log, so classification parity is verifiable headlessly (e.g. via `run_debug_action`). Available while playing on a map or on the world.

## Extending or replacing this adapter

Register your own `WorldObjectAdapterBase` with the core registry. The registry is priority-ordered: an adapter registered with a priority below 130 is consulted before this one for the same objects. Classification stops at the first adapter whose `TryClassify` returns `true`.

## Source

- [`Source/WorldDominationAdapter.cs`](https://github.com/Regions-and-societies/World-Domination-CP/blob/main/Source/WorldDominationAdapter.cs) — the adapter and its full design notes
- [`Source/WorldDominationCPMod.cs`](https://github.com/Regions-and-societies/World-Domination-CP/blob/main/Source/WorldDominationCPMod.cs) — registration
- [`Source/DebugActions_WorldDominationCP.cs`](https://github.com/Regions-and-societies/World-Domination-CP/blob/main/Source/DebugActions_WorldDominationCP.cs) — debug dump

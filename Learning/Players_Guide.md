# Player's Guide

This patch makes [World Domination 2.0](https://steamcommunity.com/sharedfiles/filedetails/?id=3680501610) and [Regions and Societies](https://steamcommunity.com/sharedfiles/filedetails/?id=3784666060) understand each other. With both mods loaded, World Domination's bases, turrets and travelling groups are classified and owned under Regions and Societies territory rules instead of being lumped together or ignored.

There is nothing to configure: install it and it works. All governance toggles live in Regions and Societies itself. It works with **either edition** of Regions and Societies — the standard (Map Mode Framework) edition or the Realistic Planets 2 edition.

## World Domination stays authoritative

This patch sorts World Domination's objects into the right territorial categories, and stops there. It never resizes them: World Domination's own settlement grades, upgrades, outpost systems and strength mechanics run exactly as that mod intends. (A mapping from WD settlement grades onto Regions and Societies territory tiers is built and ships dormant in this release; it goes live in a future release after in-game validation.)

## Travelers count as caravans

World Domination sends a lot of moving groups across the world map — raids, drop pods, road-building crews, purchase parties. This patch classifies all of them as caravans. A group passing through a region is treated as travellers, not as a new territorial claim, so borders stop flickering when World Domination traffic crosses them.

## Outposts hold ground

A World Domination outpost claims territory like any other holding. Its size and strength stay whatever World Domination says they are — this patch does not weigh its garrison into Regions and Societies sizing.

## Turrets hold ground as military installations

World Domination's automated turrets are classified as military installations. Under Regions and Societies governance rules, a military installation holds the ground it stands on — an armed emplacement projects control even with nobody living in it.

## Ruins release territory

When a World Domination settlement is destroyed, its ruin becomes scenery. It stops claiming its region the moment it falls, instead of holding the territory forever as a ghost claim. Conquest actually redraws the map.

## Requirements and load order

- [Regions and Societies](https://steamcommunity.com/sharedfiles/filedetails/?id=3784666060) — either the standard edition or the [Realistic Planets 2 edition](https://steamcommunity.com/sharedfiles/filedetails/?id=3784666526) (required)
- [World Domination 2.0](https://steamcommunity.com/sharedfiles/filedetails/?id=3680501610) (required)

Load this patch after them; the mod's metadata orders it after both core editions and World Domination automatically. Because the two Regions and Societies editions are mutually exclusive, the mod manager does not hard-require one of them — if you load this patch with no edition at all, the log tells you plainly:

```
[RegionsAndSocieties.WorldDominationCP] Regions and Societies is not loaded — check your mod list...
```

## Verifying it is working

On load, the log shows:

```
[RegionsAndSocieties.WorldDominationCP] Registered the World Domination 2.0 adapter (priority 130).
```

With dev mode on, the debug action **R&S WD-CP: world-object dump** (under "Regions and Societies") lists every World Domination object on the world map with the kind and faction this patch resolved for it. While sizing is dormant the dump says so in its header, and the population/level columns print `-` by design.

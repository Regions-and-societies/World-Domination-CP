# Player's Guide

This patch makes [World Domination 2.0](https://steamcommunity.com/sharedfiles/filedetails/?id=3680501610) and [Regions and Societies](https://steamcommunity.com/sharedfiles/filedetails/?id=3784666060) understand each other. With both mods loaded, World Domination's bases, turrets and travelling groups are counted, populated and owned under Regions and Societies territory rules instead of being lumped together or ignored.

There is nothing to configure: install it and it works. All governance toggles live in Regions and Societies itself.

## Travelers count as caravans

World Domination sends a lot of moving groups across the world map — raids, drop pods, road-building crews, purchase parties. This patch classifies all of them as caravans. A group passing through a region is treated as travellers, not as a new territorial claim, so borders stop flickering when World Domination traffic crosses them.

## Outposts hold ground with their real population

A World Domination outpost claims territory like any other holding, and its population is read from the actual number of pawns stationed there — not a guess or a flat default. A big garrison weighs more than a skeleton crew.

## Turrets hold ground as military installations

World Domination's automated turrets are classified as military installations. Under Regions and Societies governance rules, a military installation holds the ground it stands on — an armed emplacement projects control even with nobody living in it.

## Ruins release territory

When a World Domination settlement is destroyed, its ruin becomes scenery. It stops claiming its region the moment it falls, instead of holding the territory forever as a ghost claim. Conquest actually redraws the map.

## Requirements and load order

- [Regions and Societies](https://steamcommunity.com/sharedfiles/filedetails/?id=3784666060) (required)
- [World Domination 2.0](https://steamcommunity.com/sharedfiles/filedetails/?id=3680501610) (required)

Load this patch after both. The game's mod manager enforces this automatically from the mod's metadata.

## Verifying it is working

On load, the log shows:

```
[RegionsAndSocieties.WorldDominationCP] Registered the World Domination 2.0 adapter (priority 130).
```

With dev mode on, the debug action **R&S WD-CP: world-object dump** (under "Regions and Societies") lists every World Domination object on the world map with the kind, population and faction this patch resolved for it.

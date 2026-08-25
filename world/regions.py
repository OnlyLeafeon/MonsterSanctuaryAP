from BaseClasses import Region


def create_region(name: str, player: int, multiworld) -> Region:
    region = Region(name, player, multiworld)
    multiworld.regions.append(region)
    return region


def create_regions(world) -> dict:
    player = world.player
    multiworld = world.multiworld

    region_names = [
        "Mountain Path",
        "Keepers' Stronghold",
        "Blue Cave",
        "Stronghold Dungeon",
        "Ancient Woods",
        "Snowy Peaks",
        "Magma Chamber",
        "Sun Palace",
        "Horizon Beach",
        "Mystical Workshop",
        "Underworld",
        "Abandoned Tower",
        "Blob Burg",
        "Forgotten World",
        "Eternity's End",
        "End",
    ]

    regions = {"Menu": create_region("Menu", player, multiworld)}
    for name in region_names:
        regions[name] = create_region(name, player, multiworld)

    for name in region_names:
        regions["Menu"].connect(regions[name])

    return regions

from __future__ import annotations

import ast
from typing import Callable

from worlds.generic.Rules import add_rule, set_rule

from . import ability_data
from .monster_rando import randomize_monsters


class RuleFactory:
    item_table = {
        "DJ": "Double Jump Boots",
        "WU": "Warm Underwear",
        "MPK1": ("Mountain Path Key", 1),
        "BCK1": ("Blue Caves Key", 1), "BCK3": ("Blue Caves Key", 3),
        "MCK1": ("Magma Chamber Key", 1), "MCK2": ("Magma Chamber Key", 2),
        "AWK2": ("Ancient Woods Key", 2), "AWK3": ("Ancient Woods Key", 3),
        "WSK3": ("Mystical Workshop Key", 3),
        "SDK1": ("Stronghold Dungeon Key", 1), "SDK2": ("Stronghold Dungeon Key", 2),
        "UWK1": ("Underworld Key", 1),
        "KOP1": ("Key of Power", 1),
    }

    obstacle_table = {
        "BW": "Breakable Walls", "BWA": "Breakable Walls Advanced",
        "IV": "Impassable Vines", "IVA": "Impassable Vines Advanced",
        "DB": "Diamond Blocks", "DBA": "Diamond Blocks Advanced",
        "MW": "Magic Walls",
        "FO": "Fire Orbs", "FOA": "Fire Orbs Advanced",
        "WO": "Water Orbs", "WOA": "Water Orbs Advanced",
        "LO": "Lightning Orbs", "LOA": "Lightning Orbs Advanced",
        "EO": "Earth Orbs", "EOA": "Earth Orbs Advanced",
        "IO": "Ice Orbs", "IOA": "Ice Orbs Advanced",
        "DL": "Distant Ledges", "DLA": "Distant Ledges Advanced",
        "S": "Swimming", "SA": "Swimming Advanced",
        "M": "Mounts", "MA": "Mounts Advanced",
        "Ta": "Tar", "TaA": "Tar Advanced",
        "GS": "Ground Switches", "GSA": "Ground Switches Advanced",
        "HB": "Heavy Blocks",
        "To": "Torches", "ToA": "Torches Advanced",
        "DR": "Dark Rooms", "DRA": "Dark Rooms Advanced",
        "GA": "Grappling Anchors",
        "NC": "Narrow Corridors",
        "BB": "Big Boulders",
        "IP": "Invisible Platforms",
        "MV": "Magical Vines",
        "I": "Intangiblity",
    }

    _seed: str = ""
    _mapping_cache: dict[str, dict] = {}
    _abilities_cache: dict[tuple[str, str], set] = {}

    @classmethod
    def set_seed(cls, seed: str) -> None:
        cls._seed = seed

    @classmethod
    def _monster_mapping(cls) -> dict:
        mapping = cls._mapping_cache.get(cls._seed)
        if mapping is None:
            mapping = randomize_monsters(cls._seed, ability_data.monsterlist.keys())
            cls._mapping_cache[cls._seed] = mapping
        return mapping

    @classmethod
    def _abilities_in_zone(cls, zone: str) -> set:
        key = (cls._seed, zone)
        abilities = cls._abilities_cache.get(key)
        if abilities is None:
            abilities = ability_data.abilities_available_in_region(zone, cls._monster_mapping())
            cls._abilities_cache[key] = abilities
        return abilities

    @staticmethod
    def _zone_of(region_name: str) -> str:
        return region_name.split("_", 1)[0]

    @classmethod
    def build_rule(cls, logic: "str | None", player: int, zone: "str | None" = None) -> Callable:
        if not logic:
            return lambda state: True

        expr = (
            logic.replace("&", " and ")
            .replace("|", " or ")
            .replace("!", " not ")
        )

        tree = ast.parse(expr, mode="eval")

        def eval_node(node, state):
            if isinstance(node, ast.BoolOp):
                values = [eval_node(v, state) for v in node.values]
                if isinstance(node.op, ast.And):
                    return all(values)
                if isinstance(node.op, ast.Or):
                    return any(values)
            elif isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.Not):
                return not eval_node(node.operand, state)
            elif isinstance(node, ast.Name):
                token = node.id

                if token in cls.item_table:
                    entry = cls.item_table[token]
                    item_name, count = entry if isinstance(entry, tuple) else (entry, 1)
                    return state.has(item_name, player, count)

                if token in cls.obstacle_table:
                    if zone is None:
                        raise ValueError(
                            f"Logic Broken Here Please report"
                        )
                    obstacle = cls.obstacle_table[token]
                    return bool(
                        set(ability_data.abilities_for_obstacle(obstacle))
                        & cls._abilities_in_zone(zone)
                    )

                raise KeyError(f"Unknown logic token: {token}")
            raise TypeError(f"Unsupported logic node: {ast.dump(node)}")

        return lambda state: eval_node(tree.body, state)

    @classmethod
    def connect(cls, regions: dict, a: str, b: str, player: int, logic: "str | None" = None):
        entrance = regions[a].connect(regions[b])
        if logic:
            set_rule(entrance, cls.build_rule(logic, player, zone=cls._zone_of(a)))
        return entrance

    @classmethod
    def connect_two_way(
        cls,
        regions: dict,
        a: str,
        b: str,
        player: int,
        forward_logic: "str | None" = None,
        backward_logic: "str | None" = None,
    ):
        cls.connect(regions, a, b, player, forward_logic)
        cls.connect(regions, b, a, player, backward_logic)

    @classmethod
    def set_location_rule(cls, location, player: int, logic: str):
        zone = cls._zone_of(location.parent_region.name)
        set_rule(location, cls.build_rule(logic, player, zone=zone))


rf = RuleFactory

location_logic: dict[str, str] = {
    # Mountain Path
    '(MP) Tutorial Chest on Upper Ledge': 'DJ | DL',
    '(MP) Tutorial Chest Behind Breakable Wall': 'BW',
    '(MP) Chest Above Monolith': 'DJ | DL',
    '(MP) Warp Room Behind Vines (1)': 'IV',
    '(MP) Warp Room Behind Vines (2)': 'IV',
    '(MP) East of Crystal Room - Breakable Wall Chest': 'BW',
    '(MP) Catzerker Ledge Chest': 'IV',
    '(MP) Music Wall Chest': 'MW',
    '(MP) East of Buran - Ledge Chest': 'DJ & DL',
    '(MP) Fire Orb Chest': 'FO',
    '(MP) Water Orb Chest': 'WO',
    '(MP) Narrow Corridor Room': 'DJ & NC',
    # Eternity's End
    '(EE) Eternity\'s End - Chest 2': 'DJ & IP',
    '(EE) Eternity\'s End - Chest 1': 'DJ & IP',
    # Blue Cave
    '(BC) Platforms - Breakable Wall Chest': 'BW',
    '(BC) Platforms - Ledge Chest': 'DJ | DL',
    '(BC) North Fork - East Ledge Chest': 'DJ | DL',
    '(BC) North Fork - Ground Switch Chest': 'GS',
    '(BC) Double Jump Boots Chest': 'DJ | DL',
    '(BC) South Big Rock Room': 'GSA',
    '(BC) South Jump Puzzle Room 1': 'DJ | TaA',
    '(BC) South Jump Puzzle Room 2': 'DJ | TaA',
    '(BC) Crystal Room - Middle Ledge Chest': 'IV & (DJ | DL)',
    '(BC) Crystal Room - Upper Ledge Chest 2': 'DJ | TaA',
    '(BC) Crystal Room - Upper Ledge Chest 1': 'DJ | TaA',
    '(BC) West Waters - Breakable Walls Chest': 'BW & (DJ | S)',
    '(BC) West Waters - Ledge Chest': 'DJ | S',
    '(BC) Sun Palace Entrance - Ledge Chest': 'S',
    '(BC) Sun Palace Entrance - Switch Puzzle Chest': 'GS & DJ & HB',
    '(BC) Specter Hallway - Earth Orb Chest': 'EO',
    '(BC) Specter Hallway - Lightning Orb Chest': 'LO',
    # Stronghold Dungeon
    '(SD) Jail - Switches Chest': 'GS',
    '(SD) North Vertical - Lightning Orb Chest': 'LO & (DJ | TaA)',
    '(SD) West Corridor - Heavy Block': 'HB & (DJ | DL)',
    '(SD) West Corridor - Breakable Wall': 'BW',
    '(SD) Gates Puzzle Room - Upper Ledge': 'DJ | TaA',
    '(SD) Gates Puzzle Room - Middle Ledge': 'DJ & DL',
    '(SD) Big Rock Room - Behind Gate': 'GSA',
    '(SD) West Sewer': 'DJ',
    '(SD) West Hidden - Breakable Wall': 'BW',
    '(SD) East Sewer - Central Ledge': 'DJ & DLA',
    '(SD) Slime Statue Room 2': 'DJ | TaA',
    '(SD) Slime Statue Room 1': 'DJ | TaA',
    '(SD) Ice Orb Chest': 'IO',
    '(SD) East Dark Corridor': 'DR & DJ',
    '(SD) East Lever Puzzle': 'DJ | TaA',
    '(SD) Lower Ancient Woods Entrance': 'LO & (DJ | DL) | DJ & DLA',
    '(SD) Ancient Woods Entrance': 'DJ | DL',
    '(SD) After Library Key Door': 'HB & DJ',
    '(SD) Workshop Entrance': 'DJ & DLA',
    # Ancient Woods
    '(AW) West Deep Caves - Spike Jump Chest': 'DJ | DL',
    '(AW) Dark Caves - Torches Chest': 'DJ & DR | To & (TaA | DJ)',
    '(AW) West Deep Caves - Vines Chest': 'IV',
    '(AW) West Deep Caves Spike Room': 'DJ | TaA',
    '(AW) East - Scaffold Chest 1': 'TaA',
    '(AW) West Caves - Breakable Wall': 'BW',
    '(AW) West Jump Puzzle 1': 'DJ | DL',
    '(AW) West Jump Puzzle 2': 'DJ | TaA',
    '(AW) East - Goblin King Shortcut Chest': 'DJ | TaA',
    '(AW) East - Lightning Orb Chest': 'LO',
    '(AW) South Dark Room': 'DR & DJ',
    '(AW) Torches Room 2': 'To',
    '(AW) Torches Room 1': 'To',
    '(AW) South Glowfly Room 1': 'DR',
    '(AW) South Glowfly Room 2': 'DR',
    '(AW) West Deep Caves - Dark Room': 'DR & DJ | To & (DJ | TaA)',
    '(AW) West Deep Caves - Spike Fall Chest 2': 'DJ | TaA',
    '(AW) West Deep Caves - Spike Fall Chest 1': 'DJ | TaA',
    '(AW) Upper East - Grapple Chest': 'GA',
    '(AW) Dark Caves - Vertical Shaft': 'DR & DJ',
    '(AW) West Deep Caves - Spore Shroud Chest': 'MV & DJ',
    # Snowy Peaks
    '(SNP) East - Breakable Wall Chest': 'BW',
    '(SNP) East - Floating Platform Chest': 'DJ & DL',
    '(SNP) East Cave - Upper Lever Chest': 'DJ | TaA',
    '(SNP) East Cave - Lower Lever Chest': 'DJ | TaA',
    '(SNP) East Cave - Middle Chest': 'DJ | TaA',
    '(SNP) East Hills - Breakable Walls Chest': 'BW & (DJ | TaA)',
    '(SNP) West Cave - Ice Orb Chest': 'IO',
    '(SNP) West Cave - Middle Chest': 'BW & (DJ | DL)',
    '(SNP) West Cave - Upper Chest': 'DJ | TaA',
    '(SNP) Dracozul Climb - Upper Chest': 'DJ',
    '(SNP) West Cave - Earth Orb Chest': 'DJ & EO',
    '(SNP) West Mountain - Ice Orb Chest': 'IOA',
    '(SNP) West Mountain Peak - Ledge Chest': 'DJ & DL',
    '(SNP) East Mountain Peak - Cave Chest': 'DJ | DL',
    '(SNP) East Mountain Peak - Floating Ledge Chest': 'DJ & DL',
    '(SNP) East Mountain - Lightning Orb Chest': 'DJ & LO',
    '(SNP) East Mountain - East Lower Chest': 'DJ | DL',
    '(SNP) East Mountain - Upper Crumble Ledge Chest 1': 'DJ & DL',
    '(SNP) East Mountain - Underwater Chest': 'WU',
    '(SNP) East Mountain - Crumble Platform Challenge Chest': 'DJ',
    '(SNP) East Mountain - Water Orb Chest': 'WO & (DJ | TaA)',
    '(SNP) East Mountain - Upper Cave Chest': 'DJ | TaA',
    '(SNP) East Mountain - West Crumble Ledge Chest': 'DJ | TaA',
    '(SNP) East Mountain - Spikes Chest': 'TaA',
    '(SNP) East Mountain Peak - Peak Chest': 'DJ | TaA',
    '(SNP) West Hills': 'TaA | DJ & DL',
    '(SNP) Lake - Underwater Chest': 'WU & (S | DJ & HB)',
    '(SNP) Akhlut Room Chest': 'WU & (DJ | S)',
    '(SNP) West Dark Room': 'DR & DJ',
    # Sun Palace
    '(SUN) West - Upper Interior Chest': 'DJ | DL',
    '(SUN) West - Floating Ledge Chest': 'DJ & DL',
    '(SUN) West - Mount Door Chest': 'M',
    '(SUN) South - Ice Orb Chest': 'IOA',
    '(SUN) Central Sewer - Middle East Chest': 'DJ | S & DLA',
    '(SUN) Central Sewer - Central Chest': 'S | DJ',
    '(SUN) East Sewers - Timed Jump Chest': 'DJ',
    '(SUN) East - Door Chest': 'DJ & DL & GS',
    '(SUN) East - Lever Chest': 'TaA | DJ & DL',
    '(SUN) East - Water Orb Chest': 'WO & DJ',
    '(SUN) West Sewers - Heavy Block Chest': 'HB',
    '(SUN) West Sewers - Maze Chest 1': 'S',
    '(SUN) West Sewers - Maze Chest 2': 'S',
    '(SUN) West Sewers West Hidden Area 1': 'DJ',
    '(SUN) West Sewers West Hidden Area 2': 'DJ',
    '(SUN) West Pool - Water Chest': 'S',
    '(SUN) Slime Statue Room - Center Chest': 'DJ | TaA',
    '(SUN) Slime Statue Room - Switch Chest': 'HB & (DJ | TaA)',
    # Magma Chamber
    '(MC) West - Dark Breakable Wall Chest': 'DR & BW & (DJ | TaA)',
    '(MC) North - Dark Room': 'DR & DJ',
    '(MC) East - Behind Diamond Block': 'DB',
    '(MC) Gryphonix Room 1': 'BB',
    '(MC) South Vertical Shaft - Grapple Chest': 'GA & (DJ | DL)',
    '(MC) Tar Pit': 'TaA',
    '(MC) Asura Room': 'DJ | TaA',
    '(MC) Center - Diamond Block Chest': 'DB',
    '(MC) East - Vertical Shaft Chest': 'DJ | TaA',
    '(MC) Center - Below Lava Pit': 'BW',
    '(MC) Center - Invisible Platform Chest 1': 'IP & DJ',
    '(MC) Center - Invisible Platform Chest 2': 'IP & DJ',
    '(MC) Center - Lava Puddle Chest': 'DJ & DLA',
    '(MC) Crumble Bridge Puzzle - Upper Chest': 'GS & DJ',
    # Forgotten World
    '(FW) Magma Chamber Entrance': 'BW & (DJ | DL)',
    '(FW) North Caves - Diamond Block Stack': 'DB',
    '(FW) West Caves - Levitate Chest': 'BB & (DJ | TaA)',
    '(FW) South Caves Tunnel - Magic Vines Chest': 'MV',
    '(FW) Magic Vine Maze - Lower Chest': 'MV & BW & (DJ | TaA)',
    '(FW) Magic Vine Maze - Middle Chest': 'MV & (DJ | TaA)',
    '(FW) Magic Vine Maze - Upper Chest': 'MV & (DJ | TaA)',
    '(FW) West Jungle - Upper Chest': 'DJ & DL',
    '(FW) North East Tar Pits': 'TaA',
    '(FW) East Tar Pits Vertical - Upper Chest': 'TaA',
    '(FW) North West Tar Pits': 'TaA',
    '(FW) East Caves Large Room - NE Chest': 'BW & (DJ | TaA)',
    '(FW) East Caves Large Room - West Chest': 'MV',
    '(FW) South Caves Dark Room': 'DRA',
    '(FW) South Waters - Currents Chest': 'SA',
    '(FW) South Waters - Cave Chest': 'S',
    '(FW) Dracomer Lair': 'S',
    '(FW) South Waters - Magic Walls Chest': 'MW & (DJ | TaA)',
    '(FW) South Waters - Dual Mobility Chest': 'MW & TaA & DJ',
    '(FW) South West Tar Pits': 'TaA',
    '(FW) Descent Hidden Room 3': 'DR',
    '(FW) Descent Hidden Room 2': 'DR',
    '(FW) Descent Hidden Room 1': 'DR',
    '(FW) Terradrile Lair - West Chest': 'BW',
    '(FW) Terradrile Lair Vertical': 'BW & NC & (DJ | TaA)',
    '(FW) Center Caves - Magic Wall Chest': 'MW',
    '(FW) East Jungle - High Ledge Chest': 'DJ | DL',
    '(FW) East Jungle - Invisible Platform Chest': 'IP & DJ & MV',
    '(FW) East Jungle - Magic Vines Chest': 'MV & BW & DJ & (TaA | IP)',
    '(FW) Vertical Climb - Lower Chest': 'DJ | TaA',
    '(FW) Vertical Climb - Middle Chest': 'DJ | TaA',
    '(FW) Vertical Climb - Upper Chest': 'DJ | TaA',
    '(FW) North East Climb': 'MW',
    '(FW) North Center Climb - SW Chest 2': 'DJ | TaA',
    '(FW) North Center Climb NW Chest': 'DJ | TaA',
    '(FW) North Center Climb - SW Chest 1': 'DJ | TaA',
    '(FW) North West Climb - East Chest': 'DJ | TaA',
    '(FW) Center Jungle - Floating Ledges Chest': 'DJ & TaA',
    # Horizon Beach
    '(HB) West - Underwater Chest': 'DJ | S',
    '(HB) West - Flying Ledges 1': 'DJ & DLA',
    '(HB) West - Flying Ledges 2': 'DJ & DLA',
    '(HB) West - Fast Currents Chest': 'SA',
    '(HB) Treasure Cave - Vertical Room': 'S',
    '(HB) Treasure Cave - Watery Tunnel': 'DJ | S',
    '(HB) Elderjel Room - Special Chest': 'DJ | TaA',
    '(HB) Forgotten World Entrance': 'S',
    '(HB) South - Spike Room 2': 'S',
    '(HB) South - Spike Room 1': 'S',
    '(HB) Labyrinth - Southwest Chest': 'S',
    '(HB) Labyrinth - Southeast Chest': 'SA',
    '(HB) Central - West Shortcut Chest': 'DJ',
    '(HB) Central - East Shortcut Chest': 'DJ',
    '(HB) Central - Fast Currents Chest 1': 'S',
    '(HB) Central - Fast Currents Chest 2': 'SA',
    '(HB) Central - Fast Currents Chest 3': 'SA',
    '(HB) Central - Island Chest': 'S',
    '(HB) East - West Peak 1': 'DJ | DL',
    '(HB) East - East Peak 2': 'DJ & DLA',
    '(HB) East - Middle Ledge Chest 1': 'BW',
    '(HB) East - Beneath Bridge': 'S & DJ',
    '(HB) East - Underwater Chest': 'DJ | S',
    '(HB) Fisherman Chest': 'S | DJ | DL',
    '(HB) East - Fast Currents Tunnel': 'SA',
    '(HB) Central - Ice Orb Chest': 'IO & HB & S',
    '(HB) Central - Timed Door Chest': 'S',
    '(HB) East Hidden': 'NC & DJ',
    '(HB) Vodinoy Room 2': 'NC',
    # Underworld
    '(UW) East - Catacomb Entrance': 'DJ | TaA',
    '(UW) East Catacomb - Peak Chest 1': 'GSA & (DJ | TaA)',
    '(UW) East Catacomb - Peak Chest 2': 'GSA & (DJ | TaA)',
    '(UW) Center Cave': 'BW',
    '(UW) Crystal Room': 'DJ & GA & DLA',
    '(UW) Sun Palace Entrance': 'DJ | DL',
    '(UW) Spinner Cave': 'BW & (DJ | TaA)',
    '(UW) East Catacomb - Basement Chest 2': 'GA & DJ',
    '(UW) East Catacomb - Basement Chest 1': 'DJ & GA & HB',
    '(UW) East - Big Rock Chest': 'GSA',
    '(UW) East - Floating Ledge Chest 1': 'DJ & DLA',
    '(UW) East - Floating Ledge Chest 2': 'DJ & DLA',
    # Mystical Workshop
    '(MW) South - Behind Diamond Block': 'DB',
    '(MW) Pipe Puzzle Room 2': 'GS & HB & DJ',
    '(MW) Pipe Puzzle Room 1': 'GS & HB & DJ',
    '(MW) South Eastern Shaft': 'DJ | DL',
    '(MW) South Cog Room - Middle Chest': 'TaA | DJ & DL',
    '(MW) South Cog Room - Upper Chest': 'DJ | TaA',
    '(MW) Lighting and Ice Orb Chest': 'IO & LO',
    '(MW) South Grapple Room - Grapple Chest': 'GA',
    '(MW) Timed Lever Puzzle 1': 'M & DJ',
    '(MW) Timed Lever Puzzle 2': 'M & DJ',
    '(MW) North Cog Room - West Chest': 'DJ & DLA',
    '(MW) North Cog Room - East Chest': 'DJ | DL | GSA',
    '(MW) North Lever Puzzle': 'DJ | DL',
    '(MW) Eastern Vertical': 'DJ | DL',
    '(MW) Clock Room': 'DJ',
    '(MW) Mimic Room': 'DJ & DLA',
    '(MW) Lower Vat Room 2': 'DJ | TaA',
    '(MW) Lower Vat Room 1': 'DJ | TaA',
    '(MW) Upper Vat Room': 'DJ',
    '(MW) Hidden Clock Room - Levitate Chest 2': 'BB',
    '(MW) Hidden Clock Room - Levitate Chest 1': 'BB',
    # Abandoned Tower
    '(AT) South Water Orb Chest': 'WO',
    '(AT) South Grapple Chest': 'GA',
    '(AT) South Big Rock Chest': 'GSA',
    '(AT) South Fire Orb Chest': 'FO',
    '(AT) South Breakable Walls Chest': 'BW',
    '(AT) South East Climb': 'GS & DJ',
    '(AT) Center Lightning Orb Chest': 'LO',
    '(AT) Center Diamond Chest': 'DB & (DJ | DL)',
    '(AT) Center Switch Chest 2': 'HB & (DJ | DL)',
    '(AT) Center Switch Chest 1': 'HB & (DJ | DL)',
    '(AT) South Square Room - Double Jump Chest': 'DJ & DLA',
    '(AT) South Square Room - Upper Chest': 'DJ | DL',
    '(AT) Center Timed Chest': 'M & DJ',
    '(AT) Center East Side': 'DJ | TaA',
    '(AT) Center Dark Room - Upper Chest': 'DR | To',
    '(AT) Center Dark Room - Lower Chest': 'To',
    '(AT) Center - Underwater Chest': 'SA',
    '(AT) North Ice Orb Chest': 'IO',
    '(AT) North Earth Orb Chest': 'EO',
    '(AT) North East Side': 'DJ',
    '(AT) East Invisible Platform Chest': 'IP & DJ',
    # Blob Burg
    '(BB) North East Vertical': 'DJ | TaA',
    '(BB) Center Vertical': 'DJ | TaA',
    '(BB) Crystal Room': 'DJ | DL',
    '(BB) South Room - Levitate Chest': 'BB',
    '(BB) South Room - Underwater Chest': 'S & DJ',
    '(BB) West Room - Eastern Chest': 'SA',
    '(BB) West Room - Center Chest 2': 'DJ | S',
    '(BB) West Room - Morph Ball Chest': 'NC & (DJ | S)',
    '(BB) Worm Room - Lower Chest': 'DJ | DL | S',
    '(BB) Worm Room - Upper Chest': 'IP & DJ',
}


def set_rules(world) -> None:
    player = world.player
    multiworld = world.multiworld

    for location_name, logic in location_logic.items():
        location = multiworld.get_location(location_name, player)
        rf.set_location_rule(location, player, logic)
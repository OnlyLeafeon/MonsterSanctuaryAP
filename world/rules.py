from __future__ import annotations

import ast
from typing import Callable

from worlds.generic.Rules import add_rule, set_rule


class RuleFactory:

    token_table = {
        "BW": "Breakable Walls",
        "BWA": "Breakable Walls Advanced",
        "IV": "Impassable Vines",
        "IVA": "Impassable Vines Advanced",
        "DB": "Diamond Blocks",
        "DBA": "Diamond Blocks Advanced",
        "MW": "Magic Walls",
        "FO": "Fire Orbs",
        "FOA": "Fire Orbs Advanced",
        "WO": "Water Orbs",
        "WOA": "Water Orbs Advanced",
        "LO": "Lightning Orbs",
        "LOA": "Lightning Orbs Advanced",
        "EO": "Earth Orbs",
        "EOA": "Earth Orbs Advanced",
        "IO": "Ice Orbs",
        "IOA": "Ice Orbs Advanced",
        "DL": "Distant Ledges",
        "DLA": "Distant Ledges Advanced",
        "S": "Swimming",
        "SA": "Swimming Advanced",
        "M": "Mounts",
        "MA": "Mounts Advanced",
        "Ta": "Tar",
        "TaA": "Tar Advanced",
        "GS": "Ground Switches",
        "GSA": "Ground Switches Advanced",
        "HB": "Heavy Blocks",
        "To": "Torches",
        "ToA": "Torches Advanced",
        "DR": "Dark Rooms",
        "DRA": "Dark Rooms Advanced",
        "GA": "Grappling Anchors",
        "NC": "Narrow Corridors",
        "BB": "Big Boulders",
        "IP": "Invisible Platforms",
        "MV": "Magical Vines",
        "I": "Intangiblity",
        "DJ": "Double Jump Boots",
        "WU": "Warm Underwear",
    }

    @classmethod
    def build_rule(cls, logic: "str | None", player: int) -> Callable:
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
                if token not in cls.token_table:
                    raise KeyError(f"Unknown logic token: {token}")
                return state.has(cls.token_table[token], player)
            raise TypeError(f"Unsupported logic node: {ast.dump(node)}")

        return lambda state: eval_node(tree.body, state)

    @classmethod
    def connect(cls, regions: dict, a: str, b: str, player: int, logic: "str | None" = None):
        entrance = regions[a].connect(regions[b])
        if logic:
            set_rule(entrance, cls.build_rule(logic, player))
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
        set_rule(location, cls.build_rule(logic, player))

    @classmethod
    def add_location_rule(cls, location, player: int, logic: str):
        add_rule(location, cls.build_rule(logic, player))


rf = RuleFactory

location_logic: dict[str, str] = {
    '(MP) Tutorial Chest on Upper Ledge': "DL | DJ",
    '(MP) Tutorial Chest Behind Breakable Wall': "BW",
    '(MP) Chest Above Monolith': "DL | DJ",
    '(MP) Warp Room Behind Vines (1)': "IV",
    '(MP) Warp Room Behind Vines (2)': "IV",
}


def set_rules(world) -> None:
    player = world.player
    multiworld = world.multiworld

    for location_name, logic in location_logic.items():
        location = multiworld.get_location(location_name, player)
        rf.set_location_rule(location, player, logic)
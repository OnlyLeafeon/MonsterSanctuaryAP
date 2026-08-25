"""
Monster Santuary APWorld
"""
from typing import Any, Dict

from BaseClasses import ItemClassification, Tutorial
from worlds.AutoWorld import World, WebWorld

from .items import MonsterSantuaryItem, item_table, item_name_to_id, item_name_groups
from .locations import MonsterSantuaryLocation, location_table
from .options import MonsterSantuaryOptions, option_groups
from .regions import create_regions as build_regions
from .rules import set_rules as apply_rules


class MonsterSantuaryWebWorld(WebWorld):
    theme = "grass"
    tutorials = [
        Tutorial(
            "Multiworld Setup Guide",
            "A guide to setting up Monster Santuary for Archipelago multiworld games.",
            "English",
            "setup_en.md",
            "setup/en",
            ["OnlyLeafeon", "MuratDevelopment"],
        )
    ]


class MonsterSantuaryWorld(World):
    """
    Monster Santuary is a Metroidvania-style monster-taming game. Explore
    Monster Sanctuary, capture and train monsters, and take on the game's
    champions and the Mad Lord.
    """

    game = "Monster Santuary"
    web = MonsterSantuaryWebWorld()
    options_dataclass = MonsterSantuaryOptions
    options: MonsterSantuaryOptions
    option_groups = option_groups

    location_name_to_id = {location.name: location.id for location in location_table}
    item_name_to_id = item_name_to_id
    item_name_groups = item_name_groups

    data_version = 1

    def create_item(self, name: str) -> MonsterSantuaryItem:
        item_def = next(item for item in item_table if item.name == name)
        return MonsterSantuaryItem(item_def.name, item_def.classification, item_def.id, self.player)

    def create_items(self) -> None:
        item_pool = []
        for item_def in item_table:
            if item_def.classification == ItemClassification.filler:
                continue
            for _ in range(item_def.count):
                item_pool.append(self.create_item(item_def.name))

        filler_names = [
            item_def.name for item_def in item_table
            if item_def.classification == ItemClassification.filler
        ]
        remaining = len(location_table) - len(item_pool)
        for _ in range(remaining):
            item_pool.append(self.create_item(self.random.choice(filler_names)))

        self.multiworld.itempool += item_pool

    def create_regions(self) -> None:
        regions = build_regions(self)
        for location_def in location_table:
            region = regions[location_def.region]
            region.add_locations({location_def.name: location_def.id}, MonsterSantuaryLocation)

    def set_rules(self) -> None:
        apply_rules(self)

    def fill_slot_data(self) -> Dict[str, Any]:
        return {
            "goal": self.options.goal.value,
            "champreq": self.options.champreq.value,
            "startran": self.options.startran.value,
            "monrantyp": self.options.monrantyp.value,
            "monrangrp": self.options.monrangrp.value,
            "underche": self.options.underche.value,
            "forgeche": self.options.forgeche.value,
            "remlore": self.options.remlore.value,
        }

from dataclasses import dataclass

from Options import Choice, OptionGroup, PerGameCommonOptions, Range, Toggle, OptionSet

class Goal(Choice):
    """
    What will your goal be
    mad lord: defeat the mad lord at the top of the abandoned tower, has key of power in the item pool
    mad lord and: defeat the mad lord after defeating your set amount of champions, has key of power given to you apon reaching requirement
    all champions: defeat all the champions, has key of power in the item pool
    """
    diplay_name = "Goal"
    option_mad_lord = 0
    option_mad_lord_and = 1
    option_all_champions = 2
    default = 0

class GoalChampionReq(Range):
    "How many Champions you require to beat if you Choose mad lord and"
    display_name = "Champions Required for goal"
    range_start = 1
    range_end = 26
    default = 10

class RandomizeStarter(Toggle):
    "Randomize your Spectral Familiar"
    display_name = "Randomize Familiar"

class RandomizeMonstersType(Choice):
    """
    How your monsters will be randomized per encounter
    off: no monsters are randomized
    dynamic: monsters are randomized apon entering the room and reloading changes it
    vanilla rando: done like how it was done in the vanilla game
    random: set per slot in each encounter 
    """
    display_name = "Type of Monster Randomization"
    option_off = 0
    option_dynamic = 1
    option_vanilla_rando = 2
    option_random = 3
    default = 0

class RandomizeMonsterGrouping(Choice):
    """
    How will you randomize your monsters
    Region: randomize monsters from other monsters in the same area
    balanced: later game monsters will stay in the later game
    completely random: do i need to say anything
    Region and spectral: include spectrals in each pool
    balanced and early spectral: adds spectrals to the pool in the early game
    balanced and late spectral: adds spectrals to the pool in the late game
    completely random with spectrals: again need i say anything
    """
    display_name = "Monster Grouping"
    option_region = 0
    option_balanced = 1
    option_completely_random = 2
    option_region_and_spectral = 3
    option_balanced_and_early_spectral = 4
    option_balanced_and_late_spectral = 5
    option_completely_random_with_spectrals = 6
    default = 0

class SkillTreeRando(Toggle):
    "Have your monsters skill trees randomized"
    display_name = "Randomize Skill Trees"

class UnderworldChecks(Toggle):
    "Adds the underworld into the location pool (key of power will still be in the pool)"
    display_name = "Include Underworld"

class ForgottenChecks(Toggle):
    "Adds the forgotten world to the location pool"
    display_name = "Include Forgotten World"

class LoreRemoval(Toggle):
    "Removes story from blocking gameplay"
    display_name = "Remove Story"

class ScoutHints(Toggle):
    "adds hints to the game"
    display_name = "Hints"

class AlwaysDropEgg(Toggle):
    "Makes monsters allways drop their eggs"
    display_name = "Always drops eggs"

#class Openworld(OptionSet):
    "Remove roadblocks from your game"

@dataclass
class MonsterSanctuaryOptions(PerGameCommonOptions):
    goal: Goal
    champreq: GoalChampionReq
    startran: RandomizeStarter
    monrantyp: RandomizeMonstersType
    monrangrp: RandomizeMonsterGrouping
    underche: UnderworldChecks
    forgeche: ForgottenChecks
    remlore: LoreRemoval
    scouhint: ScoutHints


option_groups = [
    OptionGroup("Goal",[
        Goal,
        GoalChampionReq,
    ]),
    OptionGroup("Monster Randomization",[
        RandomizeStarter,
        RandomizeMonstersType,
        RandomizeMonsterGrouping,
        SkillTreeRando,
    ]),
    OptionGroup("Locations",[
        UnderworldChecks,
        ForgottenChecks,
    ]),
    OptionGroup("QOL",[
        LoreRemoval,
        ScoutHints,
        AlwaysDropEgg,
        #Openworld,
    ])
]
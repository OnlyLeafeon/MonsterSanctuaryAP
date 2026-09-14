using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonsterSanctuaryAP.ArchipelagoClient;
using UnityEngine;

namespace MonsterSanctuaryAP
{
    // Ability randomization ported from the old Archipelago.MonsterSanctuary.Client.
    // Randomizes skill trees, ultimates and shift skills for every monster using a
    // Random seeded from the Archipelago slot data seed.
    public static class SkillRandomizer
    {
        private static readonly HashSet<SkillTree> _skillTrees = new();
        private static readonly HashSet<GameObject> _ultimates = new();
        private static readonly HashSet<GameObject> _lightShift = new();
        private static readonly HashSet<GameObject> _darkShift = new();
        private static bool _skillDataInitialized;
        private static System.Random _skillTreeRng;

        public static void Initialize()
        {
            _skillTreeRng = new System.Random(ApState.GetSeedHash());
            if (_skillDataInitialized) return;

            var worldData = GameController.Instance?.WorldData;
            if (worldData?.Referenceables == null) return;

            foreach (var referenceable in worldData.Referenceables)
            {
                if (referenceable == null || referenceable.gameObject == null) continue;
                GameObject go = referenceable.gameObject;
                if (go.GetComponent<Monster>() == null) continue;

                if (go.GetComponent<SkillTree>() != null)
                {
                    foreach (SkillTree skillTree in go.GetComponents<SkillTree>())
                    {
                        _skillTrees.Add(DuplicateComponentType(skillTree));
                    }
                }

                SkillManager sm = go.GetComponent<SkillManager>();
                if (sm != null)
                {
                    sm.Ultimates.ForEach(u => _ultimates.Add(u));
                    _darkShift.Add(sm.DarkSkill);
                    _lightShift.Add(sm.LightSkill);
                }
            }

            _skillDataInitialized = true;
        }

        public static void RandomizeAllMonsterSkillData()
        {
            if (!ApState.IsConnected) return;
            Initialize();
            if (!_skillDataInitialized) return;

            var worldData = GameController.Instance?.WorldData;
            if (worldData?.Referenceables == null) return;

            foreach (var referenceable in worldData.Referenceables)
            {
                if (referenceable == null || referenceable.gameObject == null) continue;
                GameObject go = referenceable.gameObject;
                if (go.GetComponent<Monster>() == null) continue;

                RandomizeSkillTreesForMonster(go);
                RandomizeUltimatesForMonster(go);
                RandomizeShiftSkillsForMonster(go);
            }
        }

        public static void RandomizeSkillTreesForMonster(GameObject monster)
        {
            if (!ApState.IsConnected) return;
            if (monster == null) return;
            if (monster.GetComponent<Monster>() == null) return;
            if (monster.GetComponent<SkillTree>() == null) return;

            SkillTree[] components = monster.GetComponents<SkillTree>();
            for (int i = 0; i < components.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(components[i]);
            }

            int treeCount = (_skillTreeRng.Next(1, 101) <= 25) ? 3 : 4;

            SkillManager skillManager = monster.GetComponent<SkillManager>();
            Monster monsterComp = monster.GetComponent<Monster>();
            skillManager.BaseSkills.RemoveAll(s => s.GetComponent<BaseAction>() != null);

            List<SkillTree> skillTreesAdded = new List<SkillTree>();
            int skillWidth = 0;

            for (int j = 0; j < treeCount; j++)
            {
                IEnumerable<SkillTree> uniqueTrees = _skillTrees.Where(tree => !HasAnyBaseSkill(skillTreesAdded, tree));
                List<SkillTree> candidates = uniqueTrees.Where(t => GetSkillTreeWidth(t) + skillWidth <= 20).ToList();
                if (candidates.Count == 0) break;

                SkillTree skillTree = DuplicateComponentType(candidates[_skillTreeRng.Next(candidates.Count)]);
                skillTreesAdded.Add(skillTree);
                CopyComponentToGameObject(skillTree, monster);
                skillWidth += GetSkillTreeWidth(skillTree);
            }

            int baseSkillCount = (monsterComp.IsSpectralFamiliar ? 2 : 1);
            for (int k = 0; k <= baseSkillCount; k++)
            {
                if (skillTreesAdded.Count == 0) break;

                SkillTree skillTree = skillTreesAdded[_skillTreeRng.Next(skillTreesAdded.Count)];
                GameObject baseSkill = null;
                if (skillTree.Tier1Skills.Any())
                {
                    baseSkill = skillTree.Tier1Skills[_skillTreeRng.Next(skillTree.Tier1Skills.Count)];
                }
                else if (skillTree.Tier2Skills.Any())
                {
                    baseSkill = skillTree.Tier2Skills[_skillTreeRng.Next(skillTree.Tier2Skills.Count)];
                }

                if (baseSkill != null)
                {
                    skillManager.BaseSkills.Add(baseSkill);
                    skillTreesAdded.Remove(skillTree);
                }
            }
        }

        private static int GetSkillTreeWidth(SkillTree tree)
        {
            return new List<int>
            {
                tree.Tier1Skills.Count,
                tree.Tier2Skills.Count,
                tree.Tier3Skills.Count,
                tree.Tier4Skills.Count,
                tree.Tier5Skills.Count
            }.Max();
        }

        public static void RandomizeUltimatesForMonster(GameObject monster)
        {
            if (!ApState.IsConnected) return;
            if (monster == null) return;
            if (monster.GetComponent<Monster>() == null) return;
            if (monster.GetComponent<SkillManager>() == null) return;

            SkillManager skillManager = monster.GetComponent<SkillManager>();
            skillManager.Ultimates = new List<GameObject>();
            while (skillManager.Ultimates.Count < 3)
            {
                if (_ultimates.Count == 0) break;
                List<GameObject> available = _ultimates.Except(skillManager.Ultimates).ToList();
                if (available.Count == 0) break;
                skillManager.Ultimates.Add(available[_skillTreeRng.Next(available.Count)]);
            }
        }

        public static void RandomizeShiftSkillsForMonster(GameObject monster)
        {
            if (!ApState.IsConnected) return;
            if (monster == null) return;
            if (monster.GetComponent<Monster>() == null) return;
            if (monster.GetComponent<SkillManager>() == null) return;

            SkillManager skillManager = monster.GetComponent<SkillManager>();

            List<GameObject> darkPool = _darkShift.ToList();
            if (darkPool.Count > 0)
            {
                skillManager.DarkSkill = darkPool[_skillTreeRng.Next(darkPool.Count)];
            }

            List<GameObject> lightPool = _lightShift.ToList();
            if (lightPool.Count > 0)
            {
                skillManager.LightSkill = lightPool[_skillTreeRng.Next(lightPool.Count)];
            }
        }

        private static bool HasAnyBaseSkill(List<SkillTree> skillTreesAdded, SkillTree tree)
        {
            if (tree.Tier1Skills.Intersect(skillTreesAdded.SelectMany(t => t.Tier1Skills)).Any())
            {
                return true;
            }
            if (!tree.Tier1Skills.Any())
            {
                return tree.Tier2Skills.Intersect(skillTreesAdded.SelectMany(t => t.Tier2Skills)).Any();
            }
            return false;
        }

        private static T CopyComponentToGameObject<T>(T original, GameObject destination) where T : Component
        {
            Type type = original.GetType();
            Component component = destination.AddComponent(type);
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                fieldInfo.SetValue(component, fieldInfo.GetValue(original));
            }
            return component as T;
        }

        private static T DuplicateComponentType<T>(T original) where T : Component
        {
            Type type = original.GetType();
            Component component = (T)(object)Activator.CreateInstance(type);
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                fieldInfo.SetValue(component, fieldInfo.GetValue(original));
            }
            return component as T;
        }
    }
}
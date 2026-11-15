using System;
using System.Collections.Generic;
using UnityEngine;

public enum AttackStat { Strength, Magick }

[Serializable] public struct StatBlock { public int hp, str, mag, agi, cunning; }
[Serializable] public struct CheatCostMod { public string cheatId; public int delta; }  // e.g. sleight_of_hand, -1

// To add a class 
//  Create 
//  Paperboard God Class > fill it in > add it to the ClassRegistry.
[CreateAssetMenu(menuName = "Paperboard God/Class", fileName = "NewClass")]
public class ClassData : ScriptableObject {
    [Header("Identity")]
    public string id = "rogue";
    public string displayName = "Rogue";
    [TextArea] public string description;
    public Sprite portrait;

    [Header("Combat")]
    public StatBlock baseStats;
    public AttackStat attackStat = AttackStat.Strength;
    public int weaponDie = 6;                      // damage = d(weaponDie) + attack stat
    public List<SkillData> skills = new List<SkillData>();

    [Header("Cheats")]
    public int startingAudacity = 3;
    public List<CheatCostMod> cheatCostMods = new List<CheatCostMod>();
}

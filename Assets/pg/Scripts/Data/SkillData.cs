using UnityEngine;

[CreateAssetMenu(menuName = "Paperboard God/Skill", fileName = "NewSkill")]
public class SkillData : ScriptableObject {
    public string id;
    public string displayName;
    [TextArea] public string description;
    public int cooldown = 3;     // skills use cooldowns only
}

using UnityEngine;

// Placeholder numbers throughout: tune with the simulation later.
[CreateAssetMenu(menuName = "Paperboard God/Enemy", fileName = "NewEnemy")]
public class EnemyData : ScriptableObject {
    public string id;
    public string displayName = "Enemy";
    public int hp = 12;
    public int defenseDc = 10;       // DC the hero must meet to hit this enemy
    public int agi = 3;              // initiative
    public int attackMod = 2;        // added to the enemy's attack roll
    public int damageDie = 4;
    public int damageBonus = 1;
    public int goldReward = 15;
    public bool attacks = true;      // false for the Training Dummy
}

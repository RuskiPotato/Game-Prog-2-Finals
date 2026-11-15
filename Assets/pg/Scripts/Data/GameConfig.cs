using System.Collections.Generic;
using UnityEngine;

// The one config asset every scene reads, through GameManager.Instance.Config.
// Must live at Assets/Resources/GameConfig.asset (Paperboard God > Create Default Assets makes it).
[CreateAssetMenu(menuName = "Paperboard God/Game Config", fileName = "GameConfig")]
public class GameConfig : ScriptableObject {
    public ClassRegistry classes;
    public RunConfig run;

    [Header("Prologue enemies")]
    public EnemyData dummy;            // tutorial target
    public EnemyData scriptedBrute;    // the unwinnable first encounter

    [Header("Run enemies")]
    public List<EnemyData> fights = new List<EnemyData>();
    public List<EnemyData> elites = new List<EnemyData>();
    public List<EnemyData> bosses = new List<EnemyData>();    // one per act, in order
}

using UnityEngine;

[CreateAssetMenu(menuName = "Paperboard God/Run Config", fileName = "RunConfig")]
public class RunConfig : ScriptableObject {
    [Header("Structure")]
    public int acts = 3;
    public int nodesPerAct = 5;              // node 1 is always a Fight, last node is always the Boss
    [Header("Guarantees (per act)")]
    public int minCombatPerAct = 2;          // Fight or Elite; the Boss is not counted
    public int shopsPerAct = 1;
    public int maxElitePerAct = 1;
    public int maxSnackPerAct = 1;
    [Header("Resources")]
    public int startingFate = 45;            // Fate when the prologue is skipped (placeholder)
    public int savestatesPerRun = 2;         // Flashback tokens, fixed for the whole run
    [Header("Weights for the free slots")]
    public int fightWeight = 4, eliteWeight = 1, snackWeight = 2, disputeWeight = 2;
}

using System.Collections.Generic;
using UnityEngine;

// The list of every cheat in the game. The battle drawer builds one card per entry.
[CreateAssetMenu(menuName = "Paperboard God/Cheat Registry", fileName = "CheatRegistry")]
public class CheatRegistry : ScriptableObject {
    public List<CheatData> cheats = new List<CheatData>();
    Dictionary<string, CheatData> byId;

    public CheatData Get(string id) {
        if (byId == null) {
            byId = new Dictionary<string, CheatData>();
            foreach (var c in cheats) if (c != null) byId[c.id] = c;
        }
        CheatData found;
        return byId.TryGetValue(id, out found) ? found : null;
    }

    void OnValidate() { byId = null; }
}

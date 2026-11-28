using System;
using System.Collections.Generic;

public enum NodeType { Fight, Elite, Shop, SnackBreak, RulesDispute, Boss }

[Serializable]
public class MapNode {
    public string id;
    public int act;          // zero-based act
    public int index;        // zero-based row within act
    public int lane;         // 0 left, 1 center, 2 right
    public NodeType type;
    public bool visited;
    public bool unlocked;
    public List<string> nextNodeIds = new List<string>();
    public bool IsCombat => type == NodeType.Fight || type == NodeType.Elite || type == NodeType.Boss;
}

public enum NodeType { Fight, Elite, Shop, SnackBreak, RulesDispute, Boss }

[System.Serializable]
public class MapNode {
    public int act;          // 0-based
    public int index;        // 0-based within the act
    public NodeType type;
    public bool visited;

    public bool IsCombat => type == NodeType.Fight || type == NodeType.Elite || type == NodeType.Boss;
}

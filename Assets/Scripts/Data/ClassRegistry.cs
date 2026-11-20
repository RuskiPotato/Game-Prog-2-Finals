using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Paperboard God/Class Registry", fileName = "ClassRegistry")]
public class ClassRegistry : ScriptableObject {
    public List<ClassData> classes = new List<ClassData>();
    public ClassData defaultClass;

    public ClassData Default => defaultClass != null ? defaultClass : (classes.Count > 0 ? classes[0] : null);
    public ClassData Get(string id) => classes.Find(c => c != null && c.id == id);
}

using UnityEngine;

// Reusable singleton base. Inherit: public class Foo : Singleton<Foo> { }
public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T> {
    public static T Instance { get; private set; }

    protected virtual void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = (T)this;
        DontDestroyOnLoad(gameObject);
        OnSingletonAwake();
    }

    protected virtual void OnSingletonAwake() { }
}

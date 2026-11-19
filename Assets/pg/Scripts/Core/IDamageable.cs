public interface IDamageable {
    int Hp { get; }
    int MaxHp { get; }
    bool IsDead { get; }
    void TakeDamage(int amount);
}

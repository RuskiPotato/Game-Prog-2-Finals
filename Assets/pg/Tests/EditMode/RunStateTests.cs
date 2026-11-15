using NUnit.Framework;
using UnityEngine;

public class RunStateTests {
    [Test]
    public void Savestates_are_fixed_per_run_and_run_out() {
        var cfg = ScriptableObject.CreateInstance<RunConfig>();
        Rng.Seed(5);
        var run = new RunState(5, MapGenerator.Generate(cfg), cfg.savestatesPerRun);

        Assert.AreEqual(2, run.Savestates);
        Assert.IsTrue(run.TrySpendSavestate());
        Assert.IsTrue(run.TrySpendSavestate());
        Assert.IsFalse(run.TrySpendSavestate());   // none left, and nothing refills them
        Assert.AreEqual(0, run.Savestates);
    }
}

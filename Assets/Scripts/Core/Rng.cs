using System;

public enum RngStream { Combat, Map, Loot, Events }

// Separate streams so e.g. a loot roll never changes a later dice roll: one seed reproduces a whole run.
public static class Rng {
    const int StreamCount = 4;
    static Random[] streams;

    public static void Seed(int runSeed) {
        streams = new Random[StreamCount];
        for (int i = 0; i < StreamCount; i++)
            streams[i] = new Random(unchecked(runSeed * 31 + i));
    }

    public static Random Get(RngStream s) {
        if (streams == null) Seed(Environment.TickCount);
        return streams[(int)s];
    }
}

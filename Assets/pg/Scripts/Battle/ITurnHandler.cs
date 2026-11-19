using System.Collections;

public interface ITurnHandler {
    IEnumerator Run(BattleContext ctx);
}

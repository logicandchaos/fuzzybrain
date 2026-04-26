using FuzzyBrain;
using FuzzyBrain.Demos;
using UnityEngine;

[CreateAssetMenu(fileName = "StopRandom", menuName = "FuzzyBrain/Acts/StopRandom")]
public class StopRandom : Act
{
    public override void PerformAct(ActContext ctx)
    {
        if (Random.Range(0, 100) > 80)
            ctx.Get<WandererNPC>().Stop();
    }
}

using FuzzyBrain;
using FuzzyBrain.Demos;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveRandom", menuName = "FuzzyBrain/Acts/MoveRandom")]
public class MoveRandom : Act
{
    public override void PerformAct(ActContext ctx)
    {
        if (Random.Range(0, 2) == 0)
            ctx.Get<WandererNPC>().MoveLeft();
        else
            ctx.Get<WandererNPC>().MoveRight();
    }
}

using FuzzyBrain;
using FuzzyBrain.Demos;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveRight", menuName = "FuzzyBrain/Acts/MoveRight")]
public class MoveRight : Act
{
    public override void PerformAct(ActContext ctx)
    {
        ctx.Get<WandererNPC>().MoveRight();
    }
}

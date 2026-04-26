using FuzzyBrain;
using FuzzyBrain.Demos;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveLeft", menuName = "FuzzyBrain/Acts/MoveLeft")]
public class MoveLeft : Act
{
    public override void PerformAct(ActContext ctx)
    {
        ctx.Get<WandererNPC>().MoveLeft();
    }
}

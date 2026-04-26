using UnityEngine;
using FuzzyBrain;
using FuzzyBrain.Demos;

[CreateAssetMenu(fileName = "WandererNPCConditionTest", menuName = "FuzzyBrain/Conditions/WandererNPCConditionTest")]
public class WandererNPCConditionTest : Condition<WandererNPC>
{
    protected override bool Verify(WandererNPC component)
    {
        bool result = component.enabled == false;
        return inverted ? !result : result;
    }
}

using UnityEngine;
using FuzzyBrain;
using FuzzyBrain.Demos;

[CreateAssetMenu(fileName = "WandererNPCConditionTest2", menuName = "FuzzyBrain/Conditions/WandererNPCConditionTest2")]
public class WandererNPCConditionTest2 : Condition<WandererNPC>
{
    protected override bool Verify(WandererNPC component)
    {
        bool result = component.useGUILayout == false;
        return inverted ? !result : result;
    }
}

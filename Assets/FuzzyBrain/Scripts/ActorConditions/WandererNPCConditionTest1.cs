using UnityEngine;
using FuzzyBrain;
using FuzzyBrain.Demos;

[CreateAssetMenu(fileName = "WandererNPCConditionTest1", menuName = "FuzzyBrain/Conditions/WandererNPCConditionTest1")]
public class WandererNPCConditionTest1 : Condition<WandererNPC>
{
    protected override bool Verify(WandererNPC component)
    {
        bool result = component.useGUILayout != true;
        return inverted ? !result : result;
    }
}

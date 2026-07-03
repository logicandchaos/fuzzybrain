using UnityEngine;

public class DustCloud : MonoBehaviour
{
    public void AutoDisable()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Invoke(nameof(AutoDisable), .125f);
    }
}

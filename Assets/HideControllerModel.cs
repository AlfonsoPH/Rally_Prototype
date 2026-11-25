using UnityEngine;

public class HideControllerModel : MonoBehaviour
{
    void LateUpdate()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            r.forceRenderingOff = true;
        }
    }
}
using UnityEngine;
using UnityEngine.InputSystem; // VR input ke liye

public class RightHandcontrol : MonoBehaviour
{
    public WeldingHandle weldingHandle; // Torch ki main script
    public InputActionProperty triggerAction; // VR Trigger button

    [HideInInspector] public bool hasInteracted = false;

    void Update()
    {
        // 1. VR Trigger ki value check karna (0 se 1 tak)
        float triggerValue = triggerAction.action.ReadValue<float>();

        // 2. Agar trigger 10% se zyada daba ho to welding shuru
        if (triggerValue > 0.1f)
        {
            if (weldingHandle != null)
            {
                hasInteracted = true;
                weldingHandle.StartWelding();
                Debug.Log("Welding Starting...");
            }
        }
        else
        {
            if (weldingHandle != null)
            {
                weldingHandle.StopWelding();
            }
        }

        // 3. Tip ki position update karna taakay sparks sahi jagah se nikalain
        if (weldingHandle != null)
        {
            weldingHandle.GetWeldPoint();
        }
    }
}
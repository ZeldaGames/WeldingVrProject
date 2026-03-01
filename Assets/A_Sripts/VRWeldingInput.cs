using UnityEngine;
using UnityEngine.InputSystem; // Controller buttons read karne ke liye

public class VRWelderInput : MonoBehaviour
{
    [Header("Welding References")]
    public WeldingHandle weldingHandle; // Inspector mein Torch (X_MigWelder) yahan drag karein

    [Header("Input Actions")]
    public InputActionProperty triggerAction; // VR Trigger (Simulator mein 'T' key)

    // Purani scripts ki compatibility ke liye variables
    [HideInInspector] public bool isInteracting = false;
    [HideInInspector] public bool isWelderOn = true;

    void Update()
    {
        // 1. Check karna ke trigger kitna daba hua hai (0 to 1)
        float triggerValue = triggerAction.action.ReadValue<float>();

        // 2. Agar trigger 10% se zyada daba ho (0.1f)
        if (triggerValue > 0.1f && isWelderOn)
        {
            if (!isInteracting)
            {
                isInteracting = true;
                Debug.Log("VR Welding Started!");
            }

            // WeldingHandle script ka Start function call karna
            weldingHandle.StartWelding();
        }
        else
        {
            if (isInteracting)
            {
                isInteracting = false;
                Debug.Log("VR Welding Stopped!");

                // WeldingHandle script ka Stop function call karna
                weldingHandle.StopWelding();
            }
        }
    }
}
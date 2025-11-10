using UnityEngine;

/// <summary>
/// DEBUG SCRIPT: Place this on any GameObject to see touch/mouse input in real-time.
/// Shows touch count, positions, and screen info in the console.
/// Remove this script when debugging is complete.
/// </summary>
public class TouchInputDebugger : MonoBehaviour
{
    private void Update()
    {
        // Log screen info once per second
        if(Time.frameCount % 60 == 0)
        {
            Debug.Log($"[INPUT DEBUG] Screen Size: {Screen.width}x{Screen.height}, DPI: {Screen.dpi}");
        }

        // Touch input
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Debug.Log($"[INPUT DEBUG] Touch {i}: Phase={touch.phase}, Position={touch.position}, DeltaPosition={touch.deltaPosition}");
            }
        }

        // Mouse input (for simulator)
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"[INPUT DEBUG] Mouse Down at {Input.mousePosition}");
        }
        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log($"[INPUT DEBUG] Mouse Up at {Input.mousePosition}");
        }
    }
}

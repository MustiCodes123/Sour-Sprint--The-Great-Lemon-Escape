using System.Collections;
using UnityEngine;

/// <summary>
/// TEMPLATE for creating new obstacles.
/// 
/// HOW TO ADD NEW OBSTACLES:
/// 
/// 1. CREATE THE PREFAB:
///    - Create your 3D model/visual in Unity
///    - Add a Collider component (BoxCollider, CapsuleCollider, etc.)
///    - Set the Layer to "Obstacle" (layer 9)
///    - Add this script (or BreakableObstacle) as a component
///    - Add an AudioSource component
///    - Save as a prefab in Assets/Bundles/Obstacles/ folder
/// 
/// 2. MAKE IT ADDRESSABLE:
///    - Select your obstacle prefab
///    - In Inspector, check "Addressable"
///    - Add the label "obstacle" to it
///    - Window > Asset Management > Addressables > Groups
///    - Build Addressables (Build > New Build > Default Build Script)
/// 
/// 3. ADD TO TRACK SEGMENTS:
///    - Open a TrackSegment prefab (Assets/Bundles/Themes/[ThemeName]/Segments/)
///    - In Inspector, find "Possible Obstacles" array
///    - Increase the size and drag your obstacle into the new slot
///    - The obstacle will now spawn randomly during gameplay
/// 
/// OBSTACLE TYPES:
/// - Regular Obstacle (this class): Player must jump over or avoid
/// - BreakableObstacle: Can be destroyed by sliding, damages if hit normally
/// - PatrollingObstacle: Moves side-to-side between lanes
/// - AllLaneObstacle: Blocks all three lanes
/// 
/// </summary>
public class CustomObstacle : Obstacle
{
    [Header("Custom Settings")]
    public float spawnHeight = 0f; // Height offset from ground
    public bool randomLane = true; // Spawn on random lane or all lanes
    
    public override void Setup()
    {
        base.Setup();
        // Initialize any custom variables here
    }

    public override IEnumerator Spawn(TrackSegment segment, float t)
    {
        Vector3 position;
        Quaternion rotation;
        segment.GetPointAtInWorldUnit(t, out position, out rotation);

        if (randomLane)
        {
            // Spawn on a random lane (0=left, 1=center, 2=right)
            int lane = Random.Range(0, 3);
            position += Vector3.right * (lane - 1) * segment.manager.laneOffset;
        }
        // If not random lane, spawns in center by default

        // Apply height offset
        position += Vector3.up * spawnHeight;

        GameObject obj = Instantiate(gameObject);
        obj.transform.SetParent(segment.objectRoot, true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        obj.GetComponent<CustomObstacle>().Setup();

        yield return null;
    }

    public override void Impacted()
    {
        base.Impacted();
        // Add custom impact behavior here (particles, sound, animation)
    }
}

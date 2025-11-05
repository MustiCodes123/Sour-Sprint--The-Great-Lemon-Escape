using System.Collections;
using UnityEngine;

/// <summary>
/// Breakable obstacles can be destroyed by sliding through them.
/// If the player hits them without sliding, they take damage.
/// Examples: Sugar cubes, cookie crumbs, weak barriers
/// </summary>
public class BreakableObstacle : Obstacle
{
    [Header("Breakable Settings")]
    public GameObject brokenPiecesEffect; // Optional particle effect when broken
    public AudioClip breakSound; // Sound when successfully broken
    
    private bool m_Broken = false;

    public override void Setup()
    {
        base.Setup();
        m_Broken = false;
    }

    public override IEnumerator Spawn(TrackSegment segment, float t)
    {
        Vector3 position;
        Quaternion rotation;
        segment.GetPointAtInWorldUnit(t, out position, out rotation);

        // Spawn on a random lane
        int lane = Random.Range(0, 3);
        position += Vector3.right * (lane - 1) * segment.manager.laneOffset;

        GameObject obj = Instantiate(gameObject);
        obj.transform.SetParent(segment.objectRoot, true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        obj.GetComponent<BreakableObstacle>().Setup();

        yield return null;
    }

    // Called when player slides through this obstacle
    public void Break(CharacterInputController character)
    {
        if (m_Broken)
            return;

        m_Broken = true;

        // Play break sound
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && breakSound != null)
        {
            audioSource.PlayOneShot(breakSound);
        }

        // Spawn particle effect if available
        if (brokenPiecesEffect != null)
        {
            GameObject effect = Instantiate(brokenPiecesEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Hide the visual mesh
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Disable the collider so player can pass through
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Destroy this obstacle after a short delay
        Destroy(gameObject, 0.5f);
    }

    public override void Impacted()
    {
        // Don't call base.Impacted() - we handle this differently
        // Player hit this without sliding, so they should take damage
        // The CharacterCollider will handle the damage
    }
}

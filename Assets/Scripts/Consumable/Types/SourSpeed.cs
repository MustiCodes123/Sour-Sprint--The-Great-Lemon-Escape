using UnityEngine;
using System.Collections;

/// <summary>
/// Sour Speed power-up: Makes the player faster and invincible for a short duration.
/// This represents the "Sparkling Slices" collectible in the lemon game.
/// </summary>
public class SourSpeed : Consumable
{
    [Header("Sour Speed Settings")]
    public float speedMultiplier = 1.5f; // Multiplies current speed by this amount
    
    private float m_OriginalMaxSpeed;
    private float m_OriginalSpeed;

    public override string GetConsumableName()
    {
        return "Sour Speed";
    }

    public override ConsumableType GetConsumableType()
    {
        return ConsumableType.SOUR_SPEED;
    }

    public override int GetPrice()
    {
        return 2000; // More expensive than regular powerups
    }

    public override int GetPremiumCost()
    {
        return 7;
    }

    public override IEnumerator Started(CharacterInputController c)
    {
        yield return base.Started(c);
        
        // Make player invincible
        c.characterCollider.SetInvincible(duration);
        
        // Store original speeds
        m_OriginalMaxSpeed = c.trackManager.maxSpeed;
        m_OriginalSpeed = c.trackManager.speed;
        
        // Increase speed
        c.trackManager.maxSpeed *= speedMultiplier;
        // Don't directly multiply current speed to avoid jarring acceleration
    }

    public override void Tick(CharacterInputController c)
    {
        base.Tick(c);
        
        // Keep player invincible
        c.characterCollider.SetInvincibleExplicit(true);
        
        // Gradually increase speed toward the boosted max speed
        if (c.trackManager.speed < c.trackManager.maxSpeed)
        {
            c.trackManager.speed = Mathf.MoveTowards(
                c.trackManager.speed, 
                c.trackManager.maxSpeed, 
                Time.deltaTime * 2f
            );
        }
    }

    public override void Ended(CharacterInputController c)
    {
        base.Ended(c);
        
        // Restore invincibility state
        c.characterCollider.SetInvincibleExplicit(false);
        
        // Restore original max speed
        c.trackManager.maxSpeed = m_OriginalMaxSpeed;
        
        // Gradually reduce current speed back to normal range
        if (c.trackManager.speed > m_OriginalMaxSpeed)
        {
            c.trackManager.speed = m_OriginalMaxSpeed;
        }
    }
}

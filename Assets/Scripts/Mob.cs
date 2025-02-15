using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mob : MonoBehaviour
{
    public string mobName;
    public int maxHealth;
    public int currentHealth;
    public int sightRadius;
    public GameManager gameManager;

    Vector3 movementVector;
    Vector2 targetPos;

    public AudioSource attackSound;

    public int getHealth()
    {
        return currentHealth;
    }

    public void changeHealth(int change)
    {
        currentHealth += change;
    }

    public void setHealth(int newHealth)
    {
        currentHealth = newHealth;
    }

    public void attackPlayer()
    {
        gameManager.HurtPlayer(1);
        gameManager.eventLog.logEvent(mobName + " hit you for 1 dmg.");
    }

    public void moveToPlayer()
    {
        Vector2 playerPos = gameManager.GetPlayerPos();
        targetPos = (Vector2) transform.position;
        // Find whether to travel west, east, or neither
        float distX = playerPos.x - transform.position.x;
        // if negative, mob -> player (mob needs to go east)
        // if positive, player <- mob (mob needs to go west)
        // if zero, neither
        if (distX < 0)
        {
            targetPos.x -= 1;
        }
        else if (distX > 0)
        {
            targetPos.x += 1;
        }

        float distY = playerPos.y - transform.position.y;

        if (distY < 0)
        {
            targetPos.y -= 1;
        }
        else if (distY > 0)
        {
            targetPos.y += 1;
        }

        if (gameManager.TileFree(transform, targetPos))
        {
            transform.localPosition = targetPos;
        }

    }

    // is the player in sight?
    public bool inSight()
    {
        Vector2 playerPos = gameManager.GetPlayerPos();
        float checkX = playerPos.x - transform.position.x;
        float checkY = playerPos.y - transform.position.y;

        return ((Mathf.Abs(checkX) <= sightRadius) &&
                (Mathf.Abs(checkY) <= sightRadius));
    }

    // Move around randomly.
    public void bumble()
    {

    }

    // Determines and executes the mob's action this turn.
    public void makeMove()
    {
        // First priority: attack the player if touching.
        if (gameManager.CanFight(this))
        {
            attackPlayer();
            attackSound.Play();
            return;
        }
        // Second priority: Walk toward the player if they are in sight radius.
        if (inSight())
            moveToPlayer();

        // Third priority: Bumble around if neither touching or in sight radius.
    }
    
    // Start is called before the first frame update
    void Start()
    {
        movementVector = new Vector3(0.0f, 0.0f, 0.0f);
        targetPos = new Vector2(0.0f, 0.0f);
        currentHealth = maxHealth;
    }

}

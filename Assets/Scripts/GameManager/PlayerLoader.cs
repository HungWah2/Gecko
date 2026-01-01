using UnityEngine;

public class PlayerLoader : MonoBehaviour
{
    public Transform playerTransform;
    public PlayerHealth playerHealthComponent;

    public void ApplySave(SaveData save)
    {
        if (playerTransform != null)
        {
            playerTransform.position = new Vector3(save.playerX, save.playerY, 0f);
        }

        if (playerHealthComponent != null)
        {
            // First set max health, then set current health
            playerHealthComponent.SetMaxHealth(save.playerMaxHealth);
            playerHealthComponent.SetHealth(save.playerHealth);
            
            Debug.Log($"[PlayerLoader] Loaded health: {save.playerHealth}/{save.playerMaxHealth}");
        }
    }
}

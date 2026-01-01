using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopSlot : MonoBehaviour
{
    public Image icon;
    public TMP_Text itemNameText;
    public TMP_Text priceText;
    public Button buyButton;

    private ItemObject item;
    private int price;

    public void SetSlot(ItemObject newItem, int newPrice)
    {
        item = newItem;
        price = newPrice;

        if (item != null)
        {
            icon.enabled = true;
            icon.sprite = item.icon;

            itemNameText.text = item.itemName;
            priceText.text = newPrice.ToString();
            buyButton.interactable = true;

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                BuyItem();
            });
            
            // Update button color based on affordability
            UpdateButtonState();
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        item = null;
        icon.enabled = false;
        itemNameText.text = "";
        priceText.text = "";
        buyButton.interactable = false;
        buyButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Refresh the slot to update affordability (call when money changes)
    /// </summary>
    public void RefreshSlot()
    {
        if (item != null)
        {
            UpdateButtonState();
        }
    }

    /// <summary>
    /// Update button visual state based on whether player can afford the item
    /// </summary>
    private void UpdateButtonState()
    {
        if (MoneyManager.Instance == null) return;

        bool canAfford = MoneyManager.Instance.Money >= price;
        
        // Update button interactability and visual feedback
        buyButton.interactable = canAfford;
        
        // Optional: Change text color based on affordability
        if (priceText != null)
        {
            priceText.color = canAfford ? Color.white : Color.red;
        }
    }

    void BuyItem()
    {
        Debug.Log("[ShopSlot] ====== BUY ITEM CALLED ======");
        
        if (item == null)
        {
            Debug.LogWarning("[ShopSlot] Cannot buy - item is null");
            return;
        }

        Debug.Log($"[ShopSlot] Attempting to buy: {item.itemName} for {price} leaves");

        if (MoneyManager.Instance == null)
        {
            Debug.LogError("[ShopSlot] MoneyManager.Instance is NULL!");
            return;
        }

        Debug.Log($"[ShopSlot] Current money: {MoneyManager.Instance.Money}");

        if (GameManager.Instance == null)
        {
            Debug.LogError("[ShopSlot] GameManager.Instance is NULL!");
            return;
        }

        if (GameManager.Instance.InventoryInstance == null)
        {
            Debug.LogError("[ShopSlot] GameManager.Instance.InventoryInstance is NULL!");
            return;
        }

        // Check if player has enough money
        if (GameManager.Instance.MoneyManagerInstance.Money >= price)
        {
<<<<<<< HEAD
            Debug.Log($"[ShopSlot] Player has enough money ({MoneyManager.Instance.Money} >= {price})");
            
            // Try to add item to inventory first (check if there's space)
            Debug.Log($"[ShopSlot] Attempting to add {item.itemName} to inventory...");
            int leftover = GameManager.Instance.InventoryInstance.AddItem(item, 1);
            
            Debug.Log($"[ShopSlot] AddItem returned leftover: {leftover}");
            
            if (leftover == 0)
            {
                // Successfully added to inventory
                Debug.Log($"[ShopSlot] Successfully added item to inventory");
                
                // Deduct money
                int moneyBefore = MoneyManager.Instance.Money;
                MoneyManager.Instance.AddMoney(-price);
                int moneyAfter = MoneyManager.Instance.Money;
                
                Debug.Log($"[ShopSlot] ? Money deducted: {moneyBefore} -> {moneyAfter}");
                Debug.Log($"[ShopSlot] ? PURCHASE COMPLETE: Bought {item.itemName} for {price} leaves");
                
                // Play purchase sound if available
                if (AudioManager.Instance != null)
                {
                    // Play a buy sound if you have one
                    Debug.Log("[ShopSlot] Playing purchase sound...");
                }
                
                // Refresh all shop slots to update affordability
                RefreshAllShopSlots();
                
                // Save game after purchase
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SaveGameEvent($"Purchased {item.itemName}");
                    Debug.Log("[ShopSlot] Game saved after purchase");
                }
            }
            else
            {
                // Inventory is full
                Debug.LogWarning($"[ShopSlot] ? Cannot buy {item.itemName} - Inventory is full! (leftover: {leftover})");
            }
=======
            // Deduct money
            GameManager.Instance.MoneyManagerInstance.AddMoney(-price);

            // Add item to inventory
            GameManager.Instance.InventoryInstance.AddItem(item, 1);

            Debug.Log($"Bought: {item.itemName} for {price} coins.");
>>>>>>> 41bc57b2514121a586a5074ee62989b0f81fc137
        }
        else
        {
            // Not enough money
            int needed = price - MoneyManager.Instance.Money;
            Debug.LogWarning($"[ShopSlot] ? Not enough money to buy {item.itemName}. Have: {MoneyManager.Instance.Money}, Need: {price}, Short: {needed} leaves");
        }
        
        Debug.Log("[ShopSlot] ====== BUY ITEM FINISHED ======");
    }

    /// <summary>
    /// Refresh all shop slots (call from parent ShopUI if available)
    /// </summary>
    private void RefreshAllShopSlots()
    {
        var shopUI = GetComponentInParent<ShopUI>();
        if (shopUI != null)
        {
            shopUI.RefreshShopUI();
        }
    }
}

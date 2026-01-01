using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI / Slots")]
    public GameObject inventoryPanel;
    public ItemSlot[] itemSlot;

    [Header("Navigation")]
    private PlayerControls inputActions;
    private PlayerInput playerInput;

    private int selectedIndex = 0;
    [SerializeField] private int columns = 5;

    // ========== Unity lifecycle ==========
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Input actions (keep your existing bindings)
        inputActions = new PlayerControls();
        inputActions.Player.Inventory.performed += ctx => ToggleInventory();
        inputActions.Inventory.CloseInventory.performed += ctx => ToggleInventory();
        inputActions.Inventory.NavigateLeft.performed += ctx => SelectPreviousSlot();
        inputActions.Inventory.NavigateRight.performed += ctx => SelectNextSlot();
        inputActions.Inventory.NavigateUp.performed += ctx => SelectSlotAbove();
        inputActions.Inventory.NavigateDown.performed += ctx => SelectSlotBelow();


        // Hotbar key bindings
        inputActions.Player.Hotbar1.performed += ctx => UseHotbarSlot(0);
        inputActions.Player.Hotbar2.performed += ctx => UseHotbarSlot(1);
        inputActions.Player.Hotbar3.performed += ctx => UseHotbarSlot(2);
        inputActions.Player.Hotbar4.performed += ctx => UseHotbarSlot(3);
        inputActions.Player.Hotbar5.performed += ctx => UseHotbarSlot(4);

        playerInput = FindAnyObjectByType<PlayerInput>();
    }

    private void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }


    private void OnEnable()
    {
        if (inputActions != null) inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null) inputActions.Player.Disable();
    }

    // ========== Inventory UI toggle ==========
    private void ToggleInventory()
    {
        if (inventoryPanel == null) return;

        bool isActive = inventoryPanel.activeSelf;
        inventoryPanel.SetActive(!isActive);

        if (!isActive)
        {
            // Play open inventory sound
            if (GameManager.Instance.AudioInstance != null)
                GameManager.Instance.AudioInstance.PlayOpenInventory();

            //Show amount of money when open inventory
            GameManager.Instance.MoneyManagerInstance.ShowMoneyUI();

            //time stop while player open inventory
            Time.timeScale = 0f;
            //this one just to make sure player input is disable 
            if (playerInput != null) playerInput.enabled = false;

            inputActions.Player.Disable(); //disable input for player
            inputActions.Inventory.Enable(); //enable input for inventory

            PlayerMovement playerMovement = GameManager.Instance.PlayerInstance.GetComponent<PlayerMovement>();
            playerMovement.canMove = false; //disable player movement
            SelectSlot(selectedIndex);
        }
        else
        {
            // Play close inventory sound
            if (GameManager.Instance.AudioInstance != null)
                GameManager.Instance.AudioInstance.PlayCloseInventory();

            //Stop showing amount of money when close inventory
            GameManager.Instance.MoneyManagerInstance.HideMoneyUI();

            //time continue after player close inventory
            Time.timeScale = 1f;
            if (playerInput != null) playerInput.enabled = true;

            inputActions.Player.Enable(); //enable input for player
            inputActions.Inventory.Disable(); //disable input for inventory

            PlayerMovement playerMovement = GameManager.Instance.PlayerInstance.GetComponent<PlayerMovement>();
            playerMovement.canMove = true; //enable player movement
        }
    }
    // Use hot bar
    private void UseHotbarSlot(int index)
    {
        if (itemSlot != null && index >= 0 && index < itemSlot.Length && itemSlot[index] != null)
        {
            itemSlot[index].UseItem();
        }
    }

    // ========== Add item (stacks and fills) ==========
    public int AddItem(ItemObject itemObject, int quantity)
    {
        if (itemObject == null || quantity <= 0) return quantity;

        int original = quantity;
        int leftOver = quantity;

        // Stack into existing slots
        foreach (var slot in itemSlot)
        {
            if (slot == null) continue; // Prevent NullReferenceException
            if (slot.item == itemObject)
            {
                leftOver = slot.AddItem(itemObject, leftOver);
                if (leftOver <= 0)
                {
                    return 0;
                }
            }
        }

        // Fill empty slots
        foreach (var slot in itemSlot)
        {
            if (slot == null) continue; // Prevent NullReferenceException
            if (slot.item == null)
            {
                leftOver = slot.AddItem(itemObject, leftOver);
                if (leftOver <= 0)
                {
                    return 0;
                }
            }
        }

        return leftOver;
    }

    // ========== Slot selection (unchanged logic) ==========
    public void DeselectAllSlots()
    {
        foreach (var slot in itemSlot)
        {
            if (slot == null) continue;
            if (slot.selectedRect != null)
                slot.selectedRect.SetActive(false);
            slot.thisItemSelected = false;
        }
    }

    public void SelectSlot(int index)
    {
        DeselectAllSlots();
        selectedIndex = Mathf.Clamp(index, 0, itemSlot.Length - 1);
        itemSlot[selectedIndex].selectedRect.SetActive(true);
        itemSlot[selectedIndex].thisItemSelected = true;

        // Update description for keyboard navigation
        itemSlot[selectedIndex].OnLeftClick();
    }


    public void SelectNextSlot() => SelectSlot((selectedIndex + 1) % itemSlot.Length);
    public void SelectPreviousSlot() => SelectSlot((selectedIndex - 1 + itemSlot.Length) % itemSlot.Length);

    public void SelectSlotAbove()
    {
        int above = selectedIndex - columns;
        if (above < 0) above += itemSlot.Length;
        SelectSlot(above);
    }

    public void SelectSlotBelow()
    {
        int below = selectedIndex + columns;
        if (below >= itemSlot.Length) below -= itemSlot.Length;
        SelectSlot(below);
    }

    // ========== SAVE / LOAD ==========
    public List<SaveData.InventorySlotData> GetInventorySnapshot()
    {
        var list = new List<SaveData.InventorySlotData>();

        foreach (var slot in itemSlot)
        {
            list.Add(new SaveData.InventorySlotData
            {
                itemID = slot.item != null ? slot.item.itemID : -1,
                quantity = slot.quantity
            });
        }
        return list;
    }

    public void ApplyInventorySnapshot(List<SaveData.InventorySlotData> data)
    {
        if (data == null) return;

        for (int i = 0; i < itemSlot.Length; i++)
        {
            itemSlot[i].ClearSlot();
            if (i >= data.Count) continue;

            if (data[i].itemID != -1)
            {
                ItemObject item = ItemDatabase.GetItemByID(data[i].itemID);
                if (item != null)
                {
                    itemSlot[i].item = item;
                    itemSlot[i].quantity = data[i].quantity;
                    itemSlot[i].UpdateSlotUI();
                }
            }
        }
    }

    /// <summary>
    /// Force save the current inventory state
    /// </summary>
    public void SaveInventory()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGameEvent("Inventory changed");
            Debug.Log("[InventoryManager] Inventory saved via GameManager");
        }
        else
        {
            Debug.LogWarning("[InventoryManager] Cannot save inventory - GameManager.Instance is null");
        }
    }

    // small helpers + data structures
    [System.Serializable]
    public class ItemSlotSave
    {
        public int itemID;
        public int quantity;
    }

    [System.Serializable]
    public class SaveWrapper
    {
        public List<ItemSlotSave> items;
        public int money;

        public SaveWrapper(List<ItemSlotSave> items, int money) 
        { 
            this.items = items;
            this.money = money;
        }
    }
}

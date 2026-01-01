using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    public int Money { get; private set; }
    public GameObject moneyUI;
    public TMP_Text moneyText;

    private void Awake()
    {
        moneyUI.SetActive(false);
    }

    public int GetMoneySnapshot() => Money;
    public void ApplyMoneySnapshot(int amount) => SetMoney(amount);

    public void AddMoney(int amount)
    {
        Money += amount;
        UpdateMoneyUI();
        ShowMoneyUI();
        CancelInvoke(nameof(HideMoneyUI));
        Invoke(nameof(HideMoneyUI), 2f); // Hide after 2s
    }

    public void ShowMoneyUI()
    {
        moneyUI.SetActive(true);
        UpdateMoneyUI();
    }

    public void HideMoneyUI()
    {
        moneyUI.SetActive(false);
    }

    public void UpdateMoneyUI()
    {
        moneyText.text = $"{Money}";
    }

    public void SetMoney(int amount)
    {
        Money = amount;
        UpdateMoneyUI();
    }

}

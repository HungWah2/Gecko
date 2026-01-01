using Unity.Cinemachine;
using UnityEngine;

public class MoneyDrop : MonoBehaviour
{
    public int amount = 1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) // Fixed: Use collision.gameObject to access CompareTag
        {
            GameManager.Instance.MoneyManagerInstance.AddMoney(amount);
            Destroy(gameObject);
        }
    }
}

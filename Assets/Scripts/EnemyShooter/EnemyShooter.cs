using System.Collections;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public CircleCollider2D shootZone;

    [SerializeField] private float shootCooldown = 15f;

    private Transform player;
    private Animator animator;

    private bool isPlayer = false;
    private bool isBusy = false;
    private bool animDone = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (GameManager.Instance.PlayerInstance != null)
            player = GameManager.Instance.PlayerInstance.transform;
    }

    private void Update()
    {
        if (player == null) return;
        Debug.Log("Is Player: " + isPlayer);
        FacePlayer();
        if (isPlayer && !isBusy)
            StartCoroutine(ShootRoutine());
    }

    public void CheckPlayerInZone(bool inZone)
    {
        isPlayer = inZone;
    }

    void FacePlayer()
    {
        Vector3 scale = transform.localScale;
        scale.x = (player.position.x < transform.position.x)
            ? Mathf.Abs(scale.x)
            : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    IEnumerator ShootRoutine()
    {
        isBusy = true;
        animDone = false;
        animator.SetTrigger("Shoot");

        // Wait until animation event ends it
        yield return new WaitUntil(() => animDone);
        yield return new WaitForSeconds(shootCooldown);

        animDone = false;
        isBusy = false;
    }

    //  Animation Event (fire frame)
    public void Shoot()
    {
        if (player == null) return;

        Vector2 direction = (player.position - firePoint.position).normalized;

        GameObject proj = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        EnemyProjectile projectile = proj.GetComponent<EnemyProjectile>();
        if (projectile != null)
            projectile.SetDirection(direction);
    }

    // Animation Event (last frame)
    public void AnimAttackFinished()
    {
        animDone = true;
    }
}

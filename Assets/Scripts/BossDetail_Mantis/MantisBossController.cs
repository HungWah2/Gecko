using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class MantisBossController : MonoBehaviour, IDamageable
{
    [Header("Intro")]
    public Transform spawnPoint;                // optional spawn point above scene
    public Transform finalPoint;
    public Vector2 spawnOffset = new Vector2(0f, 8f);
    public float jumpDownVelocity = -12f;
    public float waitAfterLand = 0.18f;
    public AudioClip screamClip;
    public float hpLoadDuration = 1.25f;
    public bool requireIntroBeforeFight = true; // blocks AI until intro complete

    [Header("Optional UI")]
    public Image hpBarImage;                    // assign your HP bar Image (fillAmount)
    public GameObject hpContainer;              // parent object for HP UI to enable/disable

    [Header("Optional control lock")]
    public MonoBehaviour playerControllerToDisable;


    [Header("Phases")]
    public BossPhase[] phases; // Phase1 then Phase2

    [Header("HP")]
    public int maxHP = 25;

    [Header("Refs")]
    public Animator anim;
    public AudioSource audioSource;

    [Header("Prefabs")]
    public GameObject vineProjectilePrefab;
    public Transform projectileSpawnPoint;

    [Header("Stretch / DOT")]
    public float stretchHoldTime = 3f;
    public float stretchDotInterval = 2f;
    public float stretchDotDamage = 0.5f;

    Rigidbody2D rb;
    int currentHP;
    int currentPhaseIndex = 0;
    bool busy = false;
    bool lockedFacing = false;
    bool dead = false;
    bool phaseTransitioning = false;

    private Transform player;
    private BossAttack lastAttack = null;
    private bool animAttackFinished = false;

    // Intro state flags
    bool introRunning = false;
    bool introCompleted = false;

    int GetFacingSign()
    {
        return transform.localScale.x >= 0 ? -1 : 1;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (requireIntroBeforeFight)
        {
            currentHP = 0;
            introCompleted = false;
        }
        else
        {
            currentHP = maxHP;
            introCompleted = true;
        }

        // Ensure HP UI shows initial state
        if (hpContainer != null)
        {
            // hide HP UI if intro is required; show otherwise
            hpContainer.SetActive(!requireIntroBeforeFight);
        }
        UpdateHpUI();

        ApplyPhaseStats();
    }

    void Start()
    {
        if (GameManager.Instance.PlayerInstance != null)
            player = GameManager.Instance.PlayerInstance.transform;
    }

    void UpdateHpUI()
    {
        if (hpBarImage != null)
        {
            hpBarImage.fillAmount = (float)currentHP / (float)maxHP;
        }
    }

    void Update()
    {
        if (dead || player == null) return;

        // BLOCK normal AI until intro completes if requested
        if (requireIntroBeforeFight && (introRunning || !introCompleted)) return;

        if (!lockedFacing)
            UpdateFacing();
        if (!busy && !phaseTransitioning)
        {
            TryPhaseTransition();
            StartCoroutine(DecisionLoop());
        }
    }

    void UpdateFacing()
    {
        float dx = transform.position.x - player.position.x;
        if (Mathf.Abs(dx) < 0.1f) return;
        int facingSign = dx > 0f ? 1 : -1;        // +1 => facing left(no rotate), -1 => right(rotate)
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * facingSign;
        transform.localScale = s;
    }

    void TryPhaseTransition()
    {
        if (phaseTransitioning) return;
        if (currentPhaseIndex + 1 >= phases.Length) return;
        // you asked phase change at 13 HP left; use exact HP check
        if (currentHP <= 13 && currentPhaseIndex == 0)
        {
            Debug.Log("Boss: Phase transition triggered!");
            currentPhaseIndex = 1;
            ApplyPhaseStats();
            StartCoroutine(PhaseShiftStagger());
        }
    }

    void ApplyPhaseStats()
    {
        if (phases == null || phases.Length == 0) return;
        var p = phases[Mathf.Clamp(currentPhaseIndex, 0, phases.Length - 1)];
        rb.gravityScale = p.gravityScale;
    }

    IEnumerator PhaseShiftStagger()
    {
        phaseTransitioning = true;
        busy = true;
        anim.SetTrigger("PhaseShift");
        yield return new WaitForSeconds(1f);
        busy = false;
        phaseTransitioning = false;
    }

    IEnumerator DecisionLoop()
    {
        busy = true;
        var phase = phases[Mathf.Clamp(currentPhaseIndex, 0, phases.Length - 1)];
        var atk = phase.attacks[Random.Range(0, phase.attacks.Length)];

        // spacing
        float dist = Mathf.Abs(player.position.x - transform.position.x);
        if (dist > atk.preferredRange + 0.5f)
        {
            anim.SetFloat("MoveDir", 1f);
            yield return MoveHorizontally(player.position.x, phase.moveSpeed, atk.preferredRange);
        }
        else if (dist < atk.preferredRange - 0.5f)
        {
            float stepBackDistance = 2.0f;
            float awayDir = Mathf.Sign(transform.position.x - player.position.x);
            float targetX = transform.position.x + awayDir * stepBackDistance;
            anim.SetFloat("MoveDir", -1f);
            yield return MoveHorizontally(targetX, phase.moveSpeed, 0.6f);
        }
        anim.SetFloat("MoveDir", 0f);
        // execute
        yield return StartCoroutine(ExecuteAttack(atk));
        // decision gap
        yield return new WaitForSeconds(Random.Range(phase.minDecisionGap, phase.maxDecisionGap));
        busy = false;
    }

    IEnumerator MoveHorizontally(float targetX, float speed, float stopWithin)
    {
        float timeout = 10f;
        float t = 0f;

        anim.SetBool("IsMoving", true);

        while (Mathf.Abs(targetX - transform.position.x) > stopWithin && t < timeout)
        {
            float dir = Mathf.Sign(targetX - transform.position.x);
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            yield return null;
            t += Time.deltaTime;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetBool("IsMoving", false);
    }

    IEnumerator ExecuteAttack(BossAttack atk)
    {
        lastAttack = atk;
        animAttackFinished = false;

        CacheAttackDataToHitbox(atk);

        //// Special logic for DeadlyHunt: use a dedicated coroutine that still follows the same lifecycle
        //if (atk.name == "DeadlyHunt" || atk.name == "Deadly Hunt") // handle common naming variants
        //{
        //    // Play windup (use atk.windupTrigger if present)
        //    if (!string.IsNullOrEmpty(atk.windupTrigger))
        //        anim.SetTrigger(atk.windupTrigger);
        //    else
        //        anim.SetTrigger("Windup");

        //    PlaySfx(atk);
        //    if (atk.lockFacingDuringAttack) lockedFacing = true;

        //    // Run the jump + attack behavior which will enable/disable hitbox at proper times.
        //    yield return StartCoroutine(JumpTowardPlayerAndGrab(atk));

        //    // Wait for the animation event to mark attack finished (or fallback to a timeout)
        //    float waitTimeout = atk.windup + atk.active + 1.0f;
        //    float waited = 0f;
        //    while (!animAttackFinished && waited < waitTimeout)
        //    {
        //        waited += Time.deltaTime;
        //        yield return null;
        //    }

        //    // recovery
        //    lockedFacing = false;
        //    yield return new WaitForSeconds(atk.recovery);

        //    // cleanup
        //    lastAttack = null;
        //    animAttackFinished = false;
        //}
        //else
        //{
            // windup
            if (!string.IsNullOrEmpty(atk.windupTrigger))
                anim.SetTrigger(atk.windupTrigger);
            else
                anim.SetTrigger("Windup");

            PlaySfx(atk);
            if (atk.lockFacingDuringAttack) lockedFacing = true;

            if (!string.IsNullOrEmpty(atk.attackTrigger))
                anim.SetTrigger(atk.attackTrigger);
            else
                anim.SetTrigger("Attack");

            yield return new WaitUntil(() => animAttackFinished);

            // recovery
            lockedFacing = false;
            yield return new WaitForSeconds(atk.recovery);

            // clean
            lastAttack = null;
            animAttackFinished = false;
        //}
    }

    //IEnumerator JumpTowardPlayerAndGrab(BossAttack atk)
    //    {
    //    // Calculate jump direction and velocity
    //    Vector2 start = transform.position;
    //    Vector2 target = player.position;
    //    float jumpHeight = 4f; // Adjust as needed
    //    float gravity = Physics2D.gravity.y * rb.gravityScale;
    //    float horizontalDistance = target.x - start.x;

    //    if (gravity >= 0f) gravity = -9.81f * rb.gravityScale;


    //    float timeToApex = Mathf.Sqrt(Mathf.Max(0.01f, -2f * jumpHeight / gravity));
    //    float totalTime = Mathf.Max(0.05f, timeToApex * 2f);
    //    float vx = horizontalDistance / totalTime;
    //    float vy = Mathf.Sqrt(Mathf.Max(0f, -2f * gravity * jumpHeight));

    //    rb.linearVelocity = new Vector2(vx, vy);

    //    float windupDelay = Mathf.Clamp(atk.windup, 0.05f, 1.5f); 
    //    yield return new WaitForSeconds(windupDelay * 0.6f);

    //    if (!string.IsNullOrEmpty(atk.attackTrigger))
    //        anim.SetTrigger(atk.attackTrigger);
    //    else
    //        anim.SetTrigger("Attack");

    //    yield return new WaitForSeconds(Mathf.Max(0.05f, windupDelay * 0.4f));
    //    EnableHitboxByName(atk.hitboxName);

    //    float landingTimeout = totalTime + 1.0f;
    //    float t = 0f;
    //    while (!(rb.linearVelocity.y <= 0f && IsGrounded()) && t < landingTimeout)
    //    {
    //        t += Time.deltaTime;
    //        yield return null;
    //    }
    //    DisableHitboxByName(atk.hitboxName);
    //    animAttackFinished = true;

    //}

    //bool IsGrounded()
    //{
    //        // Simple ground check, adjust as needed for your setup
    //        return Mathf.Abs(rb.linearVelocity.y) < 0.01f && Physics2D.Raycast(transform.position, Vector2.down, 1.1f, LayerMask.GetMask("Ground"));
    //}



    private void CacheAttackDataToHitbox(BossAttack atk)
    {
        var t = transform.Find(atk.hitboxName);
        if (t == null) return;
        var hb = t.GetComponent<MantisHitbox>();
        if (hb != null)
        {
            hb.damage = atk.damage;
        }
    }

    // Enable hitbox by path
    public void EnableHitboxByName(string path)
    {
        var t = transform.Find(path);
        if (t == null)
        {
            Debug.LogWarning("MantisBossController: Hitbox not found: " + path);
            return;
        }
        var col = t.GetComponent<Collider2D>();
        if (col) col.enabled = true;

        var mh = t.GetComponent<MantisHitbox>();
        mh?.ApplyHit();
    }

    // Disable hitbox by path
    public void DisableHitboxByName(string path)
    {
        var t = transform.Find(path);
        if (t == null)
        {
            Debug.LogWarning("MantisBossController: Hitbox not found: " + path);
            return;
        }
        var col = t.GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }

    // Spawn vine projectiles
    public void SpawnVineProjectiles()
    {
        if (vineProjectilePrefab == null || projectileSpawnPoint == null) return;
        int count = (currentPhaseIndex >= 1) ? 3 : 1;
        float[] anglesLocal = (count == 1) ? new float[] { Random.Range(0f, 5f), Random.Range(30f, 35f) } : new float[] { Random.Range(0f, 5f), Random.Range(20f, 25f), Random.Range(40f, 45f), Random.Range(60f, 65f) };
        int facing = GetFacingSign();
        float baseAngle = (facing == 1) ? 0f : 180f;  //left:0, right:180

        foreach (var a in anglesLocal)
        {
            float final = baseAngle + a * facing;
            var go = Instantiate(vineProjectilePrefab, projectileSpawnPoint.position, Quaternion.Euler(0, 0, final));
            var vp = go.GetComponent<VineProjectile>();
            if (vp != null)
            {
                vp.damage = lastAttack.damage;
                vp.Launch(final);
            }
        }
    }

    //IEnumerator StretchDotCoroutine(PlayerHealth ph, float hold)
    //{
    //    float elapsed = 0f;
    //    while (elapsed < hold && ph != null && ph.IsAlive)
    //    {
    //        yield return new WaitForSeconds(stretchDotInterval);
    //        ph.TakeDamage(stretchDotDamage, transform.position, 0f);
    //        elapsed += stretchDotInterval;
    //    }
    //}

    public void AnimAttackFinished()
    {
        animAttackFinished = true;
    }

    void PlaySfx(BossAttack a)
    {
        if (audioSource && a != null) { /* audioSource.PlayOneShot(a.sfxWindup) if exists */ } 
    }

    // Damage API from player's weapon/projectile should call this
    public void TakeDamageFromPlayer(float dmg)
    {
        if (dead) return;
        
        // Play enemy hit sound
        GameManager.Instance.AudioInstance?.PlayEnemyGetHit();
        
        currentHP -= Mathf.RoundToInt(dmg); // boss HP is integer by spec
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        // update UI immediately
        UpdateHpUI();

        if (currentHP <= 0) Die();
    }

    void Die()
    {
        if (dead) return;
        dead = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Die");

        // show empty bar immediately
        currentHP = 0;
        UpdateHpUI();

        // disable hitboxes
        foreach (var c in GetComponentsInChildren<BoxCollider2D>()) if (c.isTrigger) c.enabled = false;
        // open door / spawn loot etc.
    }

    public void TakeDamage(int amount, Vector2? knockback = null)
    {
        TakeDamageFromPlayer(amount);
    }

    public void ResetBoss()
    {
        dead = false;
        busy = false;
        phaseTransitioning = false;
        introRunning = false;
        introCompleted = !requireIntroBeforeFight;

        currentHP = requireIntroBeforeFight ? 0 : maxHP;
        UpdateHpUI();

        // Re-enable hitboxes
        foreach (var c in GetComponentsInChildren<BoxCollider2D>())
            if (c.isTrigger) c.enabled = true;

        // Reset position if needed
        if (spawnPoint != null)
            transform.position = spawnPoint.position;
    }


    /// <summary>
    /// Call this to start the intro (from interactable)
    /// </summary>
    public void StartIntro()
    {
        if (introRunning || introCompleted) return;
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        introRunning = true;
        busy = true; // block decision loop

        // Show HP UI when intro starts (if assigned)
        if (hpContainer != null) hpContainer.SetActive(true);

        // optionally disable player controls
        if (playerControllerToDisable != null) playerControllerToDisable.enabled = false;

        // compute spawn & final positions
        Vector3 finalPos = finalPoint.position;
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : (finalPos + (Vector3)spawnOffset);

        // move boss to spawn
        transform.position = spawnPos;

        // prepare physics
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.simulated = true;
        rb.gravityScale = Mathf.Abs(rb.gravityScale) < 0.001f ? 1f : rb.gravityScale;

        yield return new WaitForSeconds(0.05f);

        // play JumpDown anim if available
        if (anim != null) anim.SetTrigger("JumpDown");

        // force downward velocity for deterministic fall
        rb.linearVelocity = new Vector2(0f, jumpDownVelocity);

        // wait for landing
        yield return StartCoroutine(WaitForLanding(finalPos));

        // snap, stop physics
        transform.position = finalPos;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // small pause
        yield return new WaitForSeconds(waitAfterLand);

        // scream
        if (anim != null) anim.SetTrigger("Scream");
        float screamLen = 0f;
        if (audioSource != null && screamClip != null)
        {
            audioSource.PlayOneShot(screamClip);
            screamLen = screamClip.length;
        }
        if (screamLen > 0f) yield return new WaitForSeconds(screamLen);
        else yield return new WaitForSeconds(0.35f);

        // load HP 0 -> max and update UI
        yield return StartCoroutine(LoadHpRoutine(hpLoadDuration));

        // fight start animation
        if (anim != null) anim.SetTrigger("FightStart");

        // re-enable physics and player controls
        rb.simulated = true;
        if (playerControllerToDisable != null) playerControllerToDisable.enabled = true;

        busy = false;
        introRunning = false;
        introCompleted = true;
    }

    IEnumerator WaitForLanding(Vector3 finalPos)
    {
        float timeout = 5f;
        float t = 0f;
        while (t < timeout)
        {
            t += Time.deltaTime;
            float vertVel = Mathf.Abs(rb.linearVelocity.y);
            float dist = Vector2.Distance(transform.position, finalPos);

            if (vertVel < 0.8f || dist < 0.15f)
            {
                // small extra stabilization
                yield return new WaitForSeconds(0.05f);
                yield break;
            }

            yield return null;
        }
        yield break;
    }

    IEnumerator LoadHpRoutine(float duration)
    {
        if (duration <= 0f)
        {
            currentHP = maxHP;
            UpdateHpUI();
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            currentHP = Mathf.RoundToInt(Mathf.Lerp(0, maxHP, p));
            UpdateHpUI();
            yield return null;
        }
        currentHP = maxHP;
        UpdateHpUI();
        yield break;
    }

}

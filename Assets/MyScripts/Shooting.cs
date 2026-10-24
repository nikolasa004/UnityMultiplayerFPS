using System;
using System.Collections;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class Shooting : NetworkBehaviour
{
    [Header("Aiming and damage")]
    [SerializeField] private Camera fps_cam;
    [SerializeField] private LayerMask mask = ~0;
    public WeaponManager weaponManager = new WeaponManager();

    [Header("Pistol effects - optional")]
    [SerializeField] private AudioSource weaponAudio;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip dryShotSound;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject groundImpactPrefab;
    [SerializeField] private GameObject playerImpactPrefab;
    [SerializeField] private Animator gunAnimator;
    [SerializeField] private float impactLifetime = 3f;

    [SyncVar] private int ammoInMagazine;
    [SyncVar] private bool reloading;

    private float nextServerShotTime;
    private float nextLocalShotTime;
    private PlayerManager player;
    private UI ui;

    public int AmmoInMagazine { get { return ammoInMagazine; } }
    public bool IsReloading { get { return reloading; } }

    private void Awake()
    {
        player = GetComponent<PlayerManager>();
    }

    public void SetLocalUI(UI localUI)
    {
        ui = localUI;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ammoInMagazine = Mathf.Max(1, weaponManager.magazineSize);
        reloading = false;
    }

    private void Update()
    {
        if (!isLocalPlayer || !isClient || player == null || player.Is_dead)
            return;

        if (Input.GetKeyDown(KeyCode.R))
            TryReload();

        if (Input.GetButtonDown("Fire1"))
            TryFire();
    }

    private void TryFire()
    {
        if (fps_cam == null || reloading)
            return;

        if (ammoInMagazine <= 0)
        {
            PlayClip(dryShotSound);
            return;
        }

        if (Time.time < nextLocalShotTime)
            return;

        nextLocalShotTime = Time.time + Mathf.Max(0.05f, weaponManager.fireInterval);
        CmdFire(fps_cam.transform.position, fps_cam.transform.forward);
    }

    [Command]
    private void CmdFire(Vector3 origin, Vector3 direction)
    {
        if (player == null || player.Is_dead || reloading || ammoInMagazine <= 0 ||
            Time.time < nextServerShotTime || direction.sqrMagnitude < 0.01f)
            return;

        if ((origin - transform.position).sqrMagnitude > 25f)
            return;

        ammoInMagazine--;
        nextServerShotTime = Time.time + Mathf.Max(0.05f, weaponManager.fireInterval);
        direction.Normalize();

        bool hitSomething = false;
        bool hitPlayer = false;
        bool killed = false;
        Vector3 point = origin + direction * weaponManager.range;
        Vector3 normal = -direction;

        RaycastHit[] allHits = Physics.RaycastAll(origin, direction, weaponManager.range, mask, QueryTriggerInteraction.Collide);
        Array.Sort(allHits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in allHits)
        {
            PlayerManager target = hit.collider.GetComponentInParent<PlayerManager>();

            if (target == player)
                continue;

            if (target != null)
            {
                if (target.Is_dead)
                    continue;

                bool headshot = target.IsHeadHit(hit.collider);
                bool bodyshot = target.IsBodyHit(hit.collider);
                if (!headshot && !bodyshot)
                    continue;

                hitSomething = true;
                hitPlayer = true;
                point = hit.point;
                normal = hit.normal;

                int damage = headshot ? target.current_health : Mathf.Max(1, weaponManager.damage);
                killed = target.ServerTakeDamage(damage);
                if (killed)
                    player.ServerAddKill();
                break;
            }


            if (hit.collider.isTrigger)
                continue;

            hitSomething = true;
            point = hit.point;
            normal = hit.normal;
            break;
        }

        RpcShotEffects(hitSomething, hitPlayer, point, normal);

        if (hitPlayer)
            TargetShotFeedback(true, killed);
    }

    [TargetRpc]
    private void TargetShotFeedback(bool hitPlayer, bool killed)
    {
        if (ui == null)
            return;

        if (hitPlayer) ui.ShowHit();
        if (killed) ui.ShowKill();
    }

    [ClientRpc]
    private void RpcShotEffects(bool hitSomething, bool hitPlayer, Vector3 point, Vector3 normal)
    {
        PlayClip(shootSound);

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (gunAnimator != null)
            gunAnimator.SetTrigger("Fire");

        if (hitSomething)
        {
            GameObject prefab = hitPlayer ? playerImpactPrefab : groundImpactPrefab;
            if (prefab != null)
            {
                GameObject fx = Instantiate(prefab, point, Quaternion.LookRotation(normal == Vector3.zero ? Vector3.up : normal));
                Destroy(fx, Mathf.Max(0.2f, impactLifetime));
            }
        }
    }

    private void TryReload()
    {
        if (reloading || ammoInMagazine >= weaponManager.magazineSize)
            return;

        CmdReload();
    }

    [Command]
    private void CmdReload()
    {
        if (player == null || player.Is_dead || reloading || ammoInMagazine >= weaponManager.magazineSize)
            return;

        reloading = true;
        RpcReloadEffects();
        StartCoroutine(ServerReload());
    }

    [ClientRpc]
    private void RpcReloadEffects()
    {
        PlayClip(reloadSound);
        if (gunAnimator != null)
            gunAnimator.SetTrigger("Reload");
    }

    [Server]
    private IEnumerator ServerReload()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, weaponManager.reloadTime));

        if (player != null && !player.Is_dead)
        {
            ammoInMagazine = weaponManager.magazineSize;
        }
        reloading = false;
    }

    private void PlayClip(AudioClip clip)
    {
        if (weaponAudio != null && clip != null)
            weaponAudio.PlayOneShot(clip);
    }
}

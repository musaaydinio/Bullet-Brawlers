using UnityEngine;
using Unity.Netcode;

public class PlayerShooting : NetworkBehaviour
{
    [Header("Ateþ Ayarlarý")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;

    [Header("Animasyon")]
    [SerializeField] private string shootTriggerName = "Shoot";

    private Animator animator;
    private float nextFireTime = 0f;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if(!IsOwner) return;

        if(Input.GetMouseButtonDown(0))
        {
            if(Time.time >= nextFireTime)
            {
                nextFireTime = Time.deltaTime + fireRate;

                PlayShootAnmiation();

                ShootServerRpc();
            }                   
        }
    }

    private void PlayShootAnmiation()
    {
        if(animator !=null && !string.IsNullOrEmpty(shootTriggerName))
        {
            animator.SetTrigger(shootTriggerName);
        }  
     }

    [ServerRpc]
    private void ShootServerRpc()
    {
        if(bulletPrefab ==null || firePoint ==null) return;

        GameObject bullletInstace=Instantiate(bulletPrefab,firePoint.position,firePoint .rotation);

        NetworkObject bulletNetworkObject=bullletInstace.GetComponent<NetworkObject>();
        if (bulletNetworkObject != null)
        {
            bulletNetworkObject.Spawn();
        }
    }
}

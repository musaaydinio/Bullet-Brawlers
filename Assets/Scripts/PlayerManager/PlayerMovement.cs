using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController controller;
    private Animator animator;

    [Header("Hareket Ayarlarý")]
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;
    private Vector3 velocity;
    public float mouseSensitivity = 100f;

    [Header("UI (Arayüz) Ayarlarý")]
    public TextMeshProUGUI mermiText;

   [Header("Ayak Sesi Ayarlarý")]
    public AudioClip[] adimSesleri; 
    public float adimAraligi = 0.4f; 
    private float siradakiAdimZamani;
    private AudioSource ayakSesKaynagi;

    private GunSetting currentGun;
    private float siradakiAtesZamani = 0f;

    private CameraController camControl;

    // Að üzerindeki diðer istemcilere (Cliente) oyuncunun hýz ve zemin bilgisini aktarmak için kullanýlan deðiþkenler.
    public NetworkVariable<float> networkedSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> networkedGrounded = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        ayakSesKaynagi = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
          
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
     
        if (currentGun == null)
        {
            currentGun = GetComponentInChildren<GunSetting>(true);
        }

        Camera myCam = GetComponentInChildren<Camera>();
        AudioListener myListener = GetComponentInChildren<AudioListener>();

        // Yetki Kontrolü:Bu obje yerel oyuncuya (Local Player) mý ait?
        if (IsOwner)
        {
            // Sadece kendi karakterimin kamerasýný ve fizik kontrollerini açýyorum.
            if (controller != null) controller.enabled = true;
            if (myCam != null) myCam.enabled = true;
            if (myListener != null) myListener.enabled = true;

            camControl = GetComponentInChildren<CameraController>();
            if (camControl != null) camControl.target = this.transform;

            if (currentGun != null)
            {
                currentGun.mevcutMermi = currentGun.sarjorKapasitesi;
                MermiUIGuncelle();
            }
            // Kendi ayak sesimi uzamsal deðil, direkt 2D duyacak þekilde ayarlýyorum.
            if (ayakSesKaynagi != null)
            {
                ayakSesKaynagi.spatialBlend = 0f;
            }
        }
        else
        {
            // Aðdaki diðer oyuncularýn kontrollerini kapatýyorum ki onlarýn da karakterini hareket etmesin.
            if (controller != null) controller.enabled = false;
            if (myCam != null) myCam.enabled = false;
            if (myListener != null) myListener.enabled = false;

            // Diðer oyuncularýn ayak seslerini mesafeye göre (3D) ayarlýyorum.
            if (ayakSesKaynagi != null)
            {
                ayakSesKaynagi.spatialBlend = 1f; 
                ayakSesKaynagi.minDistance = 2f;  
                ayakSesKaynagi.maxDistance = 40f; 
                ayakSesKaynagi.rolloffMode = AudioRolloffMode.Linear;
            }
        }
    }

    private void Update()
    {
        //Animasyon verilerini belirliyorum.Obje benimse fizik motorundan, deðilse að deðiþkeninden çekiliyor.
        float aktifHiz =IsOwner ? new Vector3(Input.GetAxis("Horizontal"),0,
            Input.GetAxis("Vertical")).magnitude : networkedSpeed.Value;

        bool yereBasiyormu=IsOwner ? controller.isGrounded : networkedGrounded.Value;

        if(yereBasiyormu && aktifHiz > 0.1f)
        {
            if(Time.time >= siradakiAdimZamani)
            {
                if(adimSesleri.Length > 0)
                {
                    int rastgelelSes=Random.Range(0,adimSesleri.Length);
                    ayakSesKaynagi.PlayOneShot(adimSesleri[rastgelelSes]);
                }
                siradakiAdimZamani = Time.time + adimAraligi;
            }
        }
        // Karakter benim deðilse sadece aðdan gelen animasyonlarý güncelleyip fonksiyondan çýkýyorum.
        if (!IsOwner)
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", networkedSpeed.Value);
                animator.SetBool("isGrounded", networkedGrounded.Value);
            }
            return;
        }

        MovePlayer();
        HandleShooting();
        HandleReloading();
    }

    private void MovePlayer()
    {
        if (controller == null || !controller.enabled) return;

        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float currentMoveMagnitude = move.magnitude;

        // Hareket verilerimi aðdaki diðer oyuncularýn da görmesi için sunucuya iletiyorum.
        UpdateAnimatorServerRpc(currentMoveMagnitude, isGrounded);

        if (animator != null)
        {
            animator.SetFloat("Speed", currentMoveMagnitude);
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    [ServerRpc]
    private void UpdateAnimatorServerRpc(float newSpeed, bool newGrounded)
    {
        networkedSpeed.Value = newSpeed;
        networkedGrounded.Value = newGrounded;
    }

    private void HandleReloading()
    {
        if (currentGun == null) return;

        // R'ye basýlýrsa, yedek þarjör varsa ve mermi zaten full deðilse yenile
        if (Input.GetKeyDown(KeyCode.R) && currentGun.kalanSarjorHakki > 0 && currentGun.mevcutMermi < currentGun.sarjorKapasitesi)
        {
            currentGun.mevcutMermi = currentGun.sarjorKapasitesi; // Mermiyi doldur
            currentGun.kalanSarjorHakki--; // Yedek hakkýný 1 azalt
            MermiUIGuncelle(); // Ekranda göster
            // Yerel oyuncu reload sesini çalar ve aðdakilere bildirir.
            currentGun.ReloadSesiCal();
            PlaySoundServerRpc(false);
        }
    }
    private void HandleShooting()
    {
        currentGun = GetComponentInChildren<GunSetting>(false);
        if (currentGun == null) return;

        bool tetigeCekildi = currentGun.otomatikMi ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (tetigeCekildi && Time.time >= siradakiAtesZamani)
        {
            siradakiAtesZamani = Time.time + currentGun.atesAraligi;

            // MERMÝ KONTROLÜ BURADA YAPILIYOR
            if (currentGun.mevcutMermi > 0)
            {
                currentGun.mevcutMermi--; // Mermiyi eksilt
                MermiUIGuncelle(); // UI'ý güncelle

                if (animator != null) animator.SetTrigger("Shoot");

                // Yerel oyuncu ateþ sesini çalar ve aðdakilere bildirir.
                currentGun.AtesEtSesiCal();
                PlaySoundServerRpc(true);

                if (currentGun.firePoint != null && currentGun.bulletPrefab != null)
                {
                    // Mermi üretimini hileye karþý tamamen sunucu tarafýna býraktým.
                    ShootServerRpc(
                        currentGun.firePoint.position,
                        currentGun.firePoint.rotation,
                        currentGun.silahHasari,
                        currentGun.mermiHizi
                    );
                }
            }
            else
            {
                Debug.Log("Mermi Bitti! R tuþuna bas!");
            }
        }
    }

    private void MermiUIGuncelle()
    {
        if (mermiText != null && currentGun != null)
        {           
            mermiText.text = $"{currentGun.mevcutMermi} / {currentGun.kalanSarjorHakki}";
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPosition, Quaternion spawnRotation, int damage, float speed, ServerRpcParams rpcParams = default)
    {
        //Ýstemcinin gönderdiði ID'ye güvenmek yerine, isteði atan gerçek SenderClientId deðerini alýyorum.
        ulong gercekSikanID = rpcParams.Receive.SenderClientId;

        if (currentGun == null)
        {
            currentGun = GetComponentInChildren<GunSetting>(true);
        }

        if (currentGun == null || currentGun.bulletPrefab == null) return;

        GameObject bullet = Instantiate(currentGun.bulletPrefab, spawnPosition, spawnRotation);

        if (bullet.TryGetComponent<Bullet>(out var bulletScript))
        {
            // Merminin sahibini artýk sýfýr (0) deðil, tetiði çekenin KESÝN KÝMLÝÐÝ yapýyoruz.
            bulletScript.SetOwner(gercekSikanID);
            bulletScript.speed = speed;
            bulletScript.damageAmount = damage;
        }

        if (bullet.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();

            if (bullet.TryGetComponent<Bullet>(out var bScript))
            {
                bScript.InitializeBulletClientRpc(speed, damage);
            }
        }
    }
    [ServerRpc]
    private void PlaySoundServerRpc(bool isShooting)
    {
        PlaySoundClientRpc(isShooting);
    }

    [ClientRpc]
    private void PlaySoundClientRpc(bool isShooting)
    {
        // Sesi gönderen oyuncunun kendisi hariç diðer tüm oyuncular bu sesi kendi ekranlarýnda duyar.
        if (!IsOwner)
        {
            GunSetting aktifSilah = null;

            // Karakterin üzerindeki tüm silah bileþenlerini (pasif olanlar dahil) tarýyoruz.
            foreach (var silah in GetComponentsInChildren<GunSetting>(true))
            {
                // Sadece þu an elde açýk (aktif) olan silahý seçiyoruz.
                if (silah.gameObject.activeSelf)
                {
                    aktifSilah = silah;
                    break;
                }
            }

            if (aktifSilah != null)
            {
                if (isShooting) aktifSilah.AtesEtSesiCal();
                else aktifSilah.ReloadSesiCal();
            }
        }
    }
}

using TMPro;
using Unity.Netcode;
using UnityEngine;

// Prosedürel omurga eðilmesi için kullanýlacak rotasyon eksen tanýmý
public enum KemikEkseni { X, Y, Z }

// Karakter hareketini, FPS kamerasýný, prosedürel omurga rotasyonunu ve að üzerinden ateþ/ses senkronizasyonunu yöneten ana sýnýf
public class PlayerMovement : NetworkBehaviour
{
    private CharacterController controller;
    private Animator animator;

    [Header("Hareket Ayarlarý")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;
    private Vector3 velocity;

    [Header("Gövde / Silah Eðilme Ayarlarý")]
    public Transform spineKemigi;
    public KemikEkseni eglimeEkseni = KemikEkseni.X;
    public bool aciyiTersCevir = false; // Kamera bakýþ açýsýna göre kemik tersine eðiliyorsa düzeltme bayraðý

    [Header("UI (Arayüz) Ayarlarý")]
    public TextMeshProUGUI mermiText;

    [Header("Ses Ayarlarý")]
    public AudioClip[] adimSesleri;
    public AudioClip olumSesi;
    public float adimAraligi = 0.4f;
    private float siradakiAdimZamani;
    private AudioSource ayakSesKaynagi;

    private GunSetting currentGun;
    private float siradakiAtesZamani = 0f;

    private CameraController camControl;
    private Camera myCam;

    // Að üzerindeki diðer istemcilerin (Clients) bu oyuncunun hýzýný okuyarak animasyon yürütmesini saðlayan senkronize deðiþken
    public NetworkVariable<float> networkedSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Oyuncunun zemine basýp basmadýðýný tüm aða duyuran senkronize deðiþken
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

    // Að nesnesi sahnede aktifleþtiðinde yerel oyuncu (IsOwner) ve uzak oyuncu (Remote Client) ayrýmýnýn yapýldýðý alan
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (controller == null) controller = GetComponent<CharacterController>();
        if (currentGun == null) currentGun = GetComponentInChildren<GunSetting>(true);

        myCam = GetComponentInChildren<Camera>();
        AudioListener myListener = GetComponentInChildren<AudioListener>();

        if (IsOwner)
        {
            // Kendi karakterimiz için fizik, kamera ve dinleyici bileþenlerini aktif ediyoruz
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

            // Kendi ayak seslerimizi 2D (Stereo) olarak duyuyoruz
            if (ayakSesKaynagi != null) ayakSesKaynagi.spatialBlend = 0f;
        }
        else
        {
            // Diðer oyuncularýn kameralarýný ve dinleyicilerini kapatarak çakýþmayý önlüyoruz
            if (controller != null) controller.enabled = false;
            if (myCam != null) myCam.enabled = false;
            if (myListener != null) myListener.enabled = false;

            // Diðer oyuncularýn ayak seslerini 3D uzamsal (Spatial) olarak mesafeye göre simüle ediyoruz
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
        // Hareket ve zemin durumunu yerel girdilerden veya aðdan gelen NetworkVariable deðerlerinden çekiyoruz
        float aktifHiz = IsOwner ? new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).magnitude : networkedSpeed.Value;
        bool yereBasiyormu = IsOwner ? controller.isGrounded : networkedGrounded.Value;

        // Ayak sesi zamanlamasýnýn adým aralýðýna göre oynatýlmasý
        if (yereBasiyormu && aktifHiz > 0.1f)
        {
            if (Time.time >= siradakiAdimZamani)
            {
                if (adimSesleri.Length > 0)
                {
                    int rastgeleSes = Random.Range(0, adimSesleri.Length);
                    ayakSesKaynagi.PlayOneShot(adimSesleri[rastgeleSes]);
                }
                float tempoluAdim = Input.GetKey(KeyCode.LeftShift) ? adimAraligi * 0.7f : adimAraligi;
                siradakiAdimZamani = Time.time + tempoluAdim;
            }
        }

        // Eðer bu karakter bize ait deðilse yerel kontrol mantýðýný çalýþtýrmayýp sadece aðdan gelen animasyonu güncelliyoruz
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

    // Animator pose hesaplamasýndan hemen sonra çalýþarak omurgayý dikey bakýþ açýsýna göre eðen prosedürel rotasyon
    private void LateUpdate()
    {
        if (!IsOwner || spineKemigi == null || myCam == null) return;

        // 1. Kameranýn dikey açýsýný alýyoruz (-180, 180 aralýðýna çeviriyoruz)
        float pitch = myCam.transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        if (aciyiTersCevir) pitch = -pitch;

        // 2. Seçilen eksene göre rotasyon açýsýný hazýrlýyoruz
        Vector3 eglimeVektoru = Vector3.zero;
        switch (eglimeEkseni)
        {
            case KemikEkseni.X: eglimeVektoru = new Vector3(pitch, 0f, 0f); break;
            case KemikEkseni.Y: eglimeVektoru = new Vector3(0f, pitch, 0f); break;
            case KemikEkseni.Z: eglimeVektoru = new Vector3(0f, 0f, pitch); break;
        }

        // 3. Animator'ün o anki pozisyonunun üzerine anlýk olarak ekliyoruz (Birikme yapmaz)
        spineKemigi.localRotation *= Quaternion.Euler(eglimeVektoru);
    }

    // Karakter hareketi, yerçekimi, zýplama ve fare bakýþ kontrolleri
    private void MovePlayer()
    {
        if (controller == null || !controller.enabled) return;

        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Shift ile Koþma Mantýðý
        float mevcutHiz = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * mevcutHiz * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        float kayitliSens = PlayerPrefs.GetFloat("MouseSensitivityPref", 2f);
        float dinamikMouseSensitivity = kayitliSens * 50f;

        float mouseX = Input.GetAxis("Mouse X") * dinamikMouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float currentMoveMagnitude = move.magnitude;

        // Hýz ve zemin durumunu sunucu üzerinden aðdaki herkese senkronize ediyoruz
        UpdateAnimatorServerRpc(currentMoveMagnitude, isGrounded);

        if (animator != null)
        {
            animator.SetFloat("Speed", currentMoveMagnitude);
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    // Ýstemciden gelen hareket verisini sunucu üzerindeki NetworkVariable deðerlerine yazan RPC
    [ServerRpc]
    private void UpdateAnimatorServerRpc(float newSpeed, bool newGrounded)
    {
        networkedSpeed.Value = newSpeed;
        networkedGrounded.Value = newGrounded;
    }

    // Þarjör deðiþtirme mantýðý ve að genelinde ses senkronizasyonu
    private void HandleReloading()
    {
        if (currentGun == null) return;

        if (Input.GetKeyDown(KeyCode.R) && currentGun.kalanSarjorHakki > 0 && currentGun.mevcutMermi < currentGun.sarjorKapasitesi)
        {
            currentGun.mevcutMermi = currentGun.sarjorKapasitesi;
            currentGun.kalanSarjorHakki--;
            MermiUIGuncelle();
            currentGun.ReloadSesiCal();
            PlaySoundServerRpc(false);
        }
    }

    // Ateþ etme mekanizmasý (Otomatik / Yarý Otomatik), zamanlama ve Mermi türetme isteði
    private void HandleShooting()
    {
        currentGun = GetComponentInChildren<GunSetting>(false);
        if (currentGun == null) return;

        bool tetigeCekildi = currentGun.otomatikMi ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

        if (tetigeCekildi && Time.time >= siradakiAtesZamani)
        {
            siradakiAtesZamani = Time.time + currentGun.atesAraligi;

            if (currentGun.mevcutMermi > 0)
            {
                currentGun.mevcutMermi--;
                MermiUIGuncelle();

                if (animator != null) animator.SetTrigger("Shoot");

                currentGun.AtesEtSesiCal();
                currentGun.AtesEfektiCal();
                PlaySoundServerRpc(true);

                // Mermi nesnesinin doðrudan sunucuda türetilmesi (Server-Authoritative Projectile Spawning) için istek atýlýr
                if (currentGun.firePoint != null && currentGun.bulletPrefab != null)
                {
                    ShootServerRpc(
                        currentGun.firePoint.position,
                        currentGun.firePoint.rotation,
                        currentGun.silahHasari,
                        currentGun.mermiHizi
                    );
                }
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

    public void OlumSesiCalLokal()
    {
        if (!IsOwner) return;

        if (olumSesi != null && ayakSesKaynagi != null)
        {
            ayakSesKaynagi.PlayOneShot(olumSesi);
        }
    }

    // Mermiyi sunucuda türetip NetworkObject.Spawn() ile tüm aðda eþ zamanlý oluþturan kritik RPC
    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPosition, Quaternion spawnRotation, int damage, float speed, ServerRpcParams rpcParams = default)
    {
        ulong gercekSikanID = rpcParams.Receive.SenderClientId;

        if (currentGun == null) currentGun = GetComponentInChildren<GunSetting>(true);
        if (currentGun == null || currentGun.bulletPrefab == null) return;

        GameObject bullet = Instantiate(currentGun.bulletPrefab, spawnPosition, spawnRotation);

        if (bullet.TryGetComponent<Bullet>(out var bulletScript))
        {
            bulletScript.SetOwner(gercekSikanID);
            bulletScript.speed = speed;
            bulletScript.damageAmount = damage;
        }

        // Mermiyi að nesnesi olarak aða kaydediyoruz
        if (bullet.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();

            if (bullet.TryGetComponent<Bullet>(out var bScript))
            {
                bScript.InitializeBulletClientRpc(speed, damage);
            }
        }
    }

    // Sesi sunucu üzerinden diðer tüm istemcilere daðýtma yöntemi
    [ServerRpc]
    private void PlaySoundServerRpc(bool isShooting)
    {
        PlaySoundClientRpc(isShooting);
    }

    // Ateþ ve þarjör deðiþtirme ses/efektlerini karakter sahibi dýþýndaki diðer istemcilerde tetikleyen RPC
    [ClientRpc]
    private void PlaySoundClientRpc(bool isShooting)
    {
        if (!IsOwner)
        {
            GunSetting aktifSilah = null;

            foreach (var silah in GetComponentsInChildren<GunSetting>(true))
            {
                if (silah.gameObject.activeSelf)
                {
                    aktifSilah = silah;
                    break;
                }
            }

            if (aktifSilah != null)
            {
                if (isShooting)
                {
                    aktifSilah.AtesEtSesiCal();
                    aktifSilah.AtesEfektiCal();
                }
                else aktifSilah.ReloadSesiCal();
            }
        }
    }

    // Silah deðiþtirme isteðini sunucuya ileten RPC
    [ServerRpc(RequireOwnership = false)]
    public void SilahDegistirServerRpc(string yeniSilahAdi)
    {
        SilahDegistirClientRpc(yeniSilahAdi);
    }

    // Tüm istemcilerde oyuncunun aktif görsel silah modelini senkronize olarak deðiþtiren RPC
    [ClientRpc]
    private void SilahDegistirClientRpc(string yeniSilahAdi)
    {
        GunSetting[] tumSilahlar = GetComponentsInChildren<GunSetting>(true);
        foreach (var silah in tumSilahlar)
        {
            bool secilenMi = (silah.gameObject.name == yeniSilahAdi);
            silah.gameObject.SetActive(secilenMi);

            if (secilenMi)
            {
                silah.MermileriSifirla();              
            }
        }
    }
}
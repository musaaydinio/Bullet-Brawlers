using UnityEngine;

// Silahýn atýþ hýzýný, mermi kapasitesini, hasarýný ve ses efektlerinin að üzerindeki senkronizasyonunu yönetiyoruz.

public class GunSetting : MonoBehaviour
{
    [Header("Bu Silaha Özel Özellikler")]
    public float atesAraligi = 0.1f;
    public bool otomatikMi = false;
    public int silahHasari = 20;
    public float mermiHizi = 40f;
 
  [Header("Mermi ve Þarjör Ayarlarý")]
    public int sarjorKapasitesi = 30;
    public int mevcutMermi;
    public int kalanSarjorHakki = 4;

    [Header("Mermi ve Namlu")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public GameObject muzzleFlashPrefab;

    [Header("SES AYARLARI")]
    public AudioClip atesSesi;
    public AudioClip reloadSesi;
    private AudioSource audioSource;

    private void Awake()
    {
        mevcutMermi = sarjorKapasitesi;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 40f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }


    public void MermileriSifirla()
    {
        // Karakter yeniden doðduðunda mermi ve þarjör haklarýný baþlangýç deðerine getiriyoruz.
        mevcutMermi = sarjorKapasitesi;
        kalanSarjorHakki = 4;
    }

    public void AtesEtSesiCal()
    {
        if (atesSesi != null && audioSource != null)
        {
            audioSource.PlayOneShot(atesSesi);
        }
    }

    public void ReloadSesiCal()
    {
        if (reloadSesi != null && audioSource != null)
        {
            audioSource.PlayOneShot(reloadSesi);
        }
    }

    public void AtesEfektiCal()
    {
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            // Efekti firePoint noktasýnda oluþturup silaha baðlýyoruz (parent yapýyoruz)
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);

            Destroy(flash, 0.2f);
        }
    }
}
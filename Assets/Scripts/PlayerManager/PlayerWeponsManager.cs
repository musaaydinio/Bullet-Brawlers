using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;

public class PlayerWeponsManager : NetworkBehaviour
{
    public static PlayerWeponsManager localInstance;

    [System.Serializable]
    public class WeaponModel
    {
        public string weaponName;
        public GameObject weaponObject;
    }

    [Header("Karakterin Elindeki Silahlar")]
    public List<WeaponModel> eldekiSilahlar = new List<WeaponModel>();

    // Karakterin elindeki aktif silahýn bilgisini tüm aðla paylaþmak için kullanýlan deðiþken.
    public NetworkVariable<FixedString32Bytes> aktifSilah = new NetworkVariable<FixedString32Bytes>("",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        // Yalnýzca karakterin asýl sahibi kendi yerel hafýzasýndan seçili silahý çeker.
        if (IsOwner)
        {
            localInstance = this;

            // Envanterden kuþanýlan silah bilgisi çekilir
            string benimSilahim = PlayerPrefs.GetString("KusanilanSilah", "Pistol");

            // Seçilen silahý tüm oyuncularýn görmesi için sunucuya bildirir.
            SetWeaponServerRpc(benimSilahim);
        }
       
        aktifSilah.OnValueChanged += (eskiSilah, yeniSilah) =>
        {
            SilahiEkrandaGoster(yeniSilah.ToString());
        };

        // Oyuna sonradan baðlanan kiþilerin, diðer oyuncularýn elindeki mevcut silahý hemen görebilmesi için kontrol.
        if (!string.IsNullOrEmpty(aktifSilah.Value.ToString()))
        {
            SilahiEkrandaGoster(aktifSilah.Value.ToString());
        }
    }

    [ServerRpc]
    private void SetWeaponServerRpc(string targetWeaponName)
    {
        // Ýletilen silah ismini að deðiþkenine yazar, böylece deðiþiklik tüm oyunculara daðýtýlýr.
        aktifSilah.Value = targetWeaponName;
    }

    
    private void SilahiEkrandaGoster(string targetWeaponName)
    {
        // Karakterin elindeki tüm silah modelleri taranýr, sadece aktif olan silahýn modeli açýlýr, diðerleri gizlenir.
        foreach (var weapon in eldekiSilahlar)
        {
            if (weapon.weaponName == targetWeaponName)
            {
                weapon.weaponObject.SetActive(true); 
            }
            else
            {
                weapon.weaponObject.SetActive(false); 
            }
        }
    }
}

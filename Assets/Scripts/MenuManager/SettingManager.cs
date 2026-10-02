using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingManager : MonoBehaviour
{
    [Header("UI Elementleri (Sürükle Býrak)")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown fpsDropdown;
    public Slider oyunIciSesSlider;
    public Slider menuSesSlider;
    public Slider mouseSensitivitySlider;

    private void Start()
    {        
        LoadSettings();
    }

    public void SetResolution(int index)
    {
        // Seçilen dropdown indeksine göre ekran çözünürlüðü ayarlanýr.
        switch (index)
        {
            case 0: Screen.SetResolution(1280, 720, Screen.fullScreen); break;  
            case 1: Screen.SetResolution(1600, 900, Screen.fullScreen); break;   
            case 2: Screen.SetResolution(1920, 1080, Screen.fullScreen); break; 
            case 3: Screen.SetResolution(2560, 1440, Screen.fullScreen); break; 
            case 4: Screen.SetResolution(3840, 2160, Screen.fullScreen); break; 
        }
        // Seçilen çözünürlük indeksi hafýzaya kaydedilir
        PlayerPrefs.SetInt("ResolutionPref", index);
        PlayerPrefs.Save();
    }

    public void SetFPS(int index)
    {
        // Hedef kare hýzý sýnýrlandýrmasý uygulanýr.
        switch (index)
        {
            case 0: Application.targetFrameRate = 60; break;
            case 1: Application.targetFrameRate = 144; break;
            case 2: Application.targetFrameRate = -1; break; 
        }
        // Kare hýzý tercihi hafýzaya kaydedilir.
        PlayerPrefs.SetInt("FPSPref", index);
        PlayerPrefs.Save();
    }

    public void SetOyunIciSes(float volume)
    {
        // Genel ses dinleyicisinin (AudioListener) ses seviyesi güncellenir.
        AudioListener.volume = volume;

        PlayerPrefs.SetFloat("OyunIciSesPref", volume);
        PlayerPrefs.Save();
    }

    public void SetMenuSes(float volume)
    {
        PlayerPrefs.SetFloat("MenuSesPref", volume);
        PlayerPrefs.Save();

        // Arka plan müzik yöneticisi sahnede mevcutsa menü müzik seviyesi anýnda güncellenir.
        if (BackGroundMusicManager.Instance != null)
        {
            BackGroundMusicManager.Instance.GetComponent<AudioSource>().volume = volume;
        }
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat("MouseSensitivityPref", sensitivity);
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan Çýkýlýyor...");
        Application.Quit(); 
    }

    private void LoadSettings()
    {
        if (resolutionDropdown != null)
        {
            int savedRes = PlayerPrefs.GetInt("ResolutionPref", 2);
            resolutionDropdown.value = savedRes;
            SetResolution(savedRes);
        }
        
        if (fpsDropdown != null)
        {
            int savedFPS = PlayerPrefs.GetInt("FPSPref", 0);
            fpsDropdown.value = savedFPS;
            SetFPS(savedFPS);
        }

        if (oyunIciSesSlider != null)
        {
            float savedOyunSesi = PlayerPrefs.GetFloat("OyunIciSesPref", 1f);
            oyunIciSesSlider.value = savedOyunSesi;
            SetOyunIciSes(savedOyunSesi);
        }

        if (menuSesSlider != null)
        {
            float savedMenuSesi = PlayerPrefs.GetFloat("MenuSesPref", 1f);
            menuSesSlider.value = savedMenuSesi;
            SetMenuSes(savedMenuSesi);
        }

        if (mouseSensitivitySlider != null)
        {
            float savedSens = PlayerPrefs.GetFloat("MouseSensitivityPref", 2f);
            mouseSensitivitySlider.value = savedSens;
            SetMouseSensitivity(savedSens);
        }
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackGroundMusicManager : MonoBehaviour
{
    public static BackGroundMusicManager Instance;

    private AudioSource audioSource;

    private void Awake()
    {
        // Singleton yapýsýný kuruyoruz. Sahnede birden fazla müzik yöneticisi olmasýný engelliyor ve objenin silinmemesini saðlýyoruz.
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Oyun ilk açýldýðýnda oyuncunun daha önceden kaydettiði ses seviyesini ve sessize alma ayarlarýný çekip uyguluyoruz.
        audioSource.volume = PlayerPrefs.GetFloat("MenuSesPref", 1f);
        audioSource.mute = (PlayerPrefs.GetInt("MusicMutedPref", 0) == 1);
    }

    private void Update()
    {
        // Oyuncu maça girdiðinde menü müziðini tamamen durduruyoruz.
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            if (audioSource.isPlaying) audioSource.Stop();
        }
        
        else
        {
            // Maç bittiðinde veya menüye dönüldüðünde müziði kaldýðý yerden tekrar baþlatýyoruz.
            if (!audioSource.isPlaying) audioSource.Play();
        }
    }

    public void ToggleMusic()
    {
        if (audioSource != null)
        {
            // Müziði tamamen susturma veya açma iþlemini gerçekleþtirip bu tercihi bir sonraki giriþ için hafýzaya kaydediyoruz.
            audioSource.mute = !audioSource.mute; 

            PlayerPrefs.SetInt("MusicMutedPref", audioSource.mute ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Yerel sunucumuzla haberleþirken SSL sertifikasý hatalarýna takýlmamak için
// güvenlik doðrulamasýný geçici olarak atlýyoruz.
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}
public class AuthManager : MonoBehaviour
{
    [Header("Paneller (Geçiþ Ýçin)")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Login UI (Giriþ)")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;
    public TextMeshProUGUI loginFeedbackText;

    [Header("Register UI (Kayýt)")]
    public TMP_InputField registerUsername;
    public TMP_InputField registerPassword;
    public TextMeshProUGUI registerFeedbackText;

    private readonly string baseUrl = "https://192.168.1.103:7023/api/auth";

    private void Update()
    {
        // Kullanýcý deneyimini hýzlandýrmak için Enter tuþuna basýldýðýnda o an aktif olan formun butonunu tetikliyoruz.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (loginPanel.activeSelf)
            {
                OnLoginButtonClicked();
            }
            else if (registerPanel.activeSelf)
            {
                OnRegisterButtonClicked();
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Arayüzde Tab tuþuyla kullanýcý adý ve þifre kutularý arasýnda hýzlý geçiþ saðlýyoruz.
            if (loginPanel.activeSelf)
            {
                if (loginUsername.isFocused) loginPassword.Select();
                else if (loginPassword.isFocused) loginUsername.Select();
            }
            else if (registerPanel.activeSelf)
            {
                if (registerUsername.isFocused) registerPassword.Select();
                else if (registerPassword.isFocused) registerUsername.Select();
            }
        }
    }

    public void GoToRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        registerFeedbackText.text = "";
    }

    public void GoToLoginPanel()
    {
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
        loginFeedbackText.text = "";
    }
    public void OnLoginButtonClicked()
    {
        loginFeedbackText.color = Color.yellow;
        loginFeedbackText.text = "Sunucuya baðlanýlýyor...";
        StartCoroutine(LoginCoroutine());
    }

    public void OnRegisterButtonClicked()
    {
        registerFeedbackText.color= Color.yellow;
        registerFeedbackText.text = "Kayýt yapýlýyor...";
        StartCoroutine(RegisterCoroutine());
    }

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
        Debug.Log("Link açýlýyor: " + url);
    }

    private IEnumerator LoginCoroutine()
    {
        UserDto loginData = new UserDto
        {
            Username = loginUsername.text,
            Password = loginPassword.text
        };

        // Kullanýcýdan aldýðýmýz verileri API tarafýna gönderebilmek için JSON formatýna çeviriyoruz.
        string jsonData = JsonUtility.ToJson(loginData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/login", "POST"))
        {
            byte[] bodyraw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyraw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            request.certificateHandler = new BypassCertificate();

            yield return request.SendWebRequest();

            //Hata var mý kontrol et diyoruz.
            if (request.result != UnityWebRequest.Result.Success)
            {
                loginFeedbackText.color = Color.red;
                loginFeedbackText.text = "Giriþ Baþarýsýz: Þifre veya Kullanýcý Adý hatalý!";
            }
            else
            {
                loginFeedbackText.color = Color.green;
                loginFeedbackText.text = "Giriþ Baþarýlý! Yönlendiriliyorsunuz...";
                //Gelen JSON'ýn içindeki Token'ý okuyoruz.
                string responseText = request.downloadHandler.text;               
                TokenResponse tokendata = JsonUtility.FromJson<TokenResponse>(responseText);
                // Tokený SessionManager kaydediyoruz.
                SessionManager.Token = tokendata.token;
                //Oyuncuyu Ana Menü sahnesine ýþýnlýyoruz.
                PlayerPrefs.DeleteKey("KusanilanSilah");

                Invoke("LoadMainMenu", 1.5f);
            }
        }
    }
    private IEnumerator RegisterCoroutine()
    {
        UserDto registerData = new UserDto
        {
            Username=registerUsername.text,
            Password=registerPassword.text
        };
        string jsonData= JsonUtility.ToJson(registerData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/register", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            request.certificateHandler = new BypassCertificate();
            request.disposeCertificateHandlerOnDispose=true;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                registerFeedbackText.color = Color.red;
                registerFeedbackText.text = "Kayýt Baþarýsýz: Kullanýcý adý alýnmýþ olabilir!";
            }
            else
            {
                registerFeedbackText.color = Color.green;
                registerFeedbackText.text = "Kayýt Baþarýlý! Giriþ ekranýna dönülüyor...";

                PlayerPrefs.DeleteKey("KusanilanSilah");
                // Kayýt iþlemi baþarýyla bittikten sonra oyuncuyu direkt olarak giriþ paneline yönlendiriyoruz.
                Invoke("GoToLoginPanel", 2f);
            }
        }
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
[System.Serializable]
public class UserDto
{
    public string Username;
    public string Password;
}

[System.Serializable]
public class TokenResponse
{
    public string token;
}


using UnityEngine;

public static class SessionManager
{
    // Token'ý Play Mode kapansa bile hafýzada tutacak kalýcý yapý
    public static string Token
    {
        get { return PlayerPrefs.GetString("UserToken", ""); }
        set { PlayerPrefs.SetString("UserToken", value); PlayerPrefs.Save(); }
    }
}
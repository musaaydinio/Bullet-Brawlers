using UnityEngine;

public static class JsonHelper
{
    // API'den gelen düz listeleri (array) Unity'nin anlayacaðý hale getiren fonksiyon
    public static T[] FromJson<T>(string json)
    {
        string wrapper = "{ \"Items\": " + json + "}";
        Wrapper<T> wrapperObj = JsonUtility.FromJson<Wrapper<T>>(wrapper);
        return wrapperObj.Items;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}
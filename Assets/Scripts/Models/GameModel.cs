using UnityEngine;
using System.Collections.Generic;

// ASP.NET Core Web API üzerinden gelen JSON formatýndaki verileri Unity içerisinde
// C# objesi olarak okuyup iþleyebilmek için kullandýðýmýz standart model (DTO) yapýlarýmýz.
[System.Serializable]
public class WeaponDto
{
    public int id;
    public string name;   
    public int price;    
    public int damage;  
}
[System.Serializable]

public class WeaponListResponse
{
    public List<WeaponDto> Weapons;
}

[System.Serializable]
public class UserWeaponDto
{
    public int Id;
    public int UserId;
    public int WeaponId;
    public WeaponDto Weapon;
}

[System.Serializable]
public class BuyWeaponRequestDto
{
    public int weaponId;
}

[System.Serializable]
public class UserInfoDto
{
    public string userName;
    public int coins;
}

[System.Serializable]
public class AddCoinRequestDto
{
    public int EarnedCoins;
}

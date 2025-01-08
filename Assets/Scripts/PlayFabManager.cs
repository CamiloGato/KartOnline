using System.Globalization;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class PlayFabManager : MonoBehaviour
{
    void Start()
    {
        AuthenticatePlayer();
    }

    // 1. Autenticación y Gestión de Jugadores
    private void AuthenticatePlayer()
    {
        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Login exitoso con PlayFab");

        // Cargar datos del jugador
        LoadPlayerData();
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError($"Error al iniciar sesión: {error.GenerateErrorReport()}");
    }

    // 2. Gestión de Datos del Jugador
    public void SavePlayerData(int level)
    {
        UpdateUserDataRequest request = new UpdateUserDataRequest
        {
            Data = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Level", level.ToString() },
                { "LastUpdated", System.DateTime.Now.ToString(CultureInfo.InvariantCulture) }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, OnDataSaved, OnError);
    }

    private void LoadPlayerData()
    {
        GetUserDataRequest request = new GetUserDataRequest();

        PlayFabClientAPI.GetUserData(request, OnDataLoaded, OnError);
    }

    private void OnDataSaved(UpdateUserDataResult result)
    {
        Debug.Log("Datos del jugador guardados correctamente.");
    }

    private void OnDataLoaded(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("Level"))
        {
            int level = int.Parse(result.Data["Level"].Value);
            Debug.Log($"Nivel cargado: {level}");
        }
        else
        {
            Debug.Log("No se encontraron datos del jugador.");
        }
    }

    // 3. Estadísticas y Tablas de Clasificación
    public void UpdatePlayerStatistics(int score)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new System.Collections.Generic.List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = "HighScore", Value = score }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnStatisticsUpdated, OnError);
    }

    private void OnStatisticsUpdated(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("Estadísticas del jugador actualizadas.");
    }

    public void GetLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "HighScore",
            StartPosition = 0,
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardRetrieved, OnError);
    }

    private void OnLeaderboardRetrieved(GetLeaderboardResult result)
    {
        foreach (var entry in result.Leaderboard)
        {
            Debug.Log($"{entry.Position + 1}: {entry.DisplayName} - {entry.StatValue}");
        }
    }

    // 4. Inventarios y Tienda Virtual
    public void PurchaseItem(string itemId, string currencyCode, int price)
    {
        var request = new PurchaseItemRequest
        {
            ItemId = itemId,
            VirtualCurrency = currencyCode,
            Price = price
        };

        PlayFabClientAPI.PurchaseItem(request, OnItemPurchased, OnError);
    }

    private void OnItemPurchased(PurchaseItemResult result)
    {
        Debug.Log($"Item comprado: {result.Items[0].ItemId}");
    }

    public void GetPlayerInventory()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnInventoryRetrieved, OnError);
    }

    private void OnInventoryRetrieved(GetUserInventoryResult result)
    {
        foreach (var item in result.Inventory)
        {
            Debug.Log($"Item en inventario: {item.ItemId}");
        }
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError($"Error: {error.GenerateErrorReport()}");
    }
}

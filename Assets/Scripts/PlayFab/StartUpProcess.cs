using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.PfEditor.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayFab
{
    public class StartUpProcess : MonoBehaviour
    {
        [Header("Title Config")]
        [SerializeField] private TMP_Text titleNameTmp;
        [SerializeField] private TMP_Text titleMessageTmp;
        
        [Header("User Data")]
        [SerializeField] private TMP_Text userIdTmp;
        [SerializeField] private TMP_Text userNameTmp;
        [SerializeField] private TMP_Text userDescriptionTmp;
        [SerializeField] private TMP_Text userCoinsTmp;
        [SerializeField] private TMP_Text userLevelTmp;
        
        [Header("Buttons")]
        [SerializeField] private Button startSessionButton;
        [SerializeField] private Button loadSessionButton;
        
        [Header("Feedback")]
        [SerializeField] private TMP_Text feedbackTmp;
        
        public void StartSession()
        {
            Authenticate();
            startSessionButton.gameObject.SetActive(false);
        }

        public void SessionData()
        {
            LoadTitleData();
            loadSessionButton.gameObject.SetActive(false);
        }

        public void SessionUser()
        {
            LoadUserData();
        }

        #region AUTHENTICATION
        
        private void Authenticate()
        {
            LoginWithCustomIDRequest request = new LoginWithCustomIDRequest()
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true
            };
            
            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
        }

        private void OnLoginSuccess(LoginResult result)
        {
            userIdTmp.text = result.PlayFabId;
        }
        
        #endregion
        
        #region TITLE DATA
        private void LoadTitleData()
        {
            GetTitleDataRequest request = new GetTitleDataRequest();
            
            PlayFabClientAPI.GetTitleData(request, OnTitleData, OnError);
        }

        private void OnTitleData(GetTitleDataResult result)
        {
            JsonObject data = (JsonObject) JsonWrapper.DeserializeObject(result.Data["titleData"]);
            data.TryGetValue("titleName", out object titleName);
            data.TryGetValue("titleMessage", out object titleMessage);
            
            titleNameTmp.text = titleName?.ToString();
            titleMessageTmp.text = titleMessage?.ToString();
        }
        
        #endregion
        
        #region USER DATA

        private void LoadUserData()
        {
            GetUserDataRequest userDataRequest = new GetUserDataRequest();
            GetUserInventoryRequest inventoryRequest = new GetUserInventoryRequest();
            GetPlayerStatisticsRequest staticsRequest = new GetPlayerStatisticsRequest();
            
            PlayFabClientAPI.GetUserData(userDataRequest, OnUserData, OnError);
            PlayFabClientAPI.GetUserInventory(inventoryRequest, OnUserInventory, OnError);
            PlayFabClientAPI.GetPlayerStatistics(staticsRequest, OnPlayerStatistics, OnError);
        }

        private void OnUserData(GetUserDataResult result)
        {
            result.Data.TryGetValue("userName", out UserDataRecord userName);
            result.Data.TryGetValue("userDescription", out UserDataRecord userDescription);
            result.Data.TryGetValue("userLevel", out UserDataRecord userLevel);
            
            string userNameText = userName != null ? userName.ToString() : userIdTmp.text;
            string userDescriptionText = userDescription != null ? userDescription.ToString() : "NO DESCRIPTION";
            string userLevelText = userLevel != null ? userLevel.ToString() : "NO LEVEL";
            
            userNameTmp.text = userNameText;
            userDescriptionTmp.text = userDescriptionText;
            userLevelTmp.text = userLevelText;
        }

        private void OnUserInventory(GetUserInventoryResult result)
        {
            result.VirtualCurrency.TryGetValue("CO", out int currency);
            userCoinsTmp.text = currency.ToString();
        }

        private void OnPlayerStatistics(GetPlayerStatisticsResult result)
        {
            bool hasCurrencyGranted = false;

            foreach (var statistic in result.Statistics)
            {
                if (statistic.StatisticName == "CurrencyGranted" && statistic.Value == 1)
                {
                    hasCurrencyGranted = true;
                    break;
                }
            }

            if (!hasCurrencyGranted)
            {
                CheckFirstSession();
            }
        }

        #endregion
        
        #region CLOUD SCRIPTS

        private void CheckFirstSession()
        {
            ExecuteFunctionRequest request = new ExecuteFunctionRequest()
            {
                FunctionName = "SimpleReward",
                GeneratePlayStreamEvent = true,
            };
            
            PlayFabCloudScriptAPI.ExecuteFunction(request, OnExecuteFunction, OnError);
        }

        private void OnExecuteFunction(ExecuteFunctionResult result)
        {
            string json = JsonWrapper.SerializeObject(result.FunctionResult);
            feedbackTmp.text = json;
            // Dictionary<string, object> data = JsonUtility.FromJson<Dictionary<string, object>>(json);
            //
            // data.TryGetValue("grantedAmount", out object grantedAmount);
            // data.TryGetValue("userLevel", out object userLevel);
            // data.TryGetValue("message", out object message);
            //
            // string grantedAmountText = grantedAmount != null ? grantedAmount.ToString() : "-";
            // userCoinsTmp.text = grantedAmountText;
            //
            // string userLevelText = userLevel != null ? userLevel.ToString() : "-";
            // userLevelTmp.text = userLevelText;
            //
            // string messageText = message != null ? message.ToString() : "No Feedback";
            // feedbackTmp.text = messageText;
        }

        #endregion

        private void OnError(PlayFabError obj)
        {
            string errorMessage = "ERROR:\n" + obj.ErrorMessage;
            print(errorMessage);
            feedbackTmp.text = errorMessage;
        }
    }
}
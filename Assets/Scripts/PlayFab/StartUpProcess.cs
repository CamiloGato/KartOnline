using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.EconomyModels;
using PlayFab.PfEditor.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EntityKey = PlayFab.EconomyModels.EntityKey;

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
        
        private PlayFabClientInstanceAPI _clientApi;
        private PlayFabEconomyInstanceAPI _clientEconomyApi;
        private PlayFabCloudScriptInstanceAPI _cloudScriptApi;
        
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
            string deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
            
            _clientApi = new PlayFabClientInstanceAPI();
            
            LoginWithCustomIDRequest request = new LoginWithCustomIDRequest()
            {
                CustomId = deviceUniqueIdentifier,
                CreateAccount = true
            };
            
            _clientApi.LoginWithCustomID(request, OnLoginSuccess, OnError);
        }

        private void OnLoginSuccess(LoginResult result)
        {
            userIdTmp.text = result.PlayFabId;

            PlayFabAuthenticationContext authenticationContext = result.AuthenticationContext;
            
            _clientEconomyApi = new PlayFabEconomyInstanceAPI(authenticationContext);
            _cloudScriptApi = new PlayFabCloudScriptInstanceAPI(authenticationContext);
        }
        
        #endregion
        
        #region TITLE DATA
        private void LoadTitleData()
        {
            GetTitleDataRequest request = new GetTitleDataRequest();
            
            _clientApi.GetTitleData(request, OnTitleData, OnError);
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
            GetInventoryItemsRequest inventoryRequest = new GetInventoryItemsRequest()
            {
                Entity = new EntityKey()
                {
                    Id = "138D4A5DFA0C3F62",
                    Type = "title_player_account"
                }
            };
            
            _clientApi.GetUserData(userDataRequest, OnUserData, OnError);
            _clientEconomyApi.GetInventoryItems(inventoryRequest, OnUserInventory, OnError);
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

        private void OnUserInventory(GetInventoryItemsResponse result)
        {
            foreach (var item in result.Items)
            {
                if (item.Id == "b6191891-90ea-4749-a47a-222a9e46b180" && item.Amount > 0)
                {
                    userCoinsTmp.text = item.Amount.ToString();
                    return;
                }
            }
            
            userCoinsTmp.text = "0";
            CheckFirstSession();
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
            
            _cloudScriptApi.ExecuteFunction(request, OnExecuteFunction, OnError);
        }

        private void OnExecuteFunction(ExecuteFunctionResult result)
        {
            string json = JsonWrapper.SerializeObject(result.FunctionResult);
            feedbackTmp.text = json;
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
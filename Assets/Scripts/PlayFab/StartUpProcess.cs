using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.EconomyModels;
#if !UNITY_SERVER
using PlayFab.PfEditor.Json;
#endif
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
        [SerializeField] private TMP_InputField userIdInputField;
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

        private string _serverLabel;
        
        public void StartSession()
        {
            Authenticate();
        }

        public void SessionData()
        {
            LoadTitleData();
        }

        public void SessionUser()
        {
            LoadUserData();
        }

        #region AUTHENTICATION
        
        private void Authenticate()
        {
            string deviceUniqueIdentifier = userIdInputField.text;
            
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
            PlayFabAuthenticationContext authenticationContext = result.AuthenticationContext;
            
            _clientEconomyApi = new PlayFabEconomyInstanceAPI(authenticationContext);
            _cloudScriptApi = new PlayFabCloudScriptInstanceAPI(authenticationContext);
            
            ExecuteFunction("BodyRequest");
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
            result.Data.TryGetValue("Name", out string titleName);
            
            _serverLabel = titleName;
            titleNameTmp.text = $"Server: {titleName}";
        }
        
        #endregion
        
        #region USER DATA

        private void LoadUserData()
        {
            GetUserDataRequest userDataRequest = new GetUserDataRequest();
            GetInventoryItemsRequest inventoryRequest = new GetInventoryItemsRequest()
            {
                Entity = new EconomyModels.EntityKey()
                {
                    Id = _clientApi.authenticationContext.EntityId,
                    Type = _clientApi.authenticationContext.EntityType
                }
            };
            
            _clientApi.GetUserData(userDataRequest, OnUserData, OnError);
            _clientEconomyApi.GetInventoryItems(inventoryRequest, OnUserInventory, OnError);
        }

        private void OnUserData(GetUserDataResult result)
        {
            result.Data.TryGetValue("userDescription", out UserDataRecord userDescription);
            result.Data.TryGetValue("userLevel", out UserDataRecord userLevel);
            
            string userDescriptionText = userDescription != null ? userDescription.ToString() : "NO DESCRIPTION";
            string userLevelText = userLevel != null ? userLevel.ToString() : "NO LEVEL";
            
            userDescriptionTmp.text = userDescriptionText;
            userLevelTmp.text = userLevelText;
        }

        private void OnUserInventory(GetInventoryItemsResponse result)
        {
            foreach (var item in result.Items)
            {
                if (item.Id == "b6191891-90ea-4749-a47a-222a9e46b180" && item.Amount > 0)
                {
                    print("COINS: " + item.Amount);
                    userCoinsTmp.text = item.Amount.ToString();
                    return;
                }
            }
            
            userCoinsTmp.text = "0";
            ExecuteFunction("SimpleReward");
        }

        #endregion
        
        #region CLOUD SCRIPTS

        private void ExecuteFunction(string functionName)
        {
            ExecuteFunctionRequest request = new ExecuteFunctionRequest()
            {
                FunctionName = functionName,
                GeneratePlayStreamEvent = true,
                FunctionParameter = new { ServerLabel = _serverLabel }
            };
            
            _cloudScriptApi.ExecuteFunction(request, OnExecuteFunction, OnError);
        }

        private void OnExecuteFunction(ExecuteFunctionResult result)
        {
#if !UNITY_SERVER
            string json = JsonWrapper.SerializeObject(result.FunctionResult);
            feedbackTmp.text = json;
#endif
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
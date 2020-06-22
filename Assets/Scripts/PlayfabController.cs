using PlayFab;
using PlayFab.ClientModels;
//using PlayFab.PfEditor.Json;
using UnityEngine;
using UnityEngine.UI;

public class PlayfabController : MonoBehaviour
{
    public static PlayfabController PFC;

    public PlayfabLoginManager playfabLoginManager;

    public int playerGold;

    public Text goldNumber;
    public Text savedPrompt;
    public Transform promptLocation;

    private void OnEnable()
    {
        if(PlayfabController.PFC == null)
        {
            PlayfabController.PFC = this;
        }
        else
        {
            if(PlayfabController.PFC != this)
            {
                Destroy(this.gameObject);
            }
        }
        DontDestroyOnLoad(this.gameObject);

        GetStatistics();
    }

    public void GainGold()
    {
        playerGold += 1;
        UpdateGoldText();
    }

    private void UpdateGoldText()
    {
        goldNumber.text = playerGold.ToString();
    }

    // Build the request object and access the API
    public void UpdateCloudPlayerStats()
    {
        ValidatePlayfabAccount();
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
        {
            FunctionName = "updatePlayerStats", // Arbitrary function name (must exist in your uploaded cloud.js file)
            FunctionParameter = new { PlayerGold = playerGold }, // The parameter provided to your function
            GeneratePlayStreamEvent = true, // Optional - Shows this event in PlayStream
        }, OnCloudUpdateStats, OnErrorShared);
    }

    private void OnCloudUpdateStats(ExecuteCloudScriptResult result)
    {
        Instantiate(savedPrompt, promptLocation);
#if UNITY_EDITOR
        // CloudScript returns arbitrary results, so you have to evaluate them one step and one parameter at a time
        //Debug.Log(JsonWrapper.SerializeObject(result.FunctionResult));
        //JsonObject jsonResult = (JsonObject)(result.FunctionResult);
        //object messageValue;
        //jsonResult.TryGetValue("messageValue", out messageValue); // note how "messageValue" directly corresponds to the JSON values set in CloudScript
        //Debug.Log((string)messageValue);
#endif
    }

    private static void OnErrorShared(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
    }

    public void GetCloudPlayerStats()
    {
        ValidatePlayfabAccount();
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
        {
            FunctionName = "getPlayerStats", // Arbitrary function name (must exist in your uploaded cloud.js file)
            FunctionParameter = new {}, // The parameter provided to your function
            GeneratePlayStreamEvent = true, // Optional - Shows this event in PlayStream
        }, OnGetStats, OnErrorShared);
    }

    private void OnGetStats(ExecuteCloudScriptResult result)
    {
#if UNITY_EDITOR
        // CloudScript returns arbitrary results, so you have to evaluate them one step and one parameter at a time
        //Debug.Log(JsonWrapper.SerializeObject(result.FunctionResult));
        //JsonObject jsonResult = (JsonObject)result.FunctionResult;
        //object statsobj;
        //jsonResult.TryGetValue("result", out statsobj);
        //GetPlayerStatisticsResult stats = (GetPlayerStatisticsResult)statsobj;
        //foreach (var stat in stats.Statistics)
        //{
        //    switch (stat.StatisticName)
        //    {
        //        case "PlayerGold":
        //            playerGold = stat.Value;
        //            break;
        //        default:
        //            break;
        //    }
        //}
#endif
    }

    public void GetStatistics()
    {
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            OnGetStatistics,
            error => Debug.LogError(error.GenerateErrorReport())
        );
    }

    private void OnGetStatistics(GetPlayerStatisticsResult result)
    {
        Debug.Log("Received the following Statistics:");
        foreach (var eachStat in result.Statistics)
        {
            //Debug.Log("Statistic (" + eachStat.StatisticName + "): " + eachStat.Value);
            switch (eachStat.StatisticName)
            {
                case "PlayerGold":
                    playerGold = eachStat.Value;
                    UpdateGoldText();
                    break;
                default:
                    break;
            }
        }
    }

    public void ValidatePlayfabAccount()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            playfabLoginManager.OnLogoutClicked();
            gameObject.SetActive(false);
        }
    }
}

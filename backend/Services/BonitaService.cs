using System.Net.Http;
using System.Threading.Tasks;

public class BonitaService
{
    private readonly RequestHelper _request;

    public BonitaService(RequestHelper request)
    {
        _request = request;
    }

    public async Task<string> GetAllProcessesAsync()
    {
        return await _request.DoRequestAsync(HttpMethod.Get, "API/bpm/process?p=0&c=1000");
    }

    public async Task<string> GetTasksAsync()
    {
        return await _request.DoRequestAsync(HttpMethod.Get, "API/bpm/humanTask?p=0&c=50");
    }


    public async Task<string> GetProcessNameById(string processId)
    {
        return await _request.DoRequestAsync(HttpMethod.Get, $"API/bpm/process/{processId}");
    }

    public async Task<string> GetProcessIdByName(string processName)
    {
        return await _request.DoRequestAsync(HttpMethod.Get, $"API/bpm/process?f=name={processName}");
    }

    public async Task<string> GetProcessCountAsync()
    {
        return await _request.DoRequestAsync(HttpMethod.Get, "API/bpm/process/count");

    }

    //Initiate process by id
    public async Task<string> StartProcessById(string processId)
    {
        return await _request.DoRequestAsync(HttpMethod.Post, $"API/bpm/process/{processId}/instantiation");
    }

    public async Task<string> SetVariableByCase(string caseId, string variable, string value, string varType)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("name", variable),
            new KeyValuePair<string,string>("value", value),
            new KeyValuePair<string,string>("type", varType)
        });

        return await _request.DoRequestAsync(HttpMethod.Put, $"API/bpm/case/{caseId}/variable/{variable}", content);
    }

    public async Task<string> AssignTaskToUser(string taskId, string userId)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("assigned_id", userId)
        });
        return await _request.DoRequestAsync(HttpMethod.Put, $"API/bpm/userTask/{taskId}", content);
    }

    public async Task<string> CompleteActivityAsync(string taskId)
    {
        return await _request.DoRequestAsync(HttpMethod.Post, $"API/bpm/userTask/{taskId}/execution");
    }


    // public async Task<string> GetVariableAsync(string taskId, string variable)
    // {
    //     // Get the user task to retrieve the caseId
    //     var taskResponse = await _request.DoRequestAsync(HttpMethod.Get, $"API/bpm/userTask/{taskId}");
    //     // Parse the caseId from the response (assuming JSON format)
    //     var caseId = Newtonsoft.Json.Linq.JObject.Parse(taskResponse)["caseId"]?.ToString();

    //     if (string.IsNullOrEmpty(caseId))
    //         throw new System.Exception("caseId not found in userTask response.");

    //     // Get the variable value from the case
    //     var variableResponse = await _request.DoRequestAsync(HttpMethod.Get, $"API/bpm/caseVariable/{caseId}/{variable}");
    //     return variableResponse;
    // }



}

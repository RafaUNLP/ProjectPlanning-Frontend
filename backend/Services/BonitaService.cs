using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.ResponseCompression;
using backend.DTOs;

namespace backend.Services;

public class BonitaService
{
    private readonly RequestHelper _request;

    public BonitaService(RequestHelper request)
    {
        _request = request;
    }

    // public async Task<string> GetAllProcessesAsync()
    // {
    //     return await _request.DoRequestAsync(HttpMethod.Get, "API/bpm/process?p=0&c=1000");
    // }

    // public async Task<string> GetTasksAsync()
    // {
    //     return await _request.DoRequestAsync(HttpMethod.Get, "API/bpm/humanTask?p=0&c=50");
    // }


    // public async Task<string> GetProcessNameById(string processId)
    // {
    //     return await _request.DoRequestAsync(HttpMethod.Get, $"API/bpm/process/{processId}");
    // }

    public async Task<string?> GetProcessIdByName(string processName)
    {
        var response = await _request.DoRequestAsync<List<BonitaProcessResponse>>(HttpMethod.Get, $"API/bpm/process?f=name={processName}");

        return response?.FirstOrDefault()?.id;

    }

    // public async Task<Dictionary<string, string>> GetProcessCountAsync()
    // {
    //     return await _request.DoRequestAsync(HttpMethod.Get, "API/bpm/process/count");

    // }

    //Initiate process by id
    public async Task<long> StartProcessById(string processId)
    {
        var response = await _request.DoRequestAsync<BonitaCaseResponse>(HttpMethod.Post, $"API/bpm/process/{processId}/instantiation");


        return response.caseId;
    }

    /// <summary>
    /// Actualiza el valor de una variable de proceso en un caso específico.
    /// </summary>
    /// <param name="caseId">Identificador del caso en Bonita</param>
    /// <param name="variableName">Nombre de la variable de proceso a actualizar</param>
    /// <param name="value">Nuevo valor a asignar a la variable</param>
    /// <param name="varType">
    /// Tipo de dato de la variable en Bonita 
    /// (por ejemplo: "java.lang.String", "java.lang.Integer", "java.lang.Boolean").
    /// </param>
    /// <returns>200: Respuesta vacia</returns>
    public async Task<bool> SetVariableByCase(string caseId, string variableName, string value, string varType)
    {
        var payload = new
        {
            value = value,
            type = varType
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        return await _request.DoRequestAsync<object>(HttpMethod.Put, $"API/bpm/caseVariable/{caseId}/{variableName}", content) == null;
    }

    // public async Task<string> AssignTaskToUser(string taskId, string userId)
    // {
    //     var content = new FormUrlEncodedContent(new[]
    //     {
    //         new KeyValuePair<string,string>("assigned_id", userId)
    //     });
    //     return await _request.DoRequestAsync(HttpMethod.Put, $"API/bpm/userTask/{taskId}", content);
    // }

    public async Task<bool> CompleteActivityAsync(string taskId)
    {
        return await _request.DoRequestAsync<object>(HttpMethod.Post, $"API/bpm/userTask/{taskId}/execution") == null;
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

    public async Task<BonitaActivityResponse?> GetActivityByCaseId(string caseId)
    {
        var response = await _request.DoRequestAsync<List<BonitaActivityResponse>>(HttpMethod.Get, $"API/bpm/task?f=caseId={caseId}");
        return response?.FirstOrDefault();
    }

    public async Task<string> GetUserIdByUserName(string userName)
    {
        var response = await _request.DoRequestAsync<List<BonitaUserResponse>>(HttpMethod.Get, $"API/identity/user?f=displayName={userName}");
        return response?.FirstOrDefault()?.id;
    }
    
    public async Task<bool> AssignActivityToUser(string taskId, string userId)
    {
        var payload = new
        {
            assigned_id = userId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );
        return await _request.DoRequestAsync<object>(HttpMethod.Put, $"API/bpm/userTask/{taskId}", content) == null;
    }
}

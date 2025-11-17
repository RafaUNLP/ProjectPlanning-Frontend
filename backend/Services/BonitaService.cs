using System.Text;
using System.Text.Json;
using backend.DTOs;
namespace backend.Services;

public class BonitaService
{
    private readonly RequestHelper _request;

    public BonitaService(RequestHelper requestHelper)
    {
        _request = requestHelper ?? throw new ArgumentNullException(nameof(requestHelper));
    }

    /// <summary>
    /// Recupera el ID de un proceso en Bonita a partir de su nombre.
    /// </summary>
    /// <param name="processName">Nombre del proceso en Bonita</param>
    /// <returns>ID del proceso en Bonita</returns>
    /// OJO POR AHI NECESITAS USAR EL DE ABAJO (GetProcessIdByDisplayName)
    public async Task<string> GetProcessIdByName(string processName)
    {

        var response = await _request.DoRequestAsync<List<BonitaProcessResponse>>(HttpMethod.Get, $"API/bpm/process?f=name={processName}");
        try
        {
            return response.First().id;
        }
        catch (Exception ex)
        {
            throw new Exception($"No se encontró el proceso con nombre '{processName}': {ex.Message}");
        }

    }

    /// <summary>
    /// Recupera el ID de un proceso en Bonita a partir de su DISPLAY nombre.
    /// </summary>
    /// <param name="processName">Nombre de DISPLAY del proceso en Bonita</param>
    /// <returns>ID del proceso en Bonita</returns>
    public async Task<string> GetProcessIdByDisplayName(string processName)
    {

        var response = await _request.DoRequestAsync<List<BonitaProcessResponse>>(HttpMethod.Get, $"API/bpm/process?f=displayName={processName}");
        try
        {
            return response.First().id;
        }
        catch (Exception ex)
        {
            throw new Exception($"No se encontró el proceso con nombre '{processName}': {ex.Message}");
        }

    }


    /// <summary>
    /// Inicia una nueva instancia de un proceso específico en Bonita.
    /// </summary>
    /// <param name="processId">Identificador del proceso en Bonita</param>
    /// <returns>ID del caso (instancia de proceso) iniciado</returns>
    public async Task<long> StartProcessById(string processId)
    {
        try
        {
            var response = await _request.DoRequestAsync<BonitaCaseResponse>(HttpMethod.Post, $"API/bpm/process/{processId}/instantiation");
            return response.caseId;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al iniciar el proceso con ID '{processId}': {ex.Message}");
        }
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
        try
        {
            BonitaVariable payload = new BonitaVariable
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
        catch (Exception ex)
        {
            throw new Exception($"Error al establecer la variable '{variableName}' en el caso '{caseId}': {ex.Message}");
        }
    }

    /// <summary>
    /// Recupera el valor de una variable de proceso en un caso específico.
    /// </summary>
    /// <param name="caseId">Identificador del caso en Bonita</param>
    /// <param name="variableName">Nombre de la variable de proceso a recuperar</param>
    /// </param>
    /// <returns>200: Respuesta vacia</returns>
    public async Task<string> GetVariableByCaseIdAndName(long caseId, string variableName)
    {
        try
        {
           BonitaVariable response = await _request.DoRequestAsync<BonitaVariable>(HttpMethod.Get, $"API/bpm/caseVariable/{caseId}/{variableName}");

            //si Bonita devuelve un JSON encerrado entre comillas
            if (response.value.StartsWith("\"") && response.value.EndsWith("\""))
                response.value = response.value.Substring(1, response.value.Length - 2);

            //si hay escaping, desescapar
            if (response.value.Contains("\\\""))
                response.value = JsonSerializer.Deserialize<string>(response.value);

           return response.value; //por fuera va a haber que hacer un casting
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al recuperar la variable '{variableName}' en el caso '{caseId}': {ex.Message}");
        }
    }

    /// <summary>
    /// Completa una actividad (tarea) específica en una instancia de proceso.
    /// </summary>
    /// <param name="taskId">Identificador de la actividad (tarea) en la instancia de proceso de Bonita</param>
    /// <returns>true si la respuesta es 200</returns>
    public async Task<bool> CompleteActivityAsync(string taskId)
    {
        return await _request.DoRequestAsync<object>(HttpMethod.Post, $"API/bpm/userTask/{taskId}/execution") == null;
    }

    /// <summary>
    /// Recupera la primera actividad asociada a un caso específico.
    /// </summary>
    /// <param name="caseId">Identificador del caso en Bonita</param>
    /// <returns>Objeto BonitaActivityResponse que representa la actividad encontrada</returns> 
    public async Task<BonitaActivityResponse?> GetActivityByCaseId(string caseId)
    {
        var response = await _request.DoRequestAsync<List<BonitaActivityResponse>>(HttpMethod.Get, $"API/bpm/task?f=caseId={caseId}");
        return response?.FirstOrDefault();
    }

    /// <summary>
    /// Recupera una actividad específica dentro de un caso por su nombre.
    /// Implementa un mecanismo de reintento para manejar la latencia en la aparición de la actividad.
    /// </summary>
    /// <param name="caseId">Identificador del caso en Bonita</param>
    /// <param name="activityName">Nombre de la actividad a recuperar</param>
    /// <returns>Objeto BonitaActivityResponse que representa la actividad encontrada</returns>
    /// OJO POR AHI NECESITAS USAR EL DE ABAJO (GetActivityByCaseIdAndDisplayName)
    public async Task<BonitaActivityResponse> GetActivityByCaseIdAndName(string caseId, string activityName)
    {
        // 1. Configuración de reintento
        int attempts = 0;
        int maxAttempts = 10; // Reintentar 10 veces
        int delayMs = 500;    // Esperar 500ms entre intentos (5 segundos en total)

        string encodedName = System.Net.WebUtility.UrlEncode(activityName);
        string endpoint = $"API/bpm/humanTask?f=caseId={caseId}&f=name={encodedName}";

        //Hay que hacer polling de la actividad porque a veces tarda en aparecer
        while (attempts < maxAttempts)
        {
            try
            {
                var response = await _request.DoRequestAsync<List<BonitaActivityResponse>>(HttpMethod.Get, endpoint);

                if (response != null && response.Any())
                {
                    Console.WriteLine($"Actividad encontrada en intento {attempts + 1}.");
                    return response.First(); 
                }

                attempts++;
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al buscar actividad por nombre: {ex.Message}");
            }
        }

        throw new Exception($"Timeout: La actividad '{activityName}' no apareció a tiempo para el caso '{caseId}'.");
    }

    /// <summary>
    /// Recupera una actividad específica por DISPLAY NAME dentro de un caso por su nombre.
    /// Implementa un mecanismo de reintento para manejar la latencia en la aparición de la actividad.
    /// </summary>
    /// <param name="caseId">Identificador del caso en Bonita</param>
    /// <param name="activityDisplayName">Nombre de la actividad a recuperar</param>
    /// <returns>Objeto BonitaActivityResponse que representa la actividad encontrada</returns>
    public async Task<BonitaActivityResponse> GetActivityByCaseIdAndDisplayName(string caseId, string activityDisplayName)
    {
        // 1. Configuración de reintento
        int attempts = 0;
        int maxAttempts = 10; // Reintentar 10 veces
        int delayMs = 500;    // Esperar 500ms entre intentos (5 segundos en total)

        string endpoint = $"API/bpm/humanTask?f=caseId={caseId}&f=displayName={activityDisplayName}";

        //Hay que hacer polling de la actividad porque a veces tarda en aparecer
        while (attempts < maxAttempts)
        {
            try
            {
                var response = await _request.DoRequestAsync<List<BonitaActivityResponse>>(HttpMethod.Get, endpoint);

                if (response != null && response.Any())
                {
                    Console.WriteLine($"Actividad encontrada en intento {attempts + 1}.");
                    return response.First(); 
                }

                attempts++;
                await Task.Delay(delayMs);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al buscar actividad por nombre: {ex.Message}");
            }
        }

        throw new Exception($"Timeout: La actividad '{activityDisplayName}' no apareció a tiempo para el caso '{caseId}'.");
    }

    // <summary>
    // Recupera el ID de un usuario en Bonita a partir de su nombre de usuario.
    // </summary>
    // <param name="userName">Nombre de usuario en Bonita</param>
    // <returns>ID del usuario en Bonita</returns>
    public async Task<string> GetUserIdByUserName(string userName)
    {
        try
        {
            var response = await _request.DoRequestAsync<List<BonitaUserResponse>>(HttpMethod.Get, $"API/identity/user?s={userName}");
            return response.First().id;
        }
        catch (Exception ex)
        {
            throw new Exception($"No se encontró el usuario con nombre '{userName}': {ex.Message}");
        }
    }
    
    // <summary>
    // Asigna una actividad a un usuario específico.
    // </summary>
    // <param name="taskId">Identificador de la actividad (tarea) en la instancia de proceso de Bonita</param>
    // <param name="userId">Identificador del usuario al que se asignará la actividad</param>
    // <returns>200: Respuesta vacia</returns>
    public async Task<bool> AssignActivityToUser(string taskId, string userId)
    {
        try
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
        } catch (Exception ex)
        {
            throw new Exception($"Error al asignar la actividad '{taskId}' al usuario '{userId}': {ex.Message}");
        }
    }
}

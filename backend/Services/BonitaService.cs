using ApiACEAPP.Helpers;

namespace backend.Services;

public class BonitaService
{

    public BonitaService()
    {
        //en caso de necesitar un helper o servicio externo, se lo puede inyectar
    }

    private async Task<string> GetTokenBonita()
    {
        //método para hacer el login en bonita
        return string.Empty;
    }

    public async Task<string> CrearProceso(string algoQueNoSeQueEs,List<string> variables)
    {
        //método para instanciar un proceso con determinadas variables
        return string.Empty;
    }

    public async Task<string> GetProceso(string procesoId)
    {
        //método para recuperar un proeso
        return string.Empty;
    }
}
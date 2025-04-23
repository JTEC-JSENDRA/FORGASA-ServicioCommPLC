using Newtonsoft.Json.Linq;
using S7.Net;

namespace ServicioWindows.Clases
{
    internal class ConsAPI
    {
        private readonly Plc PLC;

        public ConsAPI(Plc PLC = null)
        {
            this.PLC = PLC;
        }
        public async Task<string> DatosCabecera(string apiUrl, string Dato)
        {
            string Valor = null;

            HttpClient httpClient = new HttpClient();
            //Console.WriteLine($"{apiUrl}");
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                //Console.WriteLine($"ResponseBody = {responseBody}");

                JArray jsonArray = JArray.Parse(responseBody);
                //Console.WriteLine($"JsonArray = {jsonArray}");

                JObject Objeto = (JObject)jsonArray[0][0];
                //Console.WriteLine($"Objeto = {Objeto}");

                Valor = Objeto.Value<string>(Dato);
                //Console.WriteLine($"Valor =  {Valor}");

            }
            else
            {
                // La solicitud no fue exitosa
                // Puedes obtener el código de estado y el mensaje de error utilizando response.StatusCode y response.ReasonPhrase
                Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            }

            return Valor;
        }

        public async Task<string> DatosCabeceraEtapa(string apiUrl, string Dato, int Etapa)
        {
            string Valor = null;

            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();

                JArray jsonArray = JArray.Parse(responseBody);

                JObject Objeto = (JObject)jsonArray[Etapa][0][0];
                Valor = Objeto.Value<string>(Dato);

                //Console.WriteLine(Valor);

            }
            else
            {
                // La solicitud no fue exitosa
                // Puedes obtener el código de estado y el mensaje de error utilizando response.StatusCode y response.ReasonPhrase
                Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            }

            return Valor;
        }

        public async Task<string> DatosEtapas(string DB, string DB_Offsets, string apiUrl, int NEtapa = 0)
        {
            string Valor = null;
            CommPLC commPLC = new CommPLC(PLC);

            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                int NumeroEtapas;
                int NumeroProcesos;
                int NumeroConsignas;

                string responseBody = await response.Content.ReadAsStringAsync();
                //Console.WriteLine($"Response body de datos etapas para la db {DB}: {responseBody}");
                //Si el jarray tiene varias recetas te cuenta todas las etapas contadas asi
                JArray jsonArray = JArray.Parse(responseBody);
                NumeroEtapas = (jsonArray.Count) - 1;

                //Console.WriteLine($"Numero de etapas: {NumeroEtapas}");
                JArray Etapa = (JArray)jsonArray[NEtapa];

                NumeroProcesos = (Etapa.Count) - 1;
                //Console.WriteLine($"Numero de procesos en la etapa {NEtapa}: {NumeroProcesos}");

                for (int i = 1; i <= (NumeroProcesos); i++)
                {
                    JArray Proceso = (JArray)jsonArray[NEtapa][i];
                    //Console.WriteLine($"Proceso: {Proceso}");

                    NumeroConsignas = (Proceso.Count());
                    //Console.WriteLine($"Numero de Consignas en el proceso {i}: {NumeroConsignas}");

                    for (int u = 0; u <= (NumeroConsignas - 1); u++)
                    {
                        JObject ProcesoObj = (JObject)jsonArray[NEtapa][i][u];
                        //Console.WriteLine($"ProcesoObj: {ProcesoObj}");

                        foreach (JProperty property in ProcesoObj.Properties())
                        {
                            string propertyName = property.Name;
                            string propertyValue = property.Value.ToString();

                            //Console.WriteLine($"Pre fallo opcion no valida");
                            commPLC.CargaDatosReceta(DB, DB_Offsets, propertyName, propertyValue);
                            //Console.WriteLine($"Nombre y valor de la propiedad: {propertyName} - {propertyValue}");
                        }
                    }
                }
            }
            else
            {
                // La solicitud no fue exitosa
                // Puedes obtener el código de estado y el mensaje de error utilizando response.StatusCode y response.ReasonPhrase
                Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            }

            return Valor;
        }
    }
}

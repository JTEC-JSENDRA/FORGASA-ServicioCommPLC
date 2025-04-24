using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using S7.Net;
using ServicioWindows.Models;
using System.Text;

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
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                JArray jsonArray = JArray.Parse(responseBody);
                JObject Objeto = (JObject)jsonArray[0][0];
                Valor = Objeto.Value<string>(Dato);
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

                //Si el jarray tiene varias recetas te cuenta todas las etapas contadas asi
                JArray jsonArray = JArray.Parse(responseBody);
                NumeroEtapas = (jsonArray.Count) - 1;

                JArray Etapa = (JArray)jsonArray[NEtapa];

                NumeroProcesos = (Etapa.Count) - 1;

                for (int i = 1; i <= (NumeroProcesos); i++)
                {
                    JArray Proceso = (JArray)jsonArray[NEtapa][i];

                    NumeroConsignas = (Proceso.Count());

                    for (int u = 0; u <= (NumeroConsignas - 1); u++)
                    {
                        JObject ProcesoObj = (JObject)jsonArray[NEtapa][i][u];

                        foreach (JProperty property in ProcesoObj.Properties())
                        {
                            string propertyName = property.Name;
                            string propertyValue = property.Value.ToString();

                            commPLC.CargaDatosReceta(DB, DB_Offsets, propertyName, propertyValue);
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

        public async Task ActualizarEtapaAPI(DatosGenReceta GenReceta, int EtapaAct, Logs Logs)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var data = new
                {
                    OF = GenReceta.OF,
                    nombreEtapa = GenReceta.NombreEtapaActual,
                    numeroEtapa = $"{EtapaAct}/{GenReceta.NumEtapas}"
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                string RutaApiAct = "http://localhost:7248/api/Worker/ActualizarOF";

                HttpResponseMessage response = await httpClient.PostAsync(RutaApiAct, content);
            }
        }

        public async Task FinalizarOFAPI(string OF, string estado, Logs Logs)
        {            
            using (HttpClient httpClient = new HttpClient())
            {
                var data = new 
                { 
                    OF = OF, 
                    estado = estado
                };
                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string RutaApiFin = "http://localhost:7248/api/Worker/FinalizarOF";
                HttpResponseMessage response = await httpClient.PostAsync(RutaApiFin, content);

                if (response.IsSuccessStatusCode)
                {
                    Logs.RegistrarInfo($"✅ Orden de fabricación finalizada correctamente: {OF}");
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Logs.RegistrarError($"❌ Error al finalizar OF: {response.StatusCode} - {errorContent}");
                }
            }
        }
    }
}

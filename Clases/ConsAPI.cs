using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using S7.Net;
using ServicioWindows.Models;
using System.Diagnostics;
using System.Text;

namespace ServicioWindows.Clases
{
    internal class ConsAPI
    {
        private readonly Plc PLC;

        private SQLServerManager BBDD_Config()
        {
            string nombreServidor = Environment.MachineName;
            string ServidorSQL = $"{nombreServidor}\\SQLEXPRESS";
            string BaseDatos = "Recetas";
            string Usuario = "sa";
            string Password = "GomezMadrid2021";
            string connectionString = $"Data Source={ServidorSQL};Initial Catalog={BaseDatos};User ID={Usuario};Password={Password};";

            return new SQLServerManager(connectionString);
        }



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
        /*
        public async Task<string> DatosEtapas(string DB, string DB_Offsets, string apiUrl, int NEtapa = 0)
        {
            string Valor = null;
            CommPLC commPLC = new CommPLC(PLC);

            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                
                int NumeroProcesos;
                int NumeroConsignas;

                string responseBody = await response.Content.ReadAsStringAsync();

                //

                //Si el jarray tiene varias recetas te cuenta todas las etapas contadas asi
                JArray jsonArray = JArray.Parse(responseBody);
                int NumeroEtapas = (jsonArray.Count) - 1;

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

        */
        public async Task<string> DatosEtapas(string DB, string DB_Offsets, string apiUrl, int NEtapa = 0)
        {
            string Valor = null;
            CommPLC commPLC = new CommPLC(PLC);

            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            var BBDD = BBDD_Config();

            if (response.IsSuccessStatusCode)
            {
                int NumeroProcesos;
                int NumeroConsignas;

                string responseBody = await response.Content.ReadAsStringAsync();

                // DEBUG: Mostrar el JSON crudo
                //Console.WriteLine("[DEBUG] JSON recibido:");
                //Console.WriteLine(responseBody);

                // Parsear JSON
                JArray jsonArray = JArray.Parse(responseBody);
                int NumeroEtapas = jsonArray.Count;

                // DEBUG: Mostrar número de etapas
                //Console.WriteLine($"[DEBUG] Número de etapas: {NumeroEtapas}");

                JArray Etapa = (JArray)jsonArray[NEtapa];
                NumeroProcesos = Etapa.Count - 1;
                //Console.WriteLine("  ");
                //Console.WriteLine($" esto es mi numero de procesos ++ {NumeroProcesos} ");

                // -- OBTENEMOS LAS CANTIDADES DE CADA UNA DE LAS MATERIAS PRIMAS

                int ordenFabricacion = jsonArray[0][0]["ordenFabricacion"]?.Value<int>() ?? 0;
                string OF = ordenFabricacion.ToString(); // Ya puedes usarla donde la necesitas

                //Console.WriteLine($"[DEBUG OF] -> {OF}");

                DatosCantidades Cantidad_MMPP = await BBDD.ExtraerMMPP_Cantidades(OF);

                //Console.WriteLine("[DEBUG CANTIDADES] -> " + JsonConvert.SerializeObject(Cantidad_MMPP, Formatting.Indented));

                for (int i = 1; i <= NumeroProcesos; i++)
                {
                    JArray Proceso = (JArray)Etapa[i];
                    NumeroConsignas = Proceso.Count;

                    for (int u = 0; u < NumeroConsignas; u++)
                    {
                        JObject ProcesoObj = (JObject)Proceso[u];

                        //DEBUG: Mostrar objeto completo antes de procesar
                        //Console.WriteLine("-----");
                        //Console.WriteLine("");
                        //Console.WriteLine($"[DEBUG] Proceso {i}, Consigna {u}: {ProcesoObj}");
                        //Console.WriteLine("");
                        string MMPP = "";

                        foreach (JProperty property in ProcesoObj.Properties())   
                        {
                            string propertyName = property.Name;
                            string propertyValue = property.Value.ToString();
                            

                            if (propertyName == "tipo")
                            {
                                MMPP = propertyValue;
                            }

                            //Console.WriteLine($"[JS2]Properti name '{propertyName} & MMPP {MMPP}");

                            if (u == 0 && propertyName == "valor")
                            {
                                double propertyValor = 0;

                                try
                                {
                                    propertyValor = property.Value.ToObject<double>();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[WARN] No se pudo convertir el valor de la propiedad '{propertyName}' a double. Valor: {property.Value}. Excepción: {ex.Message}");
                                    continue; // O asigna un valor default como 0 y sigue
                                }

                                float cantidadMMPP = 0;
                                //Console.WriteLine($"[JS2]NAME '{MMPP}' VALOR {property.Value} % U {u}");

                                switch (MMPP)
                                {
                                    case "Carga_Solidos_1":
                                        cantidadMMPP = Cantidad_MMPP.Solido_1;
                                        break;
                                    case "Carga_Solidos_2":
                                        cantidadMMPP = Cantidad_MMPP.Solido_2;
                                        break;
                                    case "Carga_Solidos_3":
                                        cantidadMMPP = Cantidad_MMPP.Solido_3;
                                        break;
                                    case "Carga_Agua_Descal":
                                        cantidadMMPP = Cantidad_MMPP.Agua;
                                        break;
                                    case "Carga_Agua_Recup":
                                        cantidadMMPP = Cantidad_MMPP.AguaRecu;
                                        break;
                                    case "Carga_Antiespumante":
                                        cantidadMMPP = Cantidad_MMPP.Antiespumante;
                                        break;
                                    case "Carga_Ligno":
                                        cantidadMMPP = Cantidad_MMPP.Lignosulfonato;
                                        break;
                                    case "Carga_Potasa":
                                        cantidadMMPP = Cantidad_MMPP.Potasa;
                                        break;
                                    default:
                                        cantidadMMPP = 0;
                                        break;
                                }

                                //Console.WriteLine($"[DEBUG] - {cantidadMMPP}");

                                if (cantidadMMPP > 0)
                                {
                                    propertyValor = (propertyValor / 100) * cantidadMMPP;
                                }

                                propertyValue = propertyValor.ToString();

                            }


                            //Console.WriteLine(" - - - - - - - - - - - - - ");
                            //Console.WriteLine($"Este es el nombre y el valor que quiero mostar ->> {propertyValue}");
                            //Console.WriteLine($"[DEBUG] Nombre: {propertyName} - Valor: {propertyValue}");

                            commPLC.CargaDatosReceta(DB, DB_Offsets, propertyName, propertyValue);
                            //
                            //Console.WriteLine("Valores CArgados Recetas");
                            //Console.WriteLine(" - - - - - - - - - - - - - ");
                        }
                    }
                }
            }
            else
            {
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

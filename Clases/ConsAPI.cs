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

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método privado que configura y devuelve una conexión a la base de datos SQL Server

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

        // ---------------------------------------------------------------------------------------------------------------------------

        public ConsAPI(Plc PLC = null)
        {
            this.PLC = PLC;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Hace una solicitud GET a una API, obtiene un JSON y extrae un valor específico.
        // Recibe la URL de la API y el nombre del dato que se quiere obtener.
        // Devuelve el valor solicitado o null si ocurre un error.

        public async Task<string> DatosCabecera(string apiUrl, string Dato)
        {
            string Valor = null;

            // Crear cliente HTTP para hacer la solicitud
            HttpClient httpClient = new HttpClient();

            // Hacer la petición GET de forma asíncrona a la URL dada
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            // Verificar si la respuesta fue exitosa (status 200-299)
            if (response.IsSuccessStatusCode)
            {
                // Leer el contenido de la respuesta como texto
                string responseBody = await response.Content.ReadAsStringAsync();

                // Parsear el texto a un arreglo JSON (espera que sea un arreglo de arreglos)
                JArray jsonArray = JArray.Parse(responseBody);

                // Obtener el primer objeto JSON del primer arreglo
                JObject Objeto = (JObject)jsonArray[0][0];

                // Extraer el valor del dato solicitado (como string)
                Valor = Objeto.Value<string>(Dato);
            }
            else
            {
                // Mostrar mensaje si la respuesta no fue exitosa
                Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            }

            return Valor;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Realiza una solicitud GET a una API y extrae un dato específico de una etapa indicada.
        // Recibe la URL, el nombre del dato y el índice de la etapa para acceder en el JSON.
        // Devuelve el valor solicitado o null si falla la petición.

        public async Task<string> DatosCabeceraEtapa(string apiUrl, string Dato, int Etapa)
        {
            // Variable donde se guardará el valor extraído
            string Valor = null;

            // Cliente HTTP para hacer la solicitud
            HttpClient httpClient = new HttpClient();

            // Enviar solicitud GET a la URL indicada
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            // Verificar si la respuesta fue exitosa (código 200-299)
            if (response.IsSuccessStatusCode)
            {
                // Leer el contenido de la respuesta como texto
                string responseBody = await response.Content.ReadAsStringAsync();

                // Convertir el texto JSON en un arreglo JSON
                JArray jsonArray = JArray.Parse(responseBody);

                // Navegar dentro del JSON para obtener el objeto deseado usando el índice 'Etapa'
                // Se asume que la estructura del JSON es un arreglo de arreglos donde se accede así: [Etapa][0][0]
                JObject Objeto = (JObject)jsonArray[Etapa][0][0];

                // Extraer el valor del campo especificado por 'Dato' dentro del objeto JSON
                Valor = Objeto.Value<string>(Dato);
            }
            else
            {
                // Si la respuesta no fue exitosa, mostrar el código de error y motivo en consola
                Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            }
            // Devolver el valor extraído (o null si no se pudo obtener)
            return Valor;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método asíncrono que obtiene datos de etapas de un proceso desde una API,
        // procesa la información de materias primas y carga esos datos en un PLC.
        // Parámetros:
        // - DB: nombre o identificador de la base de datos PLC.
        // - DB_Offsets: configuración de offsets para escribir en PLC.
        // - apiUrl: URL de la API desde donde se obtienen los datos JSON.
        // - NEtapa: número de la etapa a procesar (por defecto 0).
        // Retorna un string (actualmente null, podría modificarse para otro uso).

        public async Task<string> DatosEtapas(string DB, string DB_Offsets, string apiUrl, int NEtapa = 0)
        {
            // Variable que podría usarse para devolver algún valor (aquí no se usa)
            string Valor = null;
            // Instancia para comunicación con el PLC
            CommPLC commPLC = new CommPLC(PLC);

            // Cliente para realizar solicitudes HTTP
            HttpClient httpClient = new HttpClient();
            // Realizamos una solicitud GET a la URL de la API
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            // Obtenemos la configuración de la base de datos (clase o método externo)
            var BBDD = BBDD_Config();

            // Si la respuesta HTTP fue exitosa
            if (response.IsSuccessStatusCode)
            {
                int NumeroProcesos;
                int NumeroConsignas;

                // Leer el cuerpo de la respuesta
                string responseBody = await response.Content.ReadAsStringAsync();

                // Convertimos el texto JSON en un arreglo JSON (JArray)
                JArray jsonArray = JArray.Parse(responseBody);
                // Número total de etapas
                int NumeroEtapas = jsonArray.Count;

                // Obtenemos la etapa que queremos procesar según el parámetro NEtapa
                JArray Etapa = (JArray)jsonArray[NEtapa];
                // El número de procesos dentro de esta etapa es la cantidad de elementos menos 1
                NumeroProcesos = Etapa.Count - 1;

                // Extraemos el número de orden de fabricación desde el JSON (primer etapa, primer objeto)
                int ordenFabricacion = jsonArray[0][0]["ordenFabricacion"]?.Value<int>() ?? 0;
                // Convertimos a string para usarlo
                string OF = ordenFabricacion.ToString(); // Ya puedes usarla donde la necesitas


                // Obtenemos las cantidades de materias primas asociadas a la orden de fabricación
                DatosCantidades Cantidad_MMPP = await BBDD.ExtraerMMPP_Cantidades(OF);

                // Iteramos sobre cada proceso dentro de la etapa (comenzando en 1 porque 0 es info general)
                for (int i = 1; i <= NumeroProcesos; i++)
                {
                    // Obtenemos el proceso i
                    JArray Proceso = (JArray)Etapa[i];
                    // Número de consignas dentro del proceso
                    NumeroConsignas = Proceso.Count;

                    // Iteramos sobre cada consigna dentro del proceso
                    for (int u = 0; u < NumeroConsignas; u++)
                    {
                        // Obtenemos el objeto JSON de la consigna
                        JObject ProcesoObj = (JObject)Proceso[u];

                        // Variable para almacenar el tipo de materia prima (MMPP)
                        string MMPP = "";

                        // Recorremos todas las propiedades de la consigna
                        foreach (JProperty property in ProcesoObj.Properties())   
                        {
                            // Nombre de la propiedad
                            string propertyName = property.Name;
                            // Valor en formato string
                            string propertyValue = property.Value.ToString();
                            
                            if (propertyName == "tipo")
                            {
                                // Guardamos el tipo de materia prima
                                MMPP = propertyValue;
                            }

                            // Si estamos en la primera consigna (u == 0) y la propiedad es "valor"
                            if (u == 0 && propertyName == "valor")
                            {
                                double propertyValor = 0;
                                // Intentamos convertir el valor a double
                                try
                                {
                                    propertyValor = property.Value.ToObject<double>();
                                }
                                catch (Exception ex)
                                {
                                    // En caso de error, mostrar advertencia y continuar con el siguiente
                                    Console.WriteLine($"[WARN] No se pudo convertir el valor de la propiedad '{propertyName}' a double. Valor: {property.Value}. Excepción: {ex.Message}");
                                    continue; // Saltar esta iteración
                                }

                                float cantidadMMPP = 0;

                                // Asignamos la cantidad total de materia prima según el tipo (MMPP)
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

                                // Si la cantidad total es mayor que 0, calculamos el valor proporcional
                                if (cantidadMMPP > 0)
                                {
                                    propertyValor = (propertyValor / 100) * cantidadMMPP;
                                }
                                // Convertimos el valor calculado a string para usarlo después
                                propertyValue = propertyValor.ToString();

                            }

                            // Finalmente, cargamos los datos al PLC usando el método correspondiente
                            commPLC.CargaDatosReceta(DB, DB_Offsets, propertyName, propertyValue);
                        }
                    }
                }
            }
            else
            {
                // En caso de error en la solicitud HTTP, mostramos el código y el mensaje
                Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            }
            // Por ahora siempre devuelve null, pero se puede modificar
            return Valor;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método asíncrono que actualiza la etapa actual de una receta enviando datos a una API vía POST.
        // Parámetros:
        // - GenReceta: objeto con información general de la receta (orden de fabricación, nombre de etapa, total de etapas).
        // - EtapaAct: número de la etapa actual.
        // - Logs: objeto para registrar logs (no usado en el código actual).

        public async Task ActualizarEtapaAPI(DatosGenReceta GenReceta, int EtapaAct, Logs Logs)
        {
            // Crear instancia de HttpClient dentro de using para liberar recursos automáticamente
            using (HttpClient httpClient = new HttpClient())
            {
                // Crear un objeto anónimo con los datos que se enviarán a la API
                var data = new
                {
                    OF = GenReceta.OF,                                          // Orden de fabricación
                    nombreEtapa = GenReceta.NombreEtapaActual,                  // Nombre de la etapa actual
                    numeroEtapa = $"{EtapaAct}/{GenReceta.NumEtapas}"           // Formato "actual/total"
                };

                // Convertir el objeto 'data' a JSON
                var json = JsonConvert.SerializeObject(data);
                // Crear contenido HTTP con el JSON y especificar que es tipo "application/json"
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                // URL de la API a la que se enviarán los datos
                string RutaApiAct = "http://localhost:7248/api/Worker/ActualizarOF";
                
                // Enviar una petición POST de manera asíncrona con el contenido JSON
                HttpResponseMessage response = await httpClient.PostAsync(RutaApiAct, content);
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método asíncrono para finalizar una orden de fabricación (OF) mediante una llamada a una API.
        // Parámetros:
        // - OF: número o identificador de la orden de fabricación.
        // - estado: estado final que se quiere asignar a la OF.
        // - Logs: objeto para registrar información o errores durante la ejecución.

        public async Task FinalizarOFAPI(string OF, string estado, Logs Logs)
        {
            // Crear instancia de HttpClient con using para liberar recursos automáticamente
            using (HttpClient httpClient = new HttpClient())
            {
                // Crear un objeto anónimo con los datos que se enviarán a la API
                var data = new 
                { 
                    OF = OF,                // Orden de fabricación a finalizar
                    estado = estado         // Estado final de la orden
                };

                // Serializar el objeto 'data' a formato JSON
                var json = JsonConvert.SerializeObject(data);

                // Crear el contenido HTTP con el JSON, indicando codificación y tipo MIME
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // URL de la API donde se realizará la petición POST para finalizar la OF
                string RutaApiFin = "http://localhost:7248/api/Worker/FinalizarOF";

                // Enviar la petición POST de forma asíncrona y obtener la respuesta
                HttpResponseMessage response = await httpClient.PostAsync(RutaApiFin, content);

                // Si la respuesta es exitosa, registrar la información
                if (response.IsSuccessStatusCode)
                {
                    Logs.RegistrarInfo($"✅ Orden de fabricación finalizada correctamente: {OF}");
                }
                else
                {
                    // En caso de error, obtener el contenido de la respuesta para detalles y registrar error
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Logs.RegistrarError($"❌ Error al finalizar OF: {response.StatusCode} - {errorContent}");
                }
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------
    }
}

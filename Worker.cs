using GestionRecetas.Models;
using S7.Net;
using ServicioWindows.Clases;
using ServicioWindows.Datos;
using ServicioWindows.Models;
using System.Text.Json;

namespace ServicioWindows
{
    // ---------------------------------------------------------------------------------------------------------------------------

    public class DatosMMPP
    {
        public string OF { get; set; }
        public float Solido_1 { get; set; }
        public float Solido_2 { get; set; }
        public float Solido_3 { get; set; }
        public float Agua { get; set; }
        public float AguaRecu { get; set; }
        public float Antiespumante { get; set; }
        public float Lignosulfonato { get; set; }
        public float Potasa { get; set; }
    }

    // ---------------------------------------------------------------------------------------------------------------------------

    public class Worker : BackgroundService, IDisposable
    {
        // Objeto para manejar la conexión y operaciones con la base de datos SQL Server
        private readonly SQLServerManager bbdd;
        // Tiempo en milisegundos que el servicio esperará entre cada ciclo de trabajo (2 segundos)
        private readonly int TiempoCicloServicio = 2000;

        // Fecha y hora de la última vez que se leyó información de MMPP (materias primas), inicializado con el valor mínimo posible
        private DateTime ultimaEjecucionLecturaMMPP = DateTime.MinValue;
        // Tiempo que debe pasar entre cada lectura de MMPP (45 segundos)
        private readonly TimeSpan intervaloLecturaMMPP = TimeSpan.FromSeconds(45);

        // Instancia para manejar datos de autómatas (equipos automáticos)
        private readonly Automatas datos = new Automatas();
        // Instancia que maneja la información total de reactores
        private readonly TotalReactores TotalReactores = new TotalReactores();
        // Objeto con funciones y métodos auxiliares (herramientas útiles)
        private readonly Utiles utiles = new Utiles();
        // Objeto para registrar eventos y mensajes importantes (logs)
        private readonly Logs logs;

        // Fecha y hora de la última vez que se leyó información del sistema SAP, iniciada con el valor mínimo posible
        private DateTime ultimaEjecucionLecturaSAP = DateTime.MinValue;
        // Tiempo que debe pasar entre cada lectura de SAP (4 horas)
        private readonly TimeSpan intervaloLecturaSAP = TimeSpan.FromMinutes(240);

        // Fecha y hora de la última vez que se leyó información de Umbrales, iniciada con el valor mínimo posible
        private DateTime ultimaEjecucionLecturaUmbral = DateTime.MinValue;
        // Tiempo que debe pasar entre cada lectura de Umbral (1 hmin)
        private readonly TimeSpan intervaloLecturaUmbral = TimeSpan.FromMinutes(1);

        // Cliente HTTP para hacer peticiones web, se usa para conectarse a servicios externos
        private static readonly HttpClient client = new HttpClient();

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



            return new SQLServerManager(connectionString );
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Constructor de la clase Worker, se ejecuta cuando se crea un nuevo objeto Worker

        public Worker()
        {
            // Inicializa la base de datos llamando al método BBDD_Config()
            bbdd = BBDD_Config();
            // Define la ruta del archivo donde se guardarán los logs
            string RutaLog = @"C:\Informes\Logs.txt";
            // Crea un nuevo objeto logs para manejar los registros, usando la ruta especificada
            logs = new Logs(RutaLog);
            // Escribe en el log que el servicio se ha iniciado
            logs.Iniciar("Servicio iniciado");
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // 1. Se conecta a los PLCs y reactores para gestionar y monitorear automáticamente las recetas.
        // 2. Cada 45 segundos, actualiza en la base de datos los datos reales de materias primas (MMPP).
        // 3. Cada 4 horas, se conecta con el sistema SAP para obtener órdenes liberadas.

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Obtener la cantidad de PLCs configurados
            int NumeroPLCs = datos.ObtenerPLCs().Count();

            // Crear objeto para obtener información de reactores y contar cuántos hay
            TotalReactores totalReactores = new TotalReactores();
            int NumeroReactores = totalReactores.ObtenerReactores().Count();

            // Crear arreglos para manejar el estado de comunicación, inicio y habilitación de cada PLC
            bool[] FalloComm = new bool[NumeroPLCs];
            bool[] StartUp = new bool[NumeroPLCs];
            bool[] PLC_Enable = new bool[NumeroPLCs];

            // Matriz para guardar la etapa activa de cada PLC y reactor
            short[,] EtapaAct = new short[NumeroPLCs, NumeroReactores];

            // Crear arreglos para manejar objetos PLC y su comunicación
            Plc[] PLC = new Plc[NumeroPLCs];
            CommPLC[] commPLC = new CommPLC[NumeroPLCs];

            // Inicializar los PLCs con su IP y otros datos, además inicializar variables de estado
            for (int i = 0; i < NumeroPLCs; i++)
            {
                string IP = datos.ObtenerPLCs()[i];
                PLC[i] = new Plc(CpuType.S71500, IP, 0, 1);     // Crear objeto PLC
                commPLC[i] = new CommPLC(PLC[i]);               // Crear objeto de comunicación para el PLC
                StartUp[i] = false;                             // Marcar que el PLC aún no ha iniciado
                FalloComm[i] = false;                           // No hay fallo de comunicación inicialmente
                PLC_Enable[i] = true;                           // PLC habilitado inicialmente
            }

            string DB_Offsets = "7999";                         // Parámetro fijo usado en llamadas (offset en DB)
            string RutaApi = "https://192.168.8.2:446/api/Worker/AlgunaLanzada/"; // 7248
            string RutaApiSAP = "https://192.168.8.2:446/api/Liberadas/SAP/FO01"; // SE HA QUITADO EL CENTRO, YA UQE LO PONE POR DEFECTO
            //string RutaApiSAP_Front = "https://192.168.8.2/api/Liberadas/FO01";

            // Bucle principal que se ejecuta hasta que se solicite detener el servicio
            while (!stoppingToken.IsCancellationRequested) 
            {
                //logs.RegistrarInfo("[DEBUG 0]");
                // Recorrer todos los PLCs para verificar comunicación y actualizar estados
                for (int i = 0; i < NumeroPLCs; i++)
                {
                    string IP = datos.ObtenerPLCs()[i];
                    //logs.RegistrarInfo("[DEBUG 1]");
                    // Verificar si el PLC está disponible (encendido y accesible)
                    PLC_Enable[i] = utiles.DisponibilidadPLC(logs, IP, PLC_Enable[i]);

                    if (PLC_Enable[i])
                    {
                        // Intentar iniciar conexión si no estaba conectada
                        StartUp[i] = commPLC[i].InicioConexion(logs, StartUp[i]);
                        // Verificar si hay fallo de comunicación
                        FalloComm[i] = commPLC[i].FalloCom(logs, FalloComm[i]);
                        //logs.RegistrarInfo("[DEBUG 2]");
                        // Si la conexión con el PLC está activa
                        if (PLC[i].IsConnected)
                        {
                            try
                            {
                                // Para cada reactor asociado a este PLC
                                for (int u = 0; u < NumeroReactores; u++)
                                {
                                    //logs.RegistrarInfo("[DEBUG 3]");
                                    // Verificar si el reactor está conectado al PLC por IP
                                    if (TotalReactores.ObtenerReactores()[u][0] == IP)
                                    {
                                        //logs.RegistrarInfo("[DEBUG 4]");
                                        string NombreReactor = totalReactores.ObtenerReactores()[u][1];
                                        string DB_Reactor = totalReactores.ObtenerReactores()[u][2];

                                        // Actualizar la etapa activa del reactor consultando el PLC
                                        EtapaAct[i, u] = await commPLC[i].GestorReceta(DB_Reactor, DB_Offsets, NombreReactor, EtapaAct[i, u], RutaApi, logs);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                // Mostrar error por consola
                                Console.WriteLine(e);
                                // Marcar fallo de comunicación y continuar con siguiente PLC
                                FalloComm[i] = commPLC[i].FalloCom(logs, FalloComm[i], StartUp[i], true);
                                continue;
                            }
                        }
                        else
                        {
                            // Si no está conectado, marcar fallo de comunicación
                            FalloComm[i] = commPLC[i].FalloCom(logs, FalloComm[i], StartUp[i], true);
                        }
                    }
                }
                // Cada 45 segundos (intervaloLecturaMMPP) actualizar datos de materias primas (MMPP)
                if (DateTime.Now - ultimaEjecucionLecturaMMPP >= intervaloLecturaMMPP)
                {
                    //Console.WriteLine("MMPP -1");
                    ultimaEjecucionLecturaMMPP = DateTime.Now;

                    try
                    {
                        // Bases de datos para materias primas
                        string[] DBs = {"8500","8501","8502","8503" ,"8504"};
                        //string[] DBs = { "8500" };

                        for (int index = 0; index < DBs.Length; index++)
                        {
                            string db = DBs[index];
                            // Obtener datos reales desde PLC (materias primas)
                            string resultado = await commPLC[0].CargaDatosRealesMMPP(db);

                            // Mapear DB a nombre de destino
                            string destino = db switch
                            {
                                "8500" => "RC01",
                                "8501" => "RC02",
                                "8502" => "RC03",
                                "8503" => "IM01",
                                "8504" => "IM02",
                                _ => "Desconocido"
                            };

                            // Convertir el resultado JSON a objeto DatosMMPP
                            DatosMMPP datos = JsonSerializer.Deserialize<DatosMMPP>(resultado);
                            //logs.RegistrarError($"Este es el destino: {destino}");
                            //Console.WriteLine($"Este es el destino: {destino}");

                            if (datos != null)
                            {
                                // Actualizar la orden de fabricación y cantidades en la base de datos
                                await bbdd.ActualizarOrdenFabricacionMMPP( destino, datos.OF);
                                await bbdd.ActualizaCantidadMMPP(destino, datos.Solido_1, datos.Solido_2, datos.Solido_3,
                                                                 datos.Agua, datos.AguaRecu, datos.Antiespumante,
                                                                 datos.Lignosulfonato, datos.Potasa);
                            }

                            //Console.WriteLine($"[MMPP] {DateTime.Now}: DB={db} Destino={destino} Datos: {resultado}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error en CargaDatosRealesMMPP: {ex.Message}");
                        logs.RegistrarError("Error en CargaDatosRealesMMPP...");
                    }
                }
                
                // Cada 4 horas (intervaloLecturaSAP) hacer lectura y actualización con SAP
                if (DateTime.Now - ultimaEjecucionLecturaSAP >= intervaloLecturaSAP)
                {
                    logs.RegistrarInfo("Inicio LecturaSap");
                    ultimaEjecucionLecturaSAP = DateTime.Now;
                    try
                    {
                        // Llamar a la API para obtener datos de SAP
                        HttpResponseMessage response = await client.GetAsync(RutaApiSAP);

                        if (response.IsSuccessStatusCode)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"✅ Datos recibidos de SAP: {result.Substring(0, Math.Min(200, result.Length))}...");
                            logs.RegistrarInfo("Datos Recibidos des SAP");
                            logs.RegistrarInfo($"{result.Substring(0, Math.Min(200, result.Length))}");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Error al llamar API SAP: {response.StatusCode}");
                            logs.RegistrarError("Error al llamar API SAP");
                            logs.RegistrarInfo($"{response.StatusCode}");

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error en CargaDatosRealesSAP: {ex.Message}");
                        logs.RegistrarError("Error en CargaDatos SAP");

                    }
                }
                // Cada 60 segundos (intervaloLecturaUmbral) actualizar datos de los Umbrales 
                if (DateTime.Now - ultimaEjecucionLecturaUmbral >= intervaloLecturaUmbral)
                {
                    ultimaEjecucionLecturaUmbral = DateTime.Now;

                    try
                    {
                        // Bases de datos para materias primas
                        string db_umbral = "8750";

                        // Obtener datos umbrales
                        UmbralesRequest datos = await bbdd.ObtenerUmbrales();



                        if (datos != null)
                        {
                            commPLC[0].EnviarDatosRealesUmbral(db_umbral, datos, logs);
                        } 
                        else
                        {
                            logs.RegistrarError("No se encontraron datos de umbrales en la base de datos");
                            Console.WriteLine("⚠ No se encontraron datos de umbrales en la base de datos.");
                        }

                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine($"⚠️ Error en CargaDatosUmbrales: {ex.Message}");
                        logs.RegistrarError("Error en CargaDatosUmbrales: {ex.Message}");
                    }
                }
                // Esperar un tiempo antes de la siguiente iteración del ciclo
                await Task.Delay(TiempoCicloServicio, stoppingToken);
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método para liberar recursos cuando ya no se necesita este objeto
        // Aquí se cierra o libera la conexión con la base de datos si existe

        public void Dispose()
        {
            // Si 'bbdd' no es null, se llama a su método Dispose para liberar recursos
            bbdd?.Dispose();
        }

        // ---------------------------------------------------------------------------------------------------------------------------
    }

}

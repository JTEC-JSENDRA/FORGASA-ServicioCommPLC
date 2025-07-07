using S7.Net;
using ServicioWindows.Clases;
using ServicioWindows.Datos;
using ServicioWindows.Models;
using System.Text.Json;

namespace ServicioWindows
{
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

    public class Worker : BackgroundService, IDisposable
    {
        private readonly SQLServerManager bbdd;
        private readonly int TiempoCicloServicio = 2000;
        private DateTime ultimaEjecucionLecturaMMPP = DateTime.MinValue;
        private readonly TimeSpan intervaloLecturaMMPP = TimeSpan.FromSeconds(45);
        private readonly Automatas datos = new Automatas();
        private readonly TotalReactores TotalReactores = new TotalReactores();
        private readonly Utiles utiles = new Utiles();
        private readonly Logs logs;

        private DateTime ultimaEjecucionLecturaSAP = DateTime.MinValue;
        private readonly TimeSpan intervaloLecturaSAP = TimeSpan.FromMinutes(240);

        //private DateTime ultimaEjecucionLecturaFront = DateTime.MinValue;
        //private TimeSpan intervaloLecturaFront = TimeSpan.FromSeconds(60);

        private static readonly HttpClient client = new HttpClient();

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

        public Worker()
        {
            bbdd = BBDD_Config();
            string RutaLog = @"C:\Informes\Logs.txt";
            logs = new Logs(RutaLog);
            logs.Iniciar("Servicio iniciado");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int NumeroPLCs = datos.ObtenerPLCs().Count();

            TotalReactores totalReactores = new TotalReactores();
            int NumeroReactores = totalReactores.ObtenerReactores().Count();

            //int NumeroReactores = TotalReactores.ObtenerReactores().Count();

            //Console.WriteLine("Numero total de reactores: " + NumeroReactores);

            bool[] FalloComm = new bool[NumeroPLCs];
            bool[] StartUp = new bool[NumeroPLCs];
            bool[] PLC_Enable = new bool[NumeroPLCs];
            short[,] EtapaAct = new short[NumeroPLCs, NumeroReactores];

            Plc[] PLC = new Plc[NumeroPLCs];
            CommPLC[] commPLC = new CommPLC[NumeroPLCs];

            //// Inicialización PLCs
            for (int i = 0; i < NumeroPLCs; i++)
            {
                string IP = datos.ObtenerPLCs()[i];
                PLC[i] = new Plc(CpuType.S71500, IP, 0, 1);
                commPLC[i] = new CommPLC(PLC[i]);
                StartUp[i] = false;
                FalloComm[i] = false;
                PLC_Enable[i] = true;

            }

            string DB_Offsets = "7999";
            string RutaApi = "http://localhost:7248/api/Worker/AlgunaLanzada/";
            string RutaApiSAP = "http://localhost:7248/api/Liberadas/SAP/FO01";
            //string RutaApiSAP_Front = "http://localhost:7248/api/Liberadas/FO01";

            while (!stoppingToken.IsCancellationRequested) 
            {
                for (int i = 0; i < NumeroPLCs; i++)
                {
                    string IP = datos.ObtenerPLCs()[i];
                    PLC_Enable[i] = utiles.DisponibilidadPLC(logs, IP, PLC_Enable[i]);

                    if (PLC_Enable[i])
                    {
                        StartUp[i] = commPLC[i].InicioConexion(logs, StartUp[i]);
                        FalloComm[i] = commPLC[i].FalloCom(logs, FalloComm[i]);

                        //Console.WriteLine($"[Debug] -- NUMERO REACTORES: {NumeroReactores}");

                        if (PLC[i].IsConnected)
                        {
                            try
                            {
                                for (int u = 0; u < NumeroReactores; u++)
                                {
                                    if (TotalReactores.ObtenerReactores()[u][0] == IP)
                                    {

                                        //string NombreReactor = TotalReactores.ObtenerReactores()[u][1];
                                        //string DB_Reactor = TotalReactores.ObtenerReactores()[u][2];

                                        string NombreReactor = totalReactores.ObtenerReactores()[u][1];
                                        string DB_Reactor = totalReactores.ObtenerReactores()[u][2];

                                        //Console.WriteLine($"[Debug] -- Nombre y DB Reactor: {NombreReactor} & {DB_Reactor}");
                                        EtapaAct[i, u] = await commPLC[i].GestorReceta(DB_Reactor, DB_Offsets, NombreReactor, EtapaAct[i, u], RutaApi, logs);

                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                FalloComm[i] = commPLC[i].FalloCom(logs, FalloComm[i], StartUp[i], true);
                                continue;
                            }
                        }
                        else
                        {
                            FalloComm[i] = commPLC[i].FalloCom(logs, FalloComm[i], StartUp[i], true);
                        }
                    }
                }


                // Cada 300 segundos (5 min) hacemos la lectura y actualización
                if (DateTime.Now - ultimaEjecucionLecturaMMPP >= intervaloLecturaMMPP)
                {
                    ultimaEjecucionLecturaMMPP = DateTime.Now;

                    try
                    {
                        // poner DB's cuando esten configurados
                        string[] DBs = {"8500","8501","8502","8503" ,"8504"};

                        for (int index = 0; index < DBs.Length; index++)
                        {

                            string db = DBs[index];
                            //Console.WriteLine($"[Debug -> CD_MMPP]-> {db}");
                            string resultado = await commPLC[0].CargaDatosRealesMMPP(db);

                            

                            string destino = db switch
                            {
                                "8500" => "RC01",
                                "8501" => "RC02",
                                "8502" => "RC03",
                                "8503" => "IM01",
                                "8504" => "IM02",
                                _ => "Desconocido"
                            };

                            DatosMMPP datos = JsonSerializer.Deserialize<DatosMMPP>(resultado);

                            //SQLServerManager BBDD = BBDD_Config();

                            if (datos != null)
                            {
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
                    }
                }

                // Cada 240 minutos (4 h) hacemos la lectura y actualización
                if (DateTime.Now - ultimaEjecucionLecturaSAP >= intervaloLecturaSAP)
                {
                    ultimaEjecucionLecturaSAP = DateTime.Now;

                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(RutaApiSAP);

                        if (response.IsSuccessStatusCode)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"✅ Datos recibidos de SAP: {result.Substring(0, Math.Min(200, result.Length))}...");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Error al llamar API SAP: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error en CargaDatosRealesSAP: {ex.Message}");
                    }
                }


                await Task.Delay(TiempoCicloServicio, stoppingToken);
            }
        }

        public void Dispose()
        {
            bbdd?.Dispose();
        }

    }

}

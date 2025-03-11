using S7.Net;
using ServicioWindows.Clases;
using ServicioWindows.Datos;





namespace ServicioWindows
{
    public class Worker : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int TiempoCicloServicio = 1000;
            string DB_Offsets = "7999";
            string RutaApi = "https://localhost:7106/api/Recetas/";

            string RutaLog = $"C:\\Informes\\Logs.txt";
            Logs Logs = new Logs(RutaLog);
            Utiles Utiles = new Utiles();

            Automatas datos = new Automatas();
            TotalReactores TotalReactores = new TotalReactores();

            int NumeroPLCs = datos.ObtenerPLCs().Count();
            int NumeroReactores = TotalReactores.ObtenerReactores().Count();

            bool[] FalloComm = new bool[NumeroPLCs];
            bool [] StartUp = new bool[NumeroPLCs];
            bool [] PLC_Enable = new bool[NumeroPLCs];
            short [,] EtapaAct = new short[NumeroPLCs, NumeroReactores];

            Plc [] PLC = new Plc[NumeroPLCs];
            CommPLC[] commPLC = new CommPLC[NumeroPLCs];

            //Inicializacion de objetos y variables segun el numero de PLCs
            for (int i = 0; i <= (NumeroPLCs - 1); i++)
            {
                string IP = datos.ObtenerPLCs()[i];
                PLC[i] = new Plc(CpuType.S71500, IP, 0, 1);
                commPLC[i] = new CommPLC(PLC[i]);
                StartUp[i] = false;
                FalloComm[i] = false;
                PLC_Enable[i] = true;
            }

            Logs.Iniciar("Servicio iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                
                for (int i = 0; i <= (NumeroPLCs - 1); i++)
                {
                    //Se comprueba la disponibilidad del PLC seleccionado de la lista
                    string IP = datos.ObtenerPLCs()[i];
                    PLC_Enable[i] = Utiles.DisponibilidadPLC(Logs, IP, PLC_Enable[i]);

                    if (PLC_Enable[i])
                    {
                        //Se gestiona la secuencia de comunicacion con el PLC seleccionado de la lista
                        StartUp[i] = commPLC[i].InicioConexion(Logs, StartUp[i]);
                        FalloComm[i] = commPLC[i].FalloCom(Logs, FalloComm[i]);

                        if (PLC[i].IsConnected)
                        {
                            try
                            {
                                //Se comprueba el numero total de reactores que tiene receta
                                for (int u = 0; u <= (NumeroReactores - 1); u++)
                                {
                                    //Se seleccionan los reactores que se encuentran en el PLC seleccionado de la lista
                                    if (TotalReactores.ObtenerReactores()[u][0] == IP)
                                    {
                                        string NombreReactor = TotalReactores.ObtenerReactores()[u][1];
                                        string DB_Reactor = TotalReactores.ObtenerReactores()[u][2];

                                        EtapaAct[i, u] = await commPLC[i].GestorReceta(DB_Reactor, DB_Offsets, NombreReactor, EtapaAct[i, u], RutaApi, Logs);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                FalloComm[i] = commPLC[i].FalloCom(Logs, FalloComm[i], StartUp[i], true);
                                continue;
                            }
                        }
                        else
                        {
                            FalloComm[i] = commPLC[i].FalloCom(Logs, FalloComm[i], StartUp[i], true);

                        }
                    }

                }


                //Pruebas consumo API
                #region Pruebas consumo API

               
                
                //string NombreReceta = await DatosAPI.DatosCabecera("https://localhost:7106/api/Recetas/Crema1", "nombreReceta");
                //string NumeroEtapas = await DatosAPI.DatosCabecera("https://localhost:7106/api/Recetas/Crema1", "numeroEtapas");

                //Console.WriteLine($"El nombre de la receta es: {NombreReceta}");
                //Console.WriteLine($"El numero de etapas son: {NumeroEtapas}");




                #endregion



                await Task.Delay(TiempoCicloServicio, stoppingToken);
            }
        }
    }
}

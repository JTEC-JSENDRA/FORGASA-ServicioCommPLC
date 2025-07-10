using S7.Net;
using System.Text;
using ServicioWindows.Models;
using Newtonsoft.Json;
using System;
using System.Text.Json;
using System.Globalization;
using ServicioWindows.Models;
using System.Runtime.InteropServices;

namespace ServicioWindows.Clases
{
    internal class CommPLC
    {
        private readonly Plc PLC;
        private readonly Utiles Util = new Utiles();
        private string Tipo;
        private string Consigna;
        private int Puntero;
        private int Offset;

        public CommPLC(Plc PLC)
        {
            this.PLC = PLC;
        }

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

        #region Escrituras
        public void WriteINT(string NumeroDB, string DireccionAbs, short Valor)
        {
            string Direccion = DireccionAbs.Split('.')[0];
            PLC.Write($"DB{NumeroDB}.DBD{Direccion}", Valor);
        }
        public void WriteDINT(string NumeroDB, string DireccionAbs, int Valor)
        {
            string Direccion = DireccionAbs.Split('.')[0];
            PLC.Write($"DB{NumeroDB}.DBD{Direccion}", Valor);
        }
        public void WriteFLOAT(string NumeroDB, string DireccionAbs, double valor)
        {
            string Direccion = DireccionAbs.Split('.')[0];
            int intValue = BitConverter.ToInt32(BitConverter.GetBytes((float)valor), 0);
            uint rawValue = (uint)intValue;
            PLC.Write($"DB{NumeroDB}.DBD{Direccion}", rawValue);
        }
        public void WriteSTRING(string NumeroDB, string Direccion, string Valor, Logs Logs)
        {
            int DB = Convert.ToInt32(NumeroDB);
            int Offset = Convert.ToInt32(Direccion.Split('.')[0]);

            // Obtener la longitud de la cadena a escribir
            int LongString = Valor.Length;

            // Se Obtiene la logitud de la variable declarada en el PLC
            int LongStringPLC = (byte)PLC.Read(DataType.DataBlock, DB, (Offset), VarType.Byte, 1);

            // Se comprueba que la variable que se quiere escribir, cabe dentro de la variable de destino
            if (LongString <= LongStringPLC)
            {
                // Convertir la cadena a un arreglo de bytes
                byte[] stringBytes = Encoding.ASCII.GetBytes(Valor);

                // Escribir la longitud de la cadena en el PLC
                PLC.Write(DataType.DataBlock, DB, Offset + 1, (byte)LongString);

                // Escribir la cadena en el PLC
                PLC.Write(DataType.DataBlock, DB, Offset + 2, stringBytes);
            }
            else
            {
                Logs.RegistrarError($"La variable con valor: '{Valor}' y con longitud de {LongString} caracteres, no cabe en la variable de destino con longitud de {LongStringPLC} caracteres.");
            }

        }
        public void WriteBOOL(string NumeroDB, string Direccion, bool Valor)
        {
            try
            {
               // Console.WriteLine($"[INFO] Intentando escribir BOOL en DB{NumeroDB}.DBX{Direccion} con valor {Valor}");
                PLC.Write($"DB{NumeroDB}.DBX{Direccion}", Valor);
                //Console.WriteLine($"[OK] Escritura exitosa en DB{NumeroDB}.DBX{Direccion}");
            }
            catch (Exception ex)
            {
               // Console.WriteLine($"[ERROR] Fallo al escribir en DB{NumeroDB}.DBX{Direccion}: {ex.Message}");
                throw;
            }
        }
        #endregion

        // ---------------------------------------------------------------------------------------------------------------------------

        #region Lecturas
        public int ReadDINT(string NumeroDB, string Direccion)
        {
            return Convert.ToInt32(PLC.Read($"DB{NumeroDB}.DBD{Direccion}"));
        }
        public short ReadINT(string NumeroDB, string Direccion)
        {
            return Convert.ToInt16(PLC.Read($"DB{NumeroDB}.DBW{Direccion}"));
        }
        public string ReadSTRING(string NumeroDB, string Direccion)
        {
            int DB = Convert.ToInt32(NumeroDB);
            int Offset = Convert.ToInt32(Direccion.Split('.')[0]);
            int LongString;

            LongString = (byte)PLC.Read(DataType.DataBlock, DB, (Offset + 1), VarType.Byte, 1);

            return (string)PLC.Read(DataType.DataBlock, DB, (Offset + 2), VarType.String, LongString);
        }
        public bool ReadBOOL(string NumeroDB, string Direccion)
        {
            return (bool)PLC.Read($"DB{NumeroDB}.DBX{Direccion}");
        }
        public double ReadFLOAT(string NumeroDB, string Direccion)
        {
            uint ValorLeido = (uint)PLC.Read($"DB{NumeroDB}.DBD{Direccion}");
            double ValorReal = BitConverter.Int32BitsToSingle((int)ValorLeido);
            double Valor = Math.Round(ValorReal, 5);
            return Valor;
        }

        #endregion

        // ---------------------------------------------------------------------------------------------------------------------------

        #region Gestion Comunicacion con el PLC
        public bool InicioConexion(Logs Logs, bool Arranque)
        {
            string IP_PLC = PLC.IP;
            if (!Arranque)
            {
                try
                {
                    Logs.RegistrarInfo($"Conectando con el PLC: {IP_PLC}");
                    PLC.Open();
                    if (PLC.IsConnected)
                    {
                        Logs.RegistrarInfo($"Se ha establecido la conexion con el PLC: {IP_PLC}");
                        Arranque = true;
                    }
                    else
                    {
                        Logs.RegistrarError($"Se esta intentando la conexion con el PLC: {IP_PLC}");
                    }
                }
                catch (Exception)
                {
                    Logs.RegistrarError($"No se encuentra el PLC: {IP_PLC}");
                }
            }
            return Arranque;
        }

        public bool FalloCom(Logs Logs, bool Fallo, bool Arranque = false, bool Fallo2 = false)
        {
            string IP_PLC = PLC.IP;

            if (Fallo)
            {
                try
                {
                    Logs.RegistrarInfo($"Conectando con el PLC: {IP_PLC}");
                    PLC.Open();
                    if (PLC.IsConnected)
                    {
                        Logs.RegistrarInfo($"Se ha restablecido la comunicación con el PLC: {IP_PLC}");
                        Fallo = false;
                    }
                    else
                    {
                        Logs.RegistrarInfo($"Se esta restableciendo la comunicación con el PLC: {IP_PLC}");
                    }
                }
                catch (Exception)
                {
                    Logs.RegistrarError($"No se encuentra el PLC: {IP_PLC}");
                }
            }
            if (Fallo2 && !Fallo && Arranque)
            {
                Logs.RegistrarError($"Se ha perdido la comunicacion con el PLC {IP_PLC} y no se han podido obtener/cargar los datos");
                Fallo = true;
                PLC.Close();
            }

            return Fallo;
        }
        #endregion

        // ---------------------------------------------------------------------------------------------------------------------------

        #region Metodos Receta

        // Gestiona las etapas de una receta en un reactor conectándose a un PLC y a una API.
        // Controla inicio, avance, finalización y sincronización de datos entre PLC y servicio.
        // Actualiza datos en base de datos y limpia datos tras finalizar.
        
        public async Task<short> GestorReceta(string DB, string DB_Offsets, string NombreReactor, short EtapaAct, string RutaApi, Logs Logs)
        {
            // Instancia para consumir API y almacenar datos de la receta
            ConsAPI DatosAPI = new ConsAPI(PLC);
            DatosGenReceta GenReceta = new DatosGenReceta();

            // Leer offsets para localizar la estructura de datos dentro del DB del PLC
            short OffsetReceta = ReadINT(DB_Offsets, "0.0");
            short LongUDT = ReadINT(DB_Offsets, "2.0");

            // Calcular posiciones específicas para cada dato en el DB según offset base
            string OffsetCheckRecetaInicio = (OffsetReceta + 0).ToString();                 // 118.0 - Primer valor de chequeo
            string OffsetNumEtapas = (OffsetReceta + 2).ToString();                         // 120.0 - Número total de etapas
            string OffsetEtapaAct = (OffsetReceta + 4).ToString();                          // 122.0 - Etapa actual
            string OffsetOF = (OffsetReceta + 6).ToString();                                // 124.0 - Orden de fabricación
            string OffsetNombreReceta = (OffsetReceta + 38).ToString();                     // 156.0 - Nombre de la receta
            string OffsetNombreEtapa = (OffsetReceta + 140).ToString();                     // 258.0 - Nombre de la etapa actual
            string OffsetComm = (OffsetReceta + 172).ToString();                            // 290.0 - Bits de comunicación
            string OffsetCheckRecetaFin = (OffsetReceta + (LongUDT - 2)).ToString();        // 464.0 - Valor de chequeo final
            string OffsetAccionScada = (466).ToString();                                    // Acción específica SCADA

            // Leer estado actual desde el DB del PLC
            EtapaAct = ReadINT(DB, $"{OffsetEtapaAct}.0");
            string NomEtapaAct = ReadSTRING(DB, $"{OffsetNombreEtapa}");

            // Construir URL para consumir API específico del reactor
            string RutaConsumoAPI = $"{RutaApi}{NombreReactor}";
            string estadoFinalizada;
           
            // Leer bits de comunicación para sincronización entre PLC y API
            bool InicioReceta = ReadBOOL(DB, $"{OffsetComm}.0");                            // 290.0
            bool InicioEtapa = ReadBOOL(DB, $"{OffsetComm}.1");                             // 290.1
            bool EtapaActualCargada = ReadBOOL(DB, $"{OffsetComm}.2");                      // 290.2
            bool InicioCargado = ReadBOOL(DB, $"{OffsetComm}.3");                           // 290.3
            bool SiguienteEtapaRecibida = ReadBOOL(DB, $"{OffsetComm}.4");                  // 290.4
            bool UltimaEtapa = ReadBOOL(DB, $"{OffsetComm}.5");                             // 290.5

            // Se obtienen los valores correspondientes para la comprobacion del Offset y la longitud de la UDT de los datos de la receta en el DB del PLC seleciconado
            // Leer valores de chequeo para validar estructura de la receta en el DB
            short CheckRecetaInicio = ReadINT(DB, $"{OffsetCheckRecetaInicio}.0");
            short CheckRecetaFin = ReadINT(DB, $"{OffsetCheckRecetaFin}.0");

            // Validar los valores de chequeo (deben ser 32767 para indicar estructura correcta)
            if (CheckRecetaInicio != 32767 || CheckRecetaFin != 32767)
            {
                // Si hay error en chequeo de inicio, activar flag y registrar error
                if (CheckRecetaInicio != 32767)
                {
                    WriteBOOL(DB_Offsets, "4.0", true);
                    Logs.RegistrarError($"El offset de los datos de la receta en el DB: {DB}, no es correcto. Revisar el valor de la primera variable en el DB: {DB_Offsets}");
                }
                else
                {
                    WriteBOOL(DB_Offsets, "4.0", false);

                    // Si hay error en chequeo de fin, activar otro flag y registrar error
                    if (CheckRecetaFin != 32767)
                    {
                        WriteBOOL(DB_Offsets, "4.1", true);
                        Logs.RegistrarError($"La longitud de la UDT de la receta en el DB: {DB}, no es correcta. Revisar el valor de la segunda variable en el DB: {DB_Offsets}");
                    }
                }
            }
            else
            {
                // Si no hay errores de chequeo, desactivar flags de error
                WriteBOOL(DB_Offsets, "4.0", false);
                WriteBOOL(DB_Offsets, "4.1", false);

                // Si no se ha iniciado la receta en PLC, consultar API para iniciar
                if (!InicioReceta)
                {
                    HttpClient httpClient = new HttpClient();
                    HttpResponseMessage response = await httpClient.GetAsync(RutaConsumoAPI);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        
                        // Si API indica que hay receta, preparar inicio en PLC
                        if (responseBody != "false")
                        {
                            GenReceta.OF = ReadSTRING(DB, $"{OffsetOF}.0");
                            if (GenReceta.OF == null)
                            {
                                // Flag para inicio receta
                                WriteBOOL(DB, $"{OffsetAccionScada}.0", true);
                            }
                        }
                    }
                }
                // Si receta está iniciada pero no cargada en PLC, cargar datos desde API y escribir en PLC
                if (InicioReceta && !InicioCargado)
                {
                    EtapaAct = 0;
                    GenReceta.OF = (await (DatosAPI.DatosCabecera(RutaConsumoAPI, "ordenFabricacion")));
                    GenReceta.NombreReceta = (await (DatosAPI.DatosCabecera(RutaConsumoAPI, "nombreReceta")));
                    GenReceta.NumEtapas = Int16.Parse(await (DatosAPI.DatosCabecera(RutaConsumoAPI, "numeroEtapas")));
                    GenReceta.NombreEtapaSiguiente = await (DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct + 1));

                    // Escribir datos leídos en PLC
                    WriteSTRING(DB, $"{OffsetOF}.0", GenReceta.OF, Logs);
                    WriteSTRING(DB, $"{OffsetNombreReceta}.0", GenReceta.NombreReceta, Logs);
                    WriteINT(DB, $"{OffsetNumEtapas}.0", GenReceta.NumEtapas);
                    WriteSTRING(DB, $"{OffsetNombreEtapa}.0", GenReceta.NombreEtapaSiguiente, Logs);

                    // Cargar datos de la primera etapa desde API
                    await DatosAPI.DatosEtapas(DB, DB_Offsets, RutaConsumoAPI, EtapaAct + 1);

                    // Flag de inicio cargado
                    WriteBOOL(DB, $"{OffsetComm}.3", true); //BOOL Inicio Cargado
                    Logs.RegistrarInfo($"Inicio de la receta en el reactor: {NombreReactor}");
                }

                // Si la etapa actual está cargada pero no iniciada, avanzar a la siguiente etapa
                if (EtapaActualCargada && !InicioEtapa)
                {
                    // Incrementar etapa
                    EtapaAct++;
                    // Actualizar en PLC
                    WriteINT(DB, $"{OffsetEtapaAct}.0", EtapaAct);
                    WriteBOOL(DB, $"{OffsetComm}.3", true);
                                        
                    GenReceta.NumEtapas = ReadINT(DB, $"{OffsetNumEtapas}.0");
                    GenReceta.OF = ReadSTRING(DB, $"{OffsetOF}");

                    // Leer nombre actualizado de etapa desde API y escribirlo en PLC
                    GenReceta.NombreEtapaActual = await DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct);
                    WriteSTRING(DB, $"{OffsetNombreEtapa}.0", GenReceta.NombreEtapaActual, Logs);

                    // Si se alcanzó la última etapa, marcarla en PLC y registrar log
                    if (EtapaAct >= GenReceta.NumEtapas)
                    {
                        // Flag última etapa
                        WriteBOOL(DB, $"{OffsetComm}.5", true); //BOOL Ultima Etapa
                        Logs.RegistrarInfo($"Ultima etapa de la receta en el reactor: {NombreReactor}");
                    }
                    else
                    {
                        // Sino, cargar siguiente etapa desde API y escribir en PLC
                        GenReceta.NombreEtapaSiguiente = await (DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct));
                        WriteSTRING(DB, $"{OffsetNombreEtapa}.0", GenReceta.NombreEtapaSiguiente, Logs);
                        
                        await DatosAPI.DatosEtapas(DB, DB_Offsets, RutaConsumoAPI, EtapaAct + 1);
                    }

                    WriteBOOL(DB, $"{OffsetComm}.4", true);
                    Logs.RegistrarInfo($"📤 Enviando etapa a API - OF: {GenReceta.OF}, EtapaAct: {EtapaAct}, NombreEtapaActual: '{GenReceta.NombreEtapaActual}', NombreEtapaSiguiente: '{GenReceta.NombreEtapaSiguiente}'");
                    DatosAPI.ActualizarEtapaAPI(GenReceta, EtapaAct, Logs);
                }

                // Si siguiente etapa recibida y no es la última, verificar estado y marcar si es última
                if (SiguienteEtapaRecibida && !UltimaEtapa)
                {
                    GenReceta.NumEtapas = ReadINT(DB, $"{OffsetNumEtapas}.0");

                    if (EtapaAct >= GenReceta.NumEtapas)
                    {
                        // Flag última etapa
                        WriteBOOL(DB, $"{OffsetComm}.5", true); //BOOL Ultima Etapa
                    }
                }

                // Si la etapa actual indica que la receta terminó o fue abortada
                if ((NomEtapaAct == "Receta Finalizada") || (NomEtapaAct == "Receta Abortada"))
                {
                    if (NomEtapaAct == "Receta Finalizada")
                    {
                        estadoFinalizada = "Finalizada";
                    }
                    else
                    {
                        estadoFinalizada = "Abortada";
                    }
                    GenReceta.OF = ReadSTRING(DB, $"{OffsetOF}");

                    // Leer datos reales desde PLC y teóricos desde base de datos para comparar
                    var bbdd = BBDD_Config();
                    string destino = DB switch
                    {
                        "8000" => "8500",
                        "8001" => "8501",
                        "8002" => "8502",
                        "8003" => "8503",
                        "8004" => "8504",
                        _ => "Desconocido"
                    };

                    // LEEMOS DATOS PLC
                    string resultado = await CargaDatosRealesMMPP(destino);
                    DatosMMPP datos_MMPP_Finales = System.Text.Json.JsonSerializer.Deserialize<DatosMMPP>(resultado);

                    // LEEMOS DATOS TEORICOS DE LA BBDD
                    string resultado_teorico = await bbdd.ExtraerMMPP_Teoricas(datos_MMPP_Finales.OF);
                    DatosMMPP datos_MMPP_Teoricos = System.Text.Json.JsonSerializer.Deserialize<DatosMMPP>(resultado_teorico);

                    // Actualizar datos finales en base de datos
                    if (datos_MMPP_Finales != null)
                    {
                        await bbdd.ActualizarOrdenFabricacionMMPP(destino, datos_MMPP_Finales.OF);
                        await bbdd.ActualizaCantidadMMPP(destino, datos_MMPP_Finales.Solido_1, datos_MMPP_Finales.Solido_2, datos_MMPP_Finales.Solido_3,
                                                         datos_MMPP_Finales.Agua, datos_MMPP_Finales.AguaRecu, datos_MMPP_Finales.Antiespumante,
                                                         datos_MMPP_Finales.Lignosulfonato, datos_MMPP_Finales.Potasa);
                    }

                    DatosAPI.FinalizarOFAPI(GenReceta.OF, estadoFinalizada, Logs);

                    // ESCRIMBIMOS EN BBDD COMPARATIVA DE DATOS CON OF Y FECHA DE FIN Y BORRAMOS DATOS TANTO PLC COMO BBDD DatosRealesMMPP

                    await bbdd.Trazabilidad_Final(datos_MMPP_Finales, datos_MMPP_Teoricos);

                    // Borramos los datos desde el plc y luego los leemos otra vez.

                    BorrarDatosRealesMMPP(destino, Logs);

                    string cleanUp = await CargaDatosRealesMMPP(destino);
                    DatosMMPP datos_cleanUp = System.Text.Json.JsonSerializer.Deserialize<DatosMMPP>(cleanUp);

                    await bbdd.ActualizarOrdenFabricacionMMPP(destino, datos_cleanUp.OF);
                    await bbdd.ActualizaCantidadMMPP(destino, datos_cleanUp.Solido_1, datos_cleanUp.Solido_2, datos_cleanUp.Solido_3,
                                                     datos_cleanUp.Agua, datos_cleanUp.AguaRecu, datos_cleanUp.Antiespumante,
                                                     datos_cleanUp.Lignosulfonato, datos_cleanUp.Potasa);
                    
                    // Indicar al PLC que puede vaciar datos
                    WriteBOOL(DB, $"{OffsetAccionScada}.4", true); //BOOL Vaciar datos

                }
            }

            // Retornar la etapa actual para que se actualice en el controlador
            return EtapaAct;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // CargaDatosReceta: Actualiza datos de una receta en memoria PLC según el tipo y la propiedad dada.
        // Calcula posiciones(offsets) en memoria y escribe valores en función de las consignas recibidas.
        // Soporta varios procesos como cargas, espera, agitación y temperatura.

        public void CargaDatosReceta(string DB, string DB_Offsets, string NombrePropiedad, string ValorConsigna)
        {
            // Leer el offset base donde empieza la receta desde la memoria del PLC
            short OffsetReceta = ReadINT(DB_Offsets, "0.0");            //118.0

            // Calcular los offsets específicos para diferentes procesos sumando al offset base
            int OffsetTipoProceso = OffsetReceta + 260;                 //174 + 86
            int OffsetProcesoCargaSolidos1 = OffsetReceta + 262;        //176 + 86
            int OffsetProcesoCargaSolidos2 = OffsetReceta + 272;        //186 + 86
            int OffsetProcesoCargaSolidos3 = OffsetReceta + 282;        //196 + 86
            int OffsetProcesoCargaAguaDescal = OffsetReceta + 292;      //206 + 86
            int OffsetProcesoCargaAguaRecup = OffsetReceta + 298;       //216 + 86 ->>302
            int OffsetProcesoCargaAntiespumante = OffsetReceta + 304;   //218 + 86
            int OffsetProcesoCargaLigno = OffsetReceta + 310;           //224 + 86
            int OffsetProcesoCargaPotasa = OffsetReceta + 316;          //230 + 86
            int OffsetProcesoEspera = OffsetReceta + 322;               //236 + 86
            int OffsetProcesoAgitacion = OffsetReceta + 324;            //238 + 86 //176 + 86
            int OffsetProcesoTemperatura = OffsetReceta + 340;          //254 + 86

            // --------------------------------------------------------------------------------
            //
            // Ahora mismo tenemos 8 Tipos
            // 
            // HL PRUEBAS - POTASA LIQUIDA 70% - CALCIO LIGNOSULFONATO SOLIDO - AGUA
            // HL26(10-16)(0-0-8)-01 - LC70-01 - LC80-01 - POTASA LIQUIDA 50% - AGUA RECUPERADA
            //
            // --------------------------------------------------------------------------------

            // Dependiendo de la propiedad que llega, hacemos distintas acciones
            switch (NombrePropiedad)
            {
                case "tipo":
                    // Guardamos el tipo de proceso (ej. "Carga_Solidos_1")
                    Tipo = ValorConsigna;

                    // Según el tipo asignamos el puntero al offset correspondiente
                    switch (ValorConsigna)
                    {
                        case "Carga_Solidos_1":
                            Puntero = OffsetProcesoCargaSolidos1;
                            break;
                        case "Carga_Solidos_2":
                            Puntero = OffsetProcesoCargaSolidos2;
                            break;
                        case "Carga_Solidos_3":
                            Puntero = OffsetProcesoCargaSolidos3;
                            break;
                        case "Carga_Agua_Descal":
                            Puntero = OffsetProcesoCargaAguaDescal;
                            break;
                        case "Carga_Agua_Recup":
                            Puntero = OffsetProcesoCargaAguaRecup;
                            break;
                        case "Carga_Antiespumante":
                            Puntero = OffsetProcesoCargaAntiespumante;
                            break;
                        case "Carga_Ligno":
                            Puntero = OffsetProcesoCargaLigno;
                            break;
                        case "Carga_Potasa":
                            Puntero = OffsetProcesoCargaPotasa;
                            break;
                        case "Espera":
                            Puntero = OffsetProcesoEspera;
                            break;
                        case "Agitacion":
                            Puntero = OffsetProcesoAgitacion;
                            break;
                        case "Temperatura":
                            Puntero = OffsetProcesoTemperatura;
                            break;
                        case "Operador":
                            // No se necesita puntero para operador, no escribimos en memoria
                            break;
                        default:
                            // Si el tipo no coincide con ningún caso, no hacemos nada
                            break;
                    }
                    break;

                case "consigna":
                    // Guardamos el nombre de la consigna (ej. "Cantidad", "Velocidad")
                    Consigna = ValorConsigna;

                    // Según el tipo y la consigna, asignamos el offset donde escribir el dato
                    if (Tipo == "Carga_Solidos_1" || Tipo == "Carga_Solidos_2" || Tipo == "Carga_Solidos_3" || Tipo == "Carga_Agua_Descal" || Tipo == "Carga_Agua_Recup"
                        || Tipo == "Carga_Antiespumante" || Tipo == "Carga_Ligno" || Tipo == "Carga_Potasa")
                    {
                        switch (ValorConsigna)
                        {
                            case "Cantidad":
                                if (Tipo == "Carga_Agua_Recup")
                                { 
                                }
                                // Posición de cantidad en memoria
                                Offset = 2;
                                break;
                            case "Velocidad_Vibracion":
                                // Posición de velocidad de vibración
                                Offset = 6;
                                break;
                        }
                    }
                    if (Tipo == "Espera")
                    {
                        switch (ValorConsigna)
                        {
                            case "Tiempo":
                                // Posición del tiempo de espera
                                Offset = 0;
                                break;
                        }
                    }
                    if (Tipo == "Agitacion")
                    {
                        // Varias consignas para agitacion con diferentes offsets
                        switch (ValorConsigna)
                        {
                            case "Modo":
                                Offset = 2;
                                break;
                            case "Intermitencia":
                                Offset = 4;
                                break;
                            case "Velocidad":
                                Offset = 6;
                                break;
                            case "Temporizado":
                                Offset = 10;
                                break;
                            case "Tiempo_ON":
                                Offset = 12;
                                break;
                            case "Tiempo_OFF":
                                Offset = 14;
                                break;
                        }
                    }
                    if (Tipo == "Temperatura")
                    {
                        switch (ValorConsigna)
                        {
                            case "Temperatura":
                                // Posición de temperatura
                                Offset = 2;
                                break;
                        }
                    }
                    break;

                case "valor":
                    // Si la consigna es operador no escribimos datos
                    if (Consigna == "Operador")
                    {
                        break;
                    }
                    // Calculamos la dirección exacta donde se escribirá el dato en memoria PLC
                    string DireccionConsignas = $"{(Puntero + Offset)}.0";

                    // Según el tipo de dato escribimos usando la función adecuada
                    if (Consigna == "Cantidad" || Consigna == "Velocidad" || Consigna == "Velocidad_Vibracion" || Consigna == "Temperatura")
                    {
                        // Escribir número decimal (float/double)
                        WriteFLOAT(DB, DireccionConsignas, Double.Parse(ValorConsigna));
                    }
                    else if (Consigna == "Intermitencia")
                    {
                        // Escribir valor booleano (true/false)
                        WriteBOOL(DB, DireccionConsignas, Boolean.Parse(ValorConsigna));
                    }
                    else
                    {
                        // Escribir entero corto (int16)
                        WriteINT(DB, DireccionConsignas, Int16.Parse(ValorConsigna));
                        // WriteINT("9999", "0", Int16.Parse(ValorConsigna)); <<<<<----- DEBUG
                    }
                    break;

                case "procesoActivo":

                    // Direcciones para los bits que indican qué proceso está activo
                    string ProcesoCarga = $"{OffsetTipoProceso}.0";
                    string ProcesoEspera = $"{OffsetTipoProceso}.1";
                    string ProcesoOperador = $"{OffsetTipoProceso}.2";     
                    string ProcesoActivo = $"{Puntero}.0";

                    /*
                    // ->>> Cambio de tipos nuevos por viejos
                    Materias Primas                 Servicio Windows
                    LC70                            Carga Solidos 1
                    LC80                            Carga Solidos 2
                    HL26(10 - 16)(0 - 0 - 8)        Carga Solidos 3
                    Agua                            Agua Descal
                    Agua Recuperada                 Agua Recup
                    HL Pruebas                      Antiespumante
                    Calcio Lignosulfonato Solido    Ligno
                    Potasa                          Potasa
                    */

                    // Activar el proceso correspondiente según tipo y valor recibido
                    if ((Tipo == "Carga_Solidos_1" || Tipo == "Carga_Solidos_2" || Tipo == "Carga_Solidos_3" || Tipo == "Carga_Agua_Descal" || Tipo == "Carga_Agua_Recup"
                            || Tipo == "Carga_Antiespumante" || Tipo == "Carga_Ligno" || Tipo == "Carga_Potasa") && (ValorConsigna.ToString() == "True"))
                    {
                        WriteBOOL(DB, ProcesoCarga, true);
                        WriteBOOL(DB, ProcesoEspera, false);
                        WriteBOOL(DB, ProcesoOperador, false);
                        WriteBOOL(DB, $"{ProcesoActivo.ToString()}", true);
                    }
                    else if ((Tipo == "Espera") && (ValorConsigna.ToString() == "True"))
                    {
                        WriteBOOL(DB, ProcesoCarga, false);
                        WriteBOOL(DB, ProcesoEspera, true);
                        WriteBOOL(DB, ProcesoOperador, false);
                        }
                    else if ((Tipo == "Operador") && (ValorConsigna.ToString() == "True"))
                    {
                        WriteBOOL(DB, ProcesoCarga, false);
                        WriteBOOL(DB, ProcesoEspera, false);
                        WriteBOOL(DB, ProcesoOperador, true);
                    }

                    // Activar o desactivar procesos secundarios Agitación y Temperatura
                    if (Tipo == "Agitacion" && (ValorConsigna.ToString() == "True"))
                    {
                        WriteBOOL(DB, $"{OffsetProcesoAgitacion.ToString()}.0", true);
                    }
                    else if (Tipo == "Agitacion" && (ValorConsigna.ToString() == "False"))
                    {
                        WriteBOOL(DB, $"{OffsetProcesoAgitacion.ToString()}.0", false);
                    }
                    if (Tipo == "Temperatura" && (ValorConsigna.ToString() == "True"))
                    {
                        WriteBOOL(DB, $"{OffsetProcesoTemperatura.ToString()}.0", true);
                    }
                    else if (Tipo == "Temperatura" && (ValorConsigna.ToString() == "False"))
                    {
                        WriteBOOL(DB, $"{OffsetProcesoTemperatura.ToString()}.0", false);
                    }
                    break;

                case "id":
                    // Actualmente no hace nada con id
                break;

                default:
                    // Si el NombrePropiedad no coincide con ninguna opción válida
                break;
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Lee datos reales de materias primas desde el PLC si la lectura está habilitada.
        // Devuelve los datos en formato JSON.

        public async Task<string> CargaDatosRealesMMPP(string DB)
        {
            // Offsets para los datos en el PLC (posiciones en memoria)
            int offsetEnabled = 0;
            string offsetOF = "2";
            string offsetsolido1 = "258";
            string offsetsolido2 = "262";
            string offsetsolido3 = "266";
            string offsetAgua = "270";
            string offsetAguaRecu = "274";
            string offsetAnties = "278";
            string offsetLigno = "282";
            string offsetPotasa = "286";

            // Lee si la lectura está habilitada (true o false)
            bool InicioLectura = ReadBOOL(DB, $"{offsetEnabled}.0");

            // Inicializamos las variables con valores por defecto (vacío o cero)
            string OF = "";
            float Solido_1 = 0;
            float Solido_2 = 0;
            float Solido_3 = 0;
            float Agua = 0;
            float AguaRecu = 0;
            float Antiespumante = 0;
            float Lignosulfonato = 0;
            float Potasa = 0;

            // Si la lectura está habilitada, leemos los valores reales desde el PLC
            if (InicioLectura)
            {
                // Solo si la lectura inició, asignamos valores reales
                OF = ReadSTRING(DB, offsetOF);
                Solido_1 = (float)ReadFLOAT(DB, offsetsolido1);
                Solido_2 = (float)ReadFLOAT(DB, offsetsolido2);
                Solido_3 = (float)ReadFLOAT(DB, offsetsolido3);
                Agua = (float)ReadFLOAT(DB, offsetAgua);
                AguaRecu = (float)ReadFLOAT(DB, offsetAguaRecu);
                Antiespumante = (float)ReadFLOAT(DB, offsetAnties);
                Lignosulfonato = (float)ReadFLOAT(DB, offsetLigno);
                Potasa = (float)ReadFLOAT(DB, offsetPotasa);
            }
            else
            {
                // Si no está habilitada la lectura, dejamos valores por defecto (0 o vacío)
                // Aquí podrías agregar código para manejar este caso si es necesario.
            }

            // Definimos a qué destino se asigna según la DB (puedes agregar más casos)
            string Destino = DB switch
            {
                "8500" => "RC01",
                "8501" => "RC02",
                "8502" => "RC03",
                "8503" => "IM01",
                "8504" => "IM02",
                _ => null
            };

            // Si no se reconoce la DB, devolvemos null y avisamos
            if (Destino == null)
            {
                Console.WriteLine("⚠️ DB no reconocida");
                return null;
            }

            // Creamos un objeto con los datos leídos
            var datos = new DatosMMPP
            {
                OF = OF,
                Solido_1 = Solido_1,
                Solido_2 = Solido_2,
                Solido_3 = Solido_3,
                Agua = Agua,
                AguaRecu = AguaRecu,
                Antiespumante = Antiespumante,
                Lignosulfonato = Lignosulfonato,
                Potasa = Potasa
            };

            // Convertimos el objeto a JSON para facilitar su manejo externo
            string json = JsonConvert.SerializeObject(datos);
            // Retornamos el JSON con los datos leídos
            return json;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Borra o resetea los datos reales de materias primas en el PLC, poniendo valores vacíos o cero.
        // También deshabilita la lectura poniendo el flag en false.

        public void BorrarDatosRealesMMPP(string DB, Logs logs)
        {
            // Definimos los offsets de memoria para cada dato que vamos a borrar
            int offsetEnabled = 0;
            string offsetOF = "2";
            string offsetsolido1 = "258";
            string offsetsolido2 = "262";
            string offsetsolido3 = "266";
            string offsetAgua = "270";
            string offsetAguaRecu = "274";
            string offsetAnties = "278";
            string offsetLigno = "282";
            string offsetPotasa = "286";

            // Escribimos valores "vacíos" o cero en cada offset correspondiente:
            WriteSTRING(DB, offsetOF, "", logs); // Vaciar OF
            WriteFLOAT(DB, offsetsolido1, 0);
            WriteFLOAT(DB, offsetsolido2, 0);
            WriteFLOAT(DB, offsetsolido3, 0);
            WriteFLOAT(DB, offsetAgua, 0);
            WriteFLOAT(DB, offsetAguaRecu, 0);
            WriteFLOAT(DB, offsetAnties, 0);
            WriteFLOAT(DB, offsetLigno, 0);
            WriteFLOAT(DB, offsetPotasa, 0);

            // Finalmente, deshabilita la lectura estableciendo el flag en false
            WriteBOOL(DB, $"{offsetEnabled}.0", false); // Habilitación en false
        }

        // ---------------------------------------------------------------------------------------------------------------------------
        #endregion
    }
} 

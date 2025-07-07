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
        /*
        public void WriteBOOL(string NumeroDB, string Direccion, bool Valor)
        {
            PLC.Write($"DB{NumeroDB}.DBX{Direccion}", Valor);
            
        }
        */
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

        #region Metodos Receta

        public async Task<short> GestorReceta(string DB, string DB_Offsets, string NombreReactor, short EtapaAct, string RutaApi, Logs Logs)//, DatosGenReceta GenReceta)
        {
            ConsAPI DatosAPI = new ConsAPI(PLC);
            DatosGenReceta GenReceta = new DatosGenReceta();

            //Calculo de offsets para en caso de que se mueva la estructura dentro del DB del gestor de recetas del PLC
            short OffsetReceta = ReadINT(DB_Offsets, "0.0");
            short LongUDT = ReadINT(DB_Offsets, "2.0");

            string OffsetCheckRecetaInicio = (OffsetReceta + 0).ToString(); //118.0
            string OffsetNumEtapas = (OffsetReceta + 2).ToString(); //120.0
            string OffsetEtapaAct = (OffsetReceta + 4).ToString(); //122.0
            string OffsetOF = (OffsetReceta + 6).ToString(); //124.0
            string OffsetNombreReceta = (OffsetReceta + 38).ToString(); //156.0
            string OffsetNombreEtapa = (OffsetReceta + 140).ToString(); //258.0
            string OffsetComm = (OffsetReceta + 172).ToString(); //290.0
            string OffsetCheckRecetaFin = (OffsetReceta + (LongUDT - 2)).ToString(); //464.0
            string OffsetAccionScada = (466).ToString();

            //Datos generales de la receta
            EtapaAct = ReadINT(DB, $"{OffsetEtapaAct}.0");
            //Console.WriteLine($"[DEBUG] EtapaAct inicial leída: {EtapaAct}");

            string NomEtapaAct = ReadSTRING(DB, $"{OffsetNombreEtapa}");
            //Console.WriteLine($"[DEBUG] NombreEtapa inicial leída: '{NomEtapaAct}'");

            string RutaConsumoAPI = $"{RutaApi}{NombreReactor}";
            string estadoFinalizada;

            //Bits de comunicacion del gestor de recetas del PLC con el gestor de recetas del servicio
            bool InicioReceta = ReadBOOL(DB, $"{OffsetComm}.0"); //290.0
            bool InicioEtapa = ReadBOOL(DB, $"{OffsetComm}.1"); //290.1
            bool EtapaActualCargada = ReadBOOL(DB, $"{OffsetComm}.2"); //290.2
            bool InicioCargado = ReadBOOL(DB, $"{OffsetComm}.3"); //290.3
            bool SiguienteEtapaRecibida = ReadBOOL(DB, $"{OffsetComm}.4"); //290.4
            bool UltimaEtapa = ReadBOOL(DB, $"{OffsetComm}.5"); //290.5

            //Console.WriteLine($"[DEBUG] Bits comunicación - InicioReceta: {InicioReceta}, InicioEtapa: {InicioEtapa}, EtapaActualCargada: {EtapaActualCargada}, InicioCargado: {InicioCargado}, SiguienteEtapaRecibida: {SiguienteEtapaRecibida}, UltimaEtapa: {UltimaEtapa}");

            //Se obtienen los valores correspondientes para la comprobacion del Offset y la longitud de la UDT de los datos de la receta en el DB del PLC seleciconado
            short CheckRecetaInicio = ReadINT(DB, $"{OffsetCheckRecetaInicio}.0");
            short CheckRecetaFin = ReadINT(DB, $"{OffsetCheckRecetaFin}.0");

            //Console.WriteLine($"[DEBUG] CheckRecetaInicio: {CheckRecetaInicio}, CheckRecetaFin: {CheckRecetaFin}");


            if (CheckRecetaInicio != 32767 || CheckRecetaFin != 32767)
            {
                if (CheckRecetaInicio != 32767)
                {
                    WriteBOOL(DB_Offsets, "4.0", true);
                    Logs.RegistrarError($"El offset de los datos de la receta en el DB: {DB}, no es correcto. Revisar el valor de la primera variable en el DB: {DB_Offsets}");
                }
                else
                {
                    WriteBOOL(DB_Offsets, "4.0", false);

                    if (CheckRecetaFin != 32767)
                    {

                        WriteBOOL(DB_Offsets, "4.1", true);
                        Logs.RegistrarError($"La longitud de la UDT de la receta en el DB: {DB}, no es correcta. Revisar el valor de la segunda variable en el DB: {DB_Offsets}");
                    }
                }

            }
            else
            {
                WriteBOOL(DB_Offsets, "4.0", false);
                WriteBOOL(DB_Offsets, "4.1", false);



                if (!InicioReceta)
                {
                    HttpClient httpClient = new HttpClient();
                    HttpResponseMessage response = await httpClient.GetAsync(RutaConsumoAPI);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        //Console.WriteLine($"[DEBUG] Response API InicioReceta: {responseBody}");

                        if (responseBody != "false")
                        {
                            GenReceta.OF = ReadSTRING(DB, $"{OffsetOF}.0");
                            //Console.WriteLine($"[DEBUG] OF leído antes de inicio: '{GenReceta.OF}'");
                            if (GenReceta.OF == null)
                            {
                                WriteBOOL(DB, $"{OffsetAccionScada}.0", true);
                                //Console.WriteLine($"[INFO] Receta iniciada en reactor {NombreReactor}");
                            }
                        }
                    }
                }

                if (InicioReceta && !InicioCargado)
                {
                    EtapaAct = 0;
                    GenReceta.OF = (await (DatosAPI.DatosCabecera(RutaConsumoAPI, "ordenFabricacion")));
                    GenReceta.NombreReceta = (await (DatosAPI.DatosCabecera(RutaConsumoAPI, "nombreReceta")));
                    GenReceta.NumEtapas = Int16.Parse(await (DatosAPI.DatosCabecera(RutaConsumoAPI, "numeroEtapas")));
                    GenReceta.NombreEtapaSiguiente = await (DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct + 1));

                    //Console.WriteLine($"[INFO] Inicio de receta: OF={GenReceta.OF}, NombreReceta={GenReceta.NombreReceta}, NumEtapas={GenReceta.NumEtapas}, NombreEtapaSiguiente={GenReceta.NombreEtapaSiguiente}");

                    //Se cargan los datos de la primera etapa
                    WriteSTRING(DB, $"{OffsetOF}.0", GenReceta.OF, Logs);
                    WriteSTRING(DB, $"{OffsetNombreReceta}.0", GenReceta.NombreReceta, Logs);
                    WriteINT(DB, $"{OffsetNumEtapas}.0", GenReceta.NumEtapas);
                    WriteSTRING(DB, $"{OffsetNombreEtapa}.0", GenReceta.NombreEtapaSiguiente, Logs);

                    //Console.WriteLine($"[DEBUG] Valores escritos tras inicio: OF, NombreReceta, NumEtapas, NombreEtapa");

                    await DatosAPI.DatosEtapas(DB, DB_Offsets, RutaConsumoAPI, EtapaAct + 1);

                    WriteBOOL(DB, $"{OffsetComm}.3", true); //BOOL Inicio Cargado
                    Logs.RegistrarInfo($"Inicio de la receta en el reactor: {NombreReactor}");

                }

                if (EtapaActualCargada && !InicioEtapa)
                {
                    //Console.WriteLine($"[INFO] Procesando incremento de etapa desde: {EtapaAct}");
                    EtapaAct++;

                    WriteINT(DB, $"{OffsetEtapaAct}.0", EtapaAct);
                    WriteBOOL(DB, $"{OffsetComm}.3", true);
                    
                    //Console.WriteLine($"[DEBUG] Nuevo EtapaAct escrito: {EtapaAct}");

                    GenReceta.NumEtapas = ReadINT(DB, $"{OffsetNumEtapas}.0");
                    GenReceta.OF = ReadSTRING(DB, $"{OffsetOF}");

                    //GenReceta.NombreEtapaActual = ReadSTRING(DB, $"{OffsetNombreEtapa}");

                    // ✅ Pedir el nombre actualizado desde la API en lugar de leer desde el DB
                    GenReceta.NombreEtapaActual = await DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct);
                    WriteSTRING(DB, $"{OffsetNombreEtapa}.0", GenReceta.NombreEtapaActual, Logs);

                    //Console.WriteLine($"[DEBUG] Leído después incremento: NumEtapas={GenReceta.NumEtapas}, OF={GenReceta.OF}, NombreEtapaActual='{GenReceta.NombreEtapaActual}'");

                    if (EtapaAct >= GenReceta.NumEtapas)
                    {
                        WriteBOOL(DB, $"{OffsetComm}.5", true); //BOOL Ultima Etapa
                        //Console.WriteLine($"[INFO] Última etapa alcanzada: {EtapaAct}");
                        Logs.RegistrarInfo($"Ultima etapa de la receta en el reactor: {NombreReactor}");
                    }
                    else
                    {
                        //GenReceta.NombreEtapaSiguiente = await (DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct));
                        GenReceta.NombreEtapaSiguiente = await (DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct));
                        WriteSTRING(DB, $"{OffsetNombreEtapa}.0", GenReceta.NombreEtapaSiguiente, Logs);
                        //Console.WriteLine($"[DEBUG] NombreEtapa siguiente escrita: '{GenReceta.NombreEtapaSiguiente}'");
                        //Se cargan los datos de la siguiente etapa
                        await DatosAPI.DatosEtapas(DB, DB_Offsets, RutaConsumoAPI, EtapaAct + 1);
                    }

                    WriteBOOL(DB, $"{OffsetComm}.4", true);
                    Logs.RegistrarInfo($"📤 Enviando etapa a API - OF: {GenReceta.OF}, EtapaAct: {EtapaAct}, NombreEtapaActual: '{GenReceta.NombreEtapaActual}', NombreEtapaSiguiente: '{GenReceta.NombreEtapaSiguiente}'");
                    DatosAPI.ActualizarEtapaAPI(GenReceta, EtapaAct, Logs);
                    //Logs.RegistrarInfo($"Etapa Actual fuera del if Ultima etapa: {EtapaAct}");

                }

                if (SiguienteEtapaRecibida && !UltimaEtapa)
                {
                    GenReceta.NumEtapas = ReadINT(DB, $"{OffsetNumEtapas}.0");
                    //Console.WriteLine($"[DEBUG] Comprobando etapa para siguiente etapa recibida. EtapaAct={EtapaAct}, NumEtapas={GenReceta.NumEtapas}");


                    if (EtapaAct >= GenReceta.NumEtapas)
                    {
                        WriteBOOL(DB, $"{OffsetComm}.5", true); //BOOL Ultima Etapa
                        //Console.WriteLine("[INFO] Marcado como última etapa");
                    }
                }
                //Console.WriteLine($"[DEBUG NOMBRE ETAPA] ---> {NomEtapaAct}");
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

                    // - - - - - - - LEEMOS CANTIDADES REALES
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
                    //Console.WriteLine($"[DEBUG NOMBRE DESTINO] ---> {destino}");
                    string resultado = await CargaDatosRealesMMPP(destino);
                    DatosMMPP datos_MMPP_Finales = System.Text.Json.JsonSerializer.Deserialize<DatosMMPP>(resultado);

                    // LEEMOS DATOS TEORICOS DE LA BBDD
                    string resultado_teorico = await bbdd.ExtraerMMPP_Teoricas(datos_MMPP_Finales.OF);
                    DatosMMPP datos_MMPP_Teoricos = System.Text.Json.JsonSerializer.Deserialize<DatosMMPP>(resultado_teorico);

                    // ACTUALIZAMOS DATOS FINALES EN LA BBDD

                    if (datos_MMPP_Finales != null)
                    {
                        await bbdd.ActualizarOrdenFabricacionMMPP(destino, datos_MMPP_Finales.OF);
                        await bbdd.ActualizaCantidadMMPP(destino, datos_MMPP_Finales.Solido_1, datos_MMPP_Finales.Solido_2, datos_MMPP_Finales.Solido_3,
                                                         datos_MMPP_Finales.Agua, datos_MMPP_Finales.AguaRecu, datos_MMPP_Finales.Antiespumante,
                                                         datos_MMPP_Finales.Lignosulfonato, datos_MMPP_Finales.Potasa);
                    }

                    //Console.WriteLine($"[MMPP - FINALES] {DateTime.Now}: Destino={destino} Datos: {resultado}");
                    // - - - - - - - - - - - - - - - - - - - - 
                    DatosAPI.FinalizarOFAPI(GenReceta.OF, estadoFinalizada, Logs);

                    // ---- ESCRIMBIMOS EN BBDD COMPARATIVA DE DATOS CON OF Y FECHA DE FIN Y BORRAMOS DATOS TANTO PLC COMO BBDD DatosRealesMMPP

                    await bbdd.Trazabilidad_Final(datos_MMPP_Finales, datos_MMPP_Teoricos);

                    // - Borramos los datos desde el plc y luego los leemos otra vez.

                    BorrarDatosRealesMMPP(destino, Logs);

                    string cleanUp = await CargaDatosRealesMMPP(destino);
                    DatosMMPP datos_cleanUp = System.Text.Json.JsonSerializer.Deserialize<DatosMMPP>(cleanUp);

                    await bbdd.ActualizarOrdenFabricacionMMPP(destino, datos_cleanUp.OF);
                    await bbdd.ActualizaCantidadMMPP(destino, datos_cleanUp.Solido_1, datos_cleanUp.Solido_2, datos_cleanUp.Solido_3,
                                                     datos_cleanUp.Agua, datos_cleanUp.AguaRecu, datos_cleanUp.Antiespumante,
                                                     datos_cleanUp.Lignosulfonato, datos_cleanUp.Potasa);
                    //------------------------------------------------------------------------------------------

                    WriteBOOL(DB, $"{OffsetAccionScada}.4", true); //BOOL Vaciar datos

                }
            }

            return EtapaAct;
        }

        public void CargaDatosReceta(string DB, string DB_Offsets, string NombrePropiedad, string ValorConsigna)
        {
            //Console.WriteLine($"[DEBUG] → DB: {DB}, DB_Offsets: {DB_Offsets}, Propiedad: {NombrePropiedad}, Valor: {ValorConsigna}");

            short OffsetReceta = ReadINT(DB_Offsets, "0.0"); //118.0
            //Console.WriteLine($"[DEBUG] OffsetReceta kjBFDWJB: {OffsetReceta}");

            int OffsetTipoProceso = OffsetReceta + 260; //174 + 86
            int OffsetProcesoCargaSolidos1 = OffsetReceta + 262; //176 + 86
            int OffsetProcesoCargaSolidos2 = OffsetReceta + 272; //186 + 86
            int OffsetProcesoCargaSolidos3 = OffsetReceta + 282; //196 + 86
            int OffsetProcesoCargaAguaDescal = OffsetReceta + 292; //206 + 86
            int OffsetProcesoCargaAguaRecup = OffsetReceta + 298; //216 + 86 ->>302
            int OffsetProcesoCargaAntiespumante = OffsetReceta + 304; //218 + 86
            int OffsetProcesoCargaLigno = OffsetReceta + 310; //224 + 86
            int OffsetProcesoCargaPotasa = OffsetReceta + 316; //230 + 86
            int OffsetProcesoEspera = OffsetReceta + 322; //236 + 86
            int OffsetProcesoAgitacion = OffsetReceta + 324; //238 + 86 //176 + 86
            int OffsetProcesoTemperatura = OffsetReceta + 340; //254 + 86

            // --------------------------------------------------------------------------------
            //
            // Ahora mismo tenemos 8 Tipos
            // 
            // HL PRUEBAS - POTASA LIQUIDA 70% - CALCIO LIGNOSULFONATO SOLIDO - AGUA
            // HL26(10-16)(0-0-8)-01 - LC70-01 - LC80-01 - POTASA LIQUIDA 50% - AGUA RECUPERADA
            //
            // --------------------------------------------------------------------------------

            // Console.WriteLine($"CARGANDO DATOS RECETAS {NombrePropiedad} - - {ValorConsigna}");

            switch (NombrePropiedad)
            {
                case "tipo":
                    Tipo = ValorConsigna;
                    

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
                            //Console.WriteLine($"[DEBUG] Tipo: {Tipo}");
                            Puntero = OffsetProcesoCargaAguaRecup;
                            //Console.WriteLine($"[DEBUG] Offset Puntero asignado: {Puntero}");
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
                            //Console.WriteLine($"[DEBUG] Tipo: {Tipo}");
                            Puntero = OffsetProcesoEspera;
                            //Console.WriteLine($"[DEBUG] Offset Puntero asignado: {Puntero}");
                            break;
                        case "Agitacion":
                            Puntero = OffsetProcesoAgitacion;
                            break;
                        case "Temperatura":
                            Puntero = OffsetProcesoTemperatura;
                            break;

                        case "Operador":
                            //Console.WriteLine("[DEBUG] Tipo 'Operador' no requiere puntero. Se omite asignación.");
                            //Puntero = 0; // o algún valor inválido para evitar escritura
                            break;

                        default:
                            break;
                    }
                    
                    break;

                case "consigna":
                    Consigna = ValorConsigna;
                    

                    if (Tipo == "Carga_Solidos_1" || Tipo == "Carga_Solidos_2" || Tipo == "Carga_Solidos_3" || Tipo == "Carga_Agua_Descal" || Tipo == "Carga_Agua_Recup"
                        || Tipo == "Carga_Antiespumante" || Tipo == "Carga_Ligno" || Tipo == "Carga_Potasa")
                    {
                        switch (ValorConsigna)
                        {
                            case "Cantidad":
                                if (Tipo == "Carga_Agua_Recup") { 
                                    //Console.WriteLine($"[DEBUG] Consigna: {Consigna}");
                                 }
                                Offset = 2;
                                break;
                            case "Velocidad_Vibracion":
                                Offset = 6;
                                break;
                        }
                    }
                    if (Tipo == "Espera")
                    {
                        //Console.WriteLine($"[DEBUG] Consigna: {Consigna}");
                        switch (ValorConsigna)
                        {
                            case "Tiempo":
                                Offset = 0;
                                break;
                        }
                    }
                    if (Tipo == "Agitacion")
                    {
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
                                Offset = 2;
                                break;
                        }
                    }

                    //Console.WriteLine($"[DEBUG] Offset de Consigna: {Offset}");
                    break;

                case "valor":

                    if (Consigna == "Operador")
                    {
                        //Console.WriteLine("[DEBUG] Consigna es 'Operador', no se realiza escritura.");
                        break;
                    }

                    string DireccionConsignas = $"{(Puntero + Offset)}.0";

                    //Console.WriteLine("     ");
                    ///Console.WriteLine($"[DEBUG] Dirección completa para escritura: DB{DB}.DBD{DireccionConsignas}");
                    //Console.WriteLine($"[DEBUG] eSTA ES MI CONSIGNA ¡¡¡ {Consigna}");
                    //Console.WriteLine($"[DEBUG] eSTA ES MI OFFSET ¡¡¡ {Offset}");
                    //Console.WriteLine($"[DEBUG] eSTA ES MI OUNTERO ¡¡¡ {Puntero}");

                    //Traspaso de consignas 
                    if (Consigna == "Cantidad" || Consigna == "Velocidad" || Consigna == "Velocidad_Vibracion" || Consigna == "Temperatura")
                    {
                        //Console.WriteLine($"[DEBUG] Escribiendo FLOAT: {ValorConsigna}");
                        WriteFLOAT(DB, DireccionConsignas, Double.Parse(ValorConsigna));
                    }
                    else if (Consigna == "Intermitencia")
                    {
                        //Console.WriteLine($"[DEBUG] Escribiendo BOOL: {ValorConsigna}");
                        //Console.WriteLine($"[DEBUG] Valor del BOOLLL Escribiendo BOOL: {ValorConsigna} y esta la consigna {Consigna}");
                        WriteBOOL(DB, DireccionConsignas, Boolean.Parse(ValorConsigna));
                    }
                    /*
                    else if (Consigna == "Operador")
                    {
                        Console.WriteLine("[DEBUG] Consigna es 'Operador', no se realiza escritura.");
                        break;                       
                    }
                    */
                    else
                    {
                        //Console.WriteLine($"[DEBUG] hbsdjhsbjdb Escribiendo INT: {ValorConsigna} y esta la consigna {Consigna}");
                        WriteINT(DB, DireccionConsignas, Int16.Parse(ValorConsigna));
                        WriteINT("9999", "0", Int16.Parse(ValorConsigna));
                    }
                    break;


                /*
                else 
                {
                    Console.WriteLine($"Mi AHSHAHS consigna ->> {Consigna}");
                    Console.WriteLine($"MI DIRECCION DE LAS CONSIGNAS? ->> {DireccionConsignas}");
                    if (Consigna != "Operador") 
                    {

                        Console.WriteLine($"[DEBUG] hbsdjhsbjdb Escribiendo INT: {ValorConsigna} y esta la consigna {Consigna}");
                        WriteINT(DB, DireccionConsignas, Int16.Parse(ValorConsigna));
                    }

                }
                break;
                */


                case "procesoActivo":
                            string ProcesoCarga = $"{OffsetTipoProceso}.0";
                            string ProcesoEspera = $"{OffsetTipoProceso}.1";
                            // Operado???? ->>>>>>>>>>>>>>><
                            string ProcesoOperador = $"{OffsetTipoProceso}.2";
                            
                            
                            string ProcesoActivo = $"{Puntero}.0";
                            /*
                            Console.WriteLine($"[DEBUG 10] la rata??? Reset de proceso activo: {ProcesoActivo}");
                            //Resets de todos los procesos
                            WriteBOOL(DB, $"{ProcesoActivo.ToString()}", false);

                            */


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

                            //Activaciones de procesos
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

                            //Activacion proceso secundario agitacion
                            if (Tipo == "Agitacion" && (ValorConsigna.ToString() == "True"))
                            {
                                //Console.WriteLine($"[DEBUG] Activando Agitacion a: {ValorConsigna}");
                                WriteBOOL(DB, $"{OffsetProcesoAgitacion.ToString()}.0", true);
                            }
                            else if (Tipo == "Agitacion" && (ValorConsigna.ToString() == "False"))
                            {
                                //Console.WriteLine($"[DEBUG] Activando Temperatura a: {ValorConsigna}");
                                WriteBOOL(DB, $"{OffsetProcesoAgitacion.ToString()}.0", false);
                            }

                            //Activacion proceso secundario temperatura
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

                            break;

                        default:
                            //Console.WriteLine($"Opcion no valida. Propiedad: {NombrePropiedad}");
                            break;
                        }



        }
        #endregion

        public async Task<string> CargaDatosRealesMMPP(string DB)
        {
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

            bool InicioLectura = ReadBOOL(DB, $"{offsetEnabled}.0");

            // Inicializamos variables con valores por defecto (0 o cadena vacía)
            string OF = "";
            float Solido_1 = 0;
            float Solido_2 = 0;
            float Solido_3 = 0;
            float Agua = 0;
            float AguaRecu = 0;
            float Antiespumante = 0;
            float Lignosulfonato = 0;
            float Potasa = 0;

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
                //Console.WriteLine("⚠️ Lectura no iniciada.");
                // Si quieres, puedes devolver un JSON con valores 0 o null, o simplemente un mensaje
                // Por ejemplo, devolver datos con valores 0:
            }

            string Destino = DB switch
            {
                "8500" => "RC01",
                "8501" => "RC02",
                "8502" => "RC03",
                "8503" => "IM01",
                "8504" => "IM02",
                _ => null
            };

            if (Destino == null)
            {
                Console.WriteLine("⚠️ DB no reconocida");
                return null;
            }

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

            string json = JsonConvert.SerializeObject(datos);
            return json;
        }

        public void BorrarDatosRealesMMPP(string DB, Logs logs)
        {
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

            // Vaciar valores
            WriteSTRING(DB, offsetOF, "", logs); // Vaciar OF
            WriteFLOAT(DB, offsetsolido1, 0);
            WriteFLOAT(DB, offsetsolido2, 0);
            WriteFLOAT(DB, offsetsolido3, 0);
            WriteFLOAT(DB, offsetAgua, 0);
            WriteFLOAT(DB, offsetAguaRecu, 0);
            WriteFLOAT(DB, offsetAnties, 0);
            WriteFLOAT(DB, offsetLigno, 0);
            WriteFLOAT(DB, offsetPotasa, 0);
            WriteBOOL(DB, $"{offsetEnabled}.0", false); // Habilitación en false
        }


    }
} 

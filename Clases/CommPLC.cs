using S7.Net;
using System.Text;
using ServicioWindows.Models;

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

            //Console.WriteLine($"Long variable destino: {LongStringPLC}");
            //Console.WriteLine($"Long variable origen: {LongString}");

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
            PLC.Write($"DB{NumeroDB}.DBX{Direccion}", Valor);
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


            //Datos generales de la receta
            EtapaAct = ReadINT(DB, $"{OffsetEtapaAct}.0");
            string RutaConsumoAPI = $"{RutaApi}{NombreReactor}";

            //Bits de comunicacion del gestor de recetas del PLC con el gestor de recetas del servicio
            bool InicioReceta = ReadBOOL(DB, $"{OffsetComm}.0"); //290.0
            bool InicioEtapa = ReadBOOL(DB, $"{OffsetComm}.1"); //290.1
            bool EtapaActualCargada = ReadBOOL(DB, $"{OffsetComm}.2"); //290.2
            bool InicioCargado = ReadBOOL(DB, $"{OffsetComm}.3"); //290.3
            bool SiguienteEtapaRecibida = ReadBOOL(DB, $"{OffsetComm}.4"); //290.4
            bool UltimaEtapa = ReadBOOL(DB, $"{OffsetComm}.5"); //290.5

            //Se obtienen los valores correspondientes para la comprobacion del Offset y la longitud de la UDT de los datos de la receta en el DB del PLC seleciconado
            short CheckRecetaInicio = ReadINT(DB, $"{OffsetCheckRecetaInicio}.0");
            short CheckRecetaFin = ReadINT(DB, $"{OffsetCheckRecetaFin}.0");

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
                    //Console.WriteLine($"{apiUrl}");
                    HttpResponseMessage response = await httpClient.GetAsync(RutaConsumoAPI);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        //Console.WriteLine($"ResponseBody en reactor {NombreReactor} = {responseBody}");

                        if (responseBody != "false")
                        {
                            GenReceta.OF = ReadSTRING(DB, $"{OffsetOF}.0");
                            //Console.WriteLine($"OF en reactor {NombreReactor} = {OF}");
                            if (GenReceta.OF == null)
                            {
                                WriteBOOL(DB, $"{OffsetComm}.0", true);
                                Console.WriteLine($"Receta iniciada en reactor {NombreReactor}");
                            }
                        }
                    }
                }


                if (InicioReceta && !InicioCargado)
                {
                    EtapaAct = 0;
                    //Console.WriteLine("1");
                    GenReceta.OF = (await (DatosAPI.DatosCabecera(RutaConsumoAPI, "ordenFabricacion")));
                    GenReceta.NombreReceta = (await (DatosAPI.DatosCabecera(RutaConsumoAPI, "nombreReceta")));
                    GenReceta.NumEtapas = Int16.Parse(await (DatosAPI.DatosCabecera(RutaConsumoAPI, "numeroEtapas")));
                    GenReceta.NombreEtapa = await (DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct + 1));
                    //Console.WriteLine("2");

                    //Se cargan los datos de la primera etapa
                    WriteSTRING(DB, $"{OffsetOF}.0", GenReceta.OF, Logs);
                    WriteSTRING(DB, $"{OffsetNombreReceta}.0", GenReceta.NombreReceta, Logs);
                    WriteINT(DB, $"{OffsetNumEtapas}.0", GenReceta.NumEtapas);
                    WriteSTRING(DB, $"{OffsetNombreEtapa}.0", GenReceta.NombreEtapa, Logs);

                    await DatosAPI.DatosEtapas(DB, DB_Offsets, RutaConsumoAPI, EtapaAct + 1);

                    WriteBOOL(DB, $"{OffsetComm}.3", true); //BOOL Inicio Cargado
                    Logs.RegistrarInfo($"Inicio de la receta en el reactor: {NombreReactor}");

                }

                if (EtapaActualCargada && !InicioEtapa)
                {
                    EtapaAct++;
                    GenReceta.NumEtapas = ReadINT(DB, $"{OffsetNumEtapas}.0");

                    WriteBOOL(DB, $"{OffsetComm}.3", true);
                    WriteINT(DB, $"{OffsetEtapaAct}.0", EtapaAct);


                    if (EtapaAct >= GenReceta.NumEtapas)
                    {
                        WriteBOOL(DB, $"{OffsetComm}.5", true); //BOOL Ultima Etapa
                        Logs.RegistrarInfo($"Ultima etapa de la receta en el reactor: {NombreReactor}");
                    }
                    else
                    {
                        GenReceta.NombreEtapa = await (DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct));
                        WriteSTRING(DB, $"{OffsetNombreEtapa}.0", GenReceta.NombreEtapa, Logs);
                        //Se cargan los datos de la siguiente etapa
                        await DatosAPI.DatosEtapas(DB, DB_Offsets, RutaConsumoAPI, EtapaAct + 1);

                    }
                    WriteBOOL(DB, $"{OffsetComm}.4", true);
                }

                if (SiguienteEtapaRecibida && !UltimaEtapa)
                {
                    GenReceta.NumEtapas = ReadINT(DB, $"{OffsetNumEtapas}.0");

                    if (EtapaAct >= GenReceta.NumEtapas)
                    {
                        WriteBOOL(DB, $"{OffsetComm}.5", true); //BOOL Ultima Etapa
                    }
                }
            }
            return EtapaAct;
        }
        public void CargaDatosReceta(string DB, string DB_Offsets, string NombrePropiedad, string ValorConsigna)
        {

            short OffsetReceta = ReadINT(DB_Offsets, "0.0"); //118.0
            int OffsetTipoProceso = OffsetReceta + 260; //174 + 86
            int OffsetProcesoCargaSolidos1 = OffsetReceta + 262; //176 + 86
            int OffsetProcesoCargaSolidos2 = OffsetReceta + 272; //186 + 86
            int OffsetProcesoCargaSolidos3 = OffsetReceta + 282; //196 + 86
            int OffsetProcesoCargaAguaDescal = OffsetReceta + 292; //206 + 86
            int OffsetProcesoCargaAguaRecup = OffsetReceta + 302; //216 + 86
            int OffsetProcesoCargaAntiespumante = OffsetReceta + 304; //218 + 86
            int OffsetProcesoCargaLigno = OffsetReceta + 310; //224 + 86
            int OffsetProcesoCargaPotasa = OffsetReceta + 316; //230 + 86
            int OffsetProcesoEspera = OffsetReceta + 322; //236 + 86
            int OffsetProcesoAgitacion = OffsetReceta + 324; //238 + 86 //176 + 86
            int OffsetProcesoTemperatura = OffsetReceta + 340; //254 + 86

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
                                Offset = 2;
                                break;
                            case "Velocidad_Vibracion":
                                Offset = 6;
                                break;
                        }
                    }
                    if (Tipo == "Espera")
                    {
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
                    break;

                case "valor":
                    string DireccionConsignas = $"{(Puntero + Offset)}.0";

                    //Traspaso de consignas
                    if (Consigna == "Cantidad" || Consigna == "Velocidad" || Consigna == "Velocidad_Vibracion" || Consigna == "Temperatura")
                    {
                        WriteFLOAT(DB, DireccionConsignas, Double.Parse(ValorConsigna));
                    }
                    else if (Consigna == "Intermitencia")
                    {
                        WriteBOOL(DB, DireccionConsignas, Boolean.Parse(ValorConsigna));
                    }
                    else
                    {
                        WriteINT(DB, DireccionConsignas, Int16.Parse(ValorConsigna));
                    }
                    break;

                case "procesoActivo":
                    string ProcesoCarga = $"{OffsetTipoProceso}.0";
                    string ProcesoEspera = $"{OffsetTipoProceso}.1";
                    string ProcesoActivo = $"{Puntero}.0";

                    //Resets de todos los procesos
                    WriteBOOL(DB, $"{ProcesoActivo.ToString()}", false);
                    //WriteBOOL(DB, $"{OffsetProcesoAgitacion.ToString()}.0", false);
                    //WriteBOOL(DB, $"{OffsetProcesoTemperatura.ToString()}.0", false);

                    //Console.WriteLine($"Valor de tipo {Tipo} y consigna de procesoActivo = {ValorConsigna}");

                    //Activaciones de procesos
                    if ((Tipo == "Carga_Solidos_1" || Tipo == "Carga_Solidos_2" || Tipo == "Carga_Solidos_3" || Tipo == "Carga_Agua_Descal" || Tipo == "Carga_Agua_Recup"
                        || Tipo == "Carga_Antiespumante" || Tipo == "Carga_Ligno" || Tipo == "Carga_Potasa") && (ValorConsigna.ToString() == "True"))
                    {
                        WriteBOOL(DB, ProcesoCarga, true);
                        WriteBOOL(DB, ProcesoEspera, false);
                        WriteBOOL(DB, $"{ProcesoActivo.ToString()}", true);
                        //Console.WriteLine($"Proceso activo: {ProcesoActivo}");
                    }
                    else if ((Tipo == "Espera") && (ValorConsigna.ToString() == "True"))
                    {
                        WriteBOOL(DB, ProcesoCarga, false);
                        WriteBOOL(DB, ProcesoEspera, true);
                        //Console.WriteLine($"Proceso tipo: {Tipo}");
                    }

                    //Activacion proceso secundario agitacion
                    if (Tipo == "Agitacion" && (ValorConsigna.ToString() == "True"))
                    {
                        WriteBOOL(DB, $"{OffsetProcesoAgitacion.ToString()}.0", true);
                    }
                    else if (Tipo == "Agitacion" && (ValorConsigna.ToString() == "False"))
                    {
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
                    Console.WriteLine($"Opcion no valida. Propiedad: {NombrePropiedad}");
                    break;
            }

        }
        #endregion




    }
}

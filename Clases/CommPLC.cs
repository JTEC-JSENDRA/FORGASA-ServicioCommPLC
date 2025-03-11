using System.Text;
using S7.Net;

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
        public async Task<short> GestorReceta(string DB, string DB_Offsets, string NombreReactor, short EtapaAct, string RutaApi, Logs Logs)
        {
            ConsAPI DatosAPI = new ConsAPI(PLC);

            //Calculo de offsets para en caso de que se mueva la estructura dentro del DB del gestor de recetas del PLC
            short OffsetReceta = ReadINT(DB_Offsets, "0.0");
            short LongUDT = ReadINT(DB_Offsets, "2.0");

            string OffsetCheckRecetaInicio = (OffsetReceta + 0).ToString();
            string OffsetNumEtapas = (OffsetReceta + 2).ToString();
            string OffsetEtapaAct = (OffsetReceta + 4).ToString();
            string OffsetOF = (OffsetReceta + 6).ToString();
            string OffsetNombreReceta = (OffsetReceta + 38).ToString();
            string OffsetNombreEtapa = (OffsetReceta + 140).ToString();
            string OffsetComm = (OffsetReceta + 172).ToString();
            string OffsetCheckRecetaFin = (OffsetReceta + (LongUDT - 2)).ToString();

            //Datos generales de la receta
            short NumEtapas;
            EtapaAct = ReadINT(DB, $"{OffsetEtapaAct}.0");
            string OF = ReadSTRING(DB, $"{OffsetOF}.0");
            string NombreReceta = ReadSTRING(DB, $"{OffsetNombreReceta}.0");
            string NombreEtapa;
            string RutaConsumoAPI = $"{RutaApi}{NombreReceta}";

            //Bits de comunicacion del gestor de recetas del PLC con el gestor de recetas del servicio
            bool InicioReceta = ReadBOOL(DB, $"{OffsetComm}.0");
            bool InicioEtapa = ReadBOOL(DB, $"{OffsetComm}.1");
            bool EtapaActualCargada = ReadBOOL(DB, $"{OffsetComm}.2");
            bool InicioCargado = ReadBOOL(DB, $"{OffsetComm}.3");
            bool SiguienteEtapaRecibida = ReadBOOL(DB, $"{OffsetComm}.4");
            bool UltimaEtapa = ReadBOOL(DB, $"{OffsetComm}.5");

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
                if (InicioReceta && !InicioCargado)
                {
                    EtapaAct = 0;
                    NumEtapas = Int16.Parse(await (DatosAPI.DatosCabecera(RutaConsumoAPI, "numeroEtapas")));
                    NombreEtapa = await (DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct + 1));

                    //Se cargan los datos de la primera etapa
                    WriteINT(DB, $"{OffsetNumEtapas}.0", NumEtapas);
                    WriteSTRING(DB, $"{OffsetNombreEtapa}.0", NombreEtapa, Logs);

                    await DatosAPI.DatosEtapas(DB, DB_Offsets, RutaConsumoAPI, EtapaAct + 1);

                    WriteBOOL(DB, $"{OffsetComm}.3", true); //BOOL Inicio Cargado

                    Logs.RegistrarInfo($"Inicio de la receta en el reactor: {NombreReactor}");

                }

                if (EtapaActualCargada && !InicioEtapa)
                {
                    EtapaAct++;
                    NumEtapas = ReadINT(DB, $"{OffsetNumEtapas}.0");

                    WriteBOOL(DB, $"{OffsetComm}.3", true);
                    WriteINT(DB, $"{OffsetEtapaAct}.0", EtapaAct);


                    if (EtapaAct >= NumEtapas)
                    {
                        WriteBOOL(DB, $"{OffsetComm}.5", true); //BOOL Ultima Etapa
                        Logs.RegistrarInfo($"Ultima etapa de la receta en el reactor: {NombreReactor}");
                    }
                    else
                    {
                        NombreEtapa = await (DatosAPI.DatosCabeceraEtapa(RutaConsumoAPI, "nombre", EtapaAct));
                        WriteSTRING(DB, $"{OffsetNombreEtapa}.0", NombreEtapa, Logs);
                        //Se cargan los datos de la siguiente etapa
                        await DatosAPI.DatosEtapas(DB, DB_Offsets, RutaConsumoAPI, EtapaAct + 1);

                    }
                    WriteBOOL(DB, $"{OffsetComm}.4", true);
                }

                if (SiguienteEtapaRecibida && !UltimaEtapa)
                {
                    NumEtapas = ReadINT(DB, $"{OffsetNumEtapas}.0");

                    if (EtapaAct >= NumEtapas)
                    {
                        WriteBOOL(DB, $"{OffsetComm}.5", true); //BOOL Ultima Etapa
                    }
                }
            }
            return EtapaAct;
        }
        public void CargaDatosReceta(string DB, string DB_Offsets, string NombrePropiedad, string ValorConsigna)
        {

            short OffsetReceta = ReadINT(DB_Offsets, "0.0");
            int OffsetProcesoPrincipal = OffsetReceta + 198;
            int OffsetProcesoAgitacion = OffsetReceta + 208;

            switch (NombrePropiedad)
            {
                case "tipo":
                    Tipo = ValorConsigna;
                    switch (ValorConsigna)
                    {
                        case "Carga":
                        case "Espera":
                            Puntero = OffsetProcesoPrincipal;
                            break;
                        case "Agitacion":
                            Puntero = OffsetProcesoAgitacion;
                            break;
                        default:
                            break;
                    }
                    break;

                case "consigna":
                    Consigna = ValorConsigna;
                    if (Tipo == "Carga" || Tipo == "Espera")
                    {
                        switch (ValorConsigna)
                        {
                            case "MateriaPrima":
                                Offset = 2;
                                break;
                            case "Cantidad":
                                Offset = 4;
                                break;
                            case "Tiempo":
                                Offset = 8;
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
                            case "Velocidad":
                                Offset = 4;
                                break;
                            case "Tiempo ON":
                                Offset = 10;
                                break;
                            case "Tiempo OFF":
                                Offset = 12;
                                break;
                        }
                    }
                    break;

                case "valor":
                    string DireccionConsignas = $"{(Puntero + Offset)}.0";
                    string ProcesoCarga = $"{Puntero}.0";
                    string ProcesoTiempo = $"{Puntero}.1";
                    string ProcesoSecundario = $"{Puntero}.0";

                    //Resets de todos los procesos secundarios

                    WriteBOOL(DB, $"{OffsetProcesoAgitacion.ToString()}.0", false);


                    //Activaciones de procesos
                    if (Tipo == "Carga" || Tipo == "Espera")
                    {
                        if (Tipo == "Carga")
                        {
                            WriteBOOL(DB, ProcesoCarga, true);
                            WriteBOOL(DB, ProcesoTiempo, false);
                        }
                        else
                        {
                            WriteBOOL(DB, ProcesoCarga, false);
                            WriteBOOL(DB, ProcesoTiempo, true);
                        }
                    }
                    else
                    {
                        WriteBOOL(DB, ProcesoSecundario, true);
                    }

                    //Traspaso de consignas
                    if (Consigna == "Cantidad" || Consigna == "Velocidad")
                    {

                        WriteFLOAT(DB, DireccionConsignas, Double.Parse(ValorConsigna));
                    }
                    else
                    {
                        WriteINT(DB, DireccionConsignas, Int16.Parse(ValorConsigna));
                    }
                    break;

                default:
                    Console.WriteLine("Opcion no valida");
                    break;
            }

        }
        #endregion




    }
}

using S7.Net;
using System.Net.NetworkInformation;

namespace ServicioWindows.Clases
{
    internal class Utiles
    {
        #region Revisables
        // ---------------------------------------------------------------------------------------------------------------------------

        public string GenerarFecha(Plc PLC, string[] Direccion, short PosicionInicioFecha, int ElementosFecha)
        {
            string FechaString;
            string[] FechaArray = new string[ElementosFecha];

            for (int i = 0; i < ElementosFecha; i++)
            {
                FechaArray[i] = Convert.ToString(PLC.Read(Direccion[i + PosicionInicioFecha]));
            }

            int Mes = int.Parse(FechaArray[1]);
            int Dia = int.Parse(FechaArray[2]);
            int Horas = int.Parse(FechaArray[3]);
            int Minutos = int.Parse(FechaArray[4]);
            int Segundos = int.Parse(FechaArray[5]);
            //Console.WriteLine($"int Mes:{Mes}");
            //Console.WriteLine($"int Dia:{Dia}");
            string mesFormateado = Mes.ToString("00");
            string diaFormateado = Dia.ToString("00");
            string horaFormateado = Horas.ToString("00");
            string minutoFormateado = Minutos.ToString("00");
            string segundoFormateado = Segundos.ToString("00");
            //Console.WriteLine($"string Mes:{mesFormateado}");
            //Console.WriteLine($"string Dia:{diaFormateado}");
            FechaString = $"{FechaArray[0]}-{mesFormateado}-{diaFormateado} {horaFormateado}:{minutoFormateado}:{segundoFormateado}";
            //Console.WriteLine($"string Fecha:{FechaString}");
            return FechaString;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        public void ComprobarDirectorios()
        {
            string rutaProyecto = Path.Combine(Directory.GetCurrentDirectory());
            string rutaLogs = $"{rutaProyecto}\\Logs";
            string rutaBBDD = $"{rutaProyecto}\\DataBase";

            if (!Directory.Exists(rutaLogs))
            {
                Directory.CreateDirectory(rutaLogs);
            }
            if (!Directory.Exists(rutaBBDD))
            {

                Directory.CreateDirectory(rutaBBDD);
            }

        }
        #endregion

        // ---------------------------------------------------------------------------------------------------------------------------

        // Verifica si un PLC responde a un ping para determinar su disponibilidad en la red.
        // Registra mensajes en logs si hay cambios en el estado de disponibilidad.
        
        public bool DisponibilidadPLC(Logs Logs, string IP, bool Disponible)
        {
            Ping ping = new Ping();
            PingReply reply;

            try
            {
                // Tiempo máximo para esperar la respuesta del ping en milisegundos (250ms)
                int timeout = 250; // 0.5 segundo

                // Envía el ping a la IP con el tiempo de espera definido
                reply = ping.Send(IP, timeout);

                // Si la respuesta es exitosa (el PLC está conectado)
                if (reply.Status == IPStatus.Success)
                {
                    // Si antes no estaba disponible, registrar que ahora sí está disponible
                    if (!Disponible)
                    {
                        Logs.RegistrarInfo($"Automata con IP:{IP} Disponible!");
                    }
                    // PLC disponible
                    return true;
                }
                else
                {
                    // Si antes estaba disponible, registrar que ahora NO está disponible
                    if (Disponible)
                    {
                        Logs.RegistrarError($"Automata con IP: {IP} NO Disponible!");
                    }
                    // PLC no disponible
                    return false;
                }
            }
            catch (PingException)
            {
                // Si ocurre algún error con el ping, se asume que no está disponible
                return false;
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------
    }
}

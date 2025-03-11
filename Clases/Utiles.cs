using S7.Net;
using System.Net.NetworkInformation;

namespace ServicioWindows.Clases
{
    internal class Utiles
    {
        #region Revisables
        public string GenerarFecha(Plc PLC, string[] Direccion,short PosicionInicioFecha,int ElementosFecha)
        {
            string FechaString;
            string [] FechaArray = new string[ElementosFecha];

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

        public bool DisponibilidadPLC(Logs Logs, string IP, bool Disponible)
        {
            Ping ping = new Ping();
            PingReply reply;

            try
            {
                // Establecer el tiempo de espera en milisegundos
                int timeout = 250; // 0.5 segundo

                reply = ping.Send(IP, timeout);

                if (reply.Status == IPStatus.Success)
                {
                    if (!Disponible)
                    {
                        Logs.RegistrarInfo($"Automata con IP:{IP} Disponible!");
                    }
                    
                    return true;
                }
                else
                {
                    if (Disponible)
                    {
                        Logs.RegistrarError($"Automata con IP: {IP} NO Disponible!");
                    }
                    
                    return false;
                }
            }
            catch (PingException)
            {
                return false;
            }
        }


    }
}

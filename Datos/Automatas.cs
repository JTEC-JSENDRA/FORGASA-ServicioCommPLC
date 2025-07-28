
// Se define el espacio de nombres donde está contenida la clase Automatas
namespace ServicioWindows.Datos
{
    // Se declara la clase Automatas, que contiene una lista de direcciones IP de PLCs (controladores lógicos programables)
    public class Automatas
    {
        // Lista privada que almacenará las direcciones IP de los PLCs de la planta
        private List<string> PLCs;

        // Constructor de la clase Automatas
        public Automatas()
        {
            // Se inicializa la lista de PLCs
            PLCs = new List<string>();

            // Aquí se agregan manualmente las direcciones IP de los PLCs que se van a utilizar en la planta
            // Puedes descomentar o agregar más líneas según los PLCs disponibles

            PLCs.Add("192.168.8.1");        // Ejemplo: PLC de la zona de Ratas muertas
            //PLCs.Add("10.10.40.30");      // PLC Simulacion
            //PLCs.Add("192.168.24.1");     // Ejemplo: PLC de la zona de sales

        }
        // Método público que permite obtener la lista de direcciones IP de los PLCs registrados
        public List<string> ObtenerPLCs()
        {
            return PLCs;
        }
    }
}

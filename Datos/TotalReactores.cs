
// Se define el espacio de nombres donde está contenida la clase TotalReactores
namespace ServicioWindows.Datos
{
    // Esta clase almacena la información de todos los reactores (nombre, IP del PLC y número de DB)
    public class TotalReactores
    {
        // Lista que almacenará arrays de strings con los datos de cada reactor
        private List<string[]> Reactores;

        // Constructor de la clase
        public TotalReactores()
        {
            // Se crea una instancia de la clase Automatas para obtener la IP del PLC
            Automatas PLC = new Automatas();
            
            // Se obtiene la primera IP de la lista de PLCs
            string PLC_Forgasa = PLC.ObtenerPLCs()[0];

            // Si hubiese más PLCs, podrías accederlos con el índice correspondiente
            // string PLC_Sales = PLC.ObtenerPLCs()[1];

            // Se inicializa la lista que almacenará los reactores
            Reactores = new List<string[]>();

            // Cada reactor se representa como un array de 3 elementos:
            // [0] = IP del PLC, [1] = nombre del reactor, [2] = número de DB usado en el PLC
            string[] RC01 = { PLC_Forgasa, "RC01", "8000" };
            string[] RC02 = { PLC_Forgasa, "RC02", "8001" };
            string[] RC03 = { PLC_Forgasa, "RC03", "8002" };
            string[] IM01 = { PLC_Forgasa, "IM01", "8003" };
            string[] IM02 = { PLC_Forgasa, "IM02", "8004" };

            // Se agregan todos los reactores definidos a la lista
            Reactores.Add(RC01);
            Reactores.Add(RC02);
            Reactores.Add(RC03);
            Reactores.Add(IM01);
            Reactores.Add(IM02);
        }

        // Método que devuelve la lista de reactores
        public List<string[]> ObtenerReactores()
        {
            return Reactores;
        }
    }
}


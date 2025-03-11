
namespace ServicioWindows.Datos
{
    public class Automatas
    {
        private List<string> PLCs;

        public Automatas()
        {
            PLCs = new List<string>();

            //Aqui se agregan los PLCs de la planta
            PLCs.Add("192.168.23.1");//PLC_Electrolito
            //PLCs.Add("192.168.24.1");//PLC_Sales

        }

        public List<string> ObtenerPLCs()
        {
            return PLCs;
        }
    }
}


namespace ServicioWindows.Datos
{
    public class Automatas
    {
        private List<string> PLCs;

        public Automatas()
        {
            PLCs = new List<string>();

            //Aqui se agregan los PLCs de la planta
            //PLCs.Add("192.168.8.1");//PLC_Electrolito
            PLCs.Add("10.10.40.30");
            //PLCs.Add("192.168.24.1");//PLC_Sales

        }

        public List<string> ObtenerPLCs()
        {
            return PLCs;
        }
    }
}

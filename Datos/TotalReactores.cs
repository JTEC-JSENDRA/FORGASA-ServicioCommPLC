
namespace ServicioWindows.Datos
{
    public class TotalReactores
    {
        private List<string[]> Reactores;

        public TotalReactores()
        {

            Automatas PLC = new Automatas();

            string PLC_Electrolito = PLC.ObtenerPLCs()[0];
            //string PLC_Sales = PLC.ObtenerPLCs()[1];


            Reactores = new List<string[]>();

            // Agregar el PLC, el nombre del reactor y el numero de DB utilizado
            string[] R8001 = { PLC_Electrolito, "R8001", "8000" };


            // Añadir los reactores a la lista
            Reactores.Add(R8001);

        }

        public List<string[]> ObtenerReactores()
        {
            return Reactores;
        }
    }
}


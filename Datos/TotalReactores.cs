
namespace ServicioWindows.Datos
{
    public class TotalReactores
    {
        private List<string[]> Reactores;

        public TotalReactores()
        {

            Automatas PLC = new Automatas();

            string PLC_Forgasa = PLC.ObtenerPLCs()[0];
            //string PLC_Sales = PLC.ObtenerPLCs()[1];


            Reactores = new List<string[]>();

            // Agregar el PLC, el nombre del reactor y el numero de DB utilizado
            string[] RC01 = { PLC_Forgasa, "RC01", "8000" };
            string[] RC02 = { PLC_Forgasa, "RC02", "8001" };
            string[] RC03 = { PLC_Forgasa, "RC03", "8002" };
            string[] IM01 = { PLC_Forgasa, "IM01", "8003" };
            string[] IM02 = { PLC_Forgasa, "IM02", "8004" };


            // Añadir los reactores a la lista
            Reactores.Add(RC01);
            Reactores.Add(RC02);
            Reactores.Add(RC03);
            Reactores.Add(IM01);
            Reactores.Add(IM02);


        }

        public List<string[]> ObtenerReactores()
        {
            return Reactores;
        }
    }
}



namespace ServicioWindows.Clases
{
    internal class Logs
    {
        private readonly string Ruta;

        // ---------------------------------------------------------------------------------------------------------------------------

        public Logs(string Ruta)
        {
            this.Ruta = Ruta;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        public void Iniciar(string Txt)
        {
            string Message = $"\n\n\n" +
                      $"******************************************************************************\n" +
                      $"******************** GMDix Servicio lectura/Escritura PLC ********************\n" +
                      $"****************************************************************************** ";

            File.AppendAllText(Ruta, Message);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(Message);
            Console.ResetColor();
            Message = $" \n\n{DateTimeOffset.Now}: Info - {Txt}\n\n";
            File.AppendAllText(Ruta, Message);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Message);
            Console.ResetColor();
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        public void RegistrarInfo(string Txt)
        {
            string Message = $"{DateTimeOffset.Now}: Info - {Txt}\n\n";
            File.AppendAllText(Ruta, Message);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Message);
            Console.ResetColor();
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        public void RegistrarError(string Txt)
        {
            string Message = $"{DateTimeOffset.Now}: Error - {Txt}\n\n";
            File.AppendAllText(Ruta, Message);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Message);
            Console.ResetColor();
        }

        // ---------------------------------------------------------------------------------------------------------------------------
    }
}

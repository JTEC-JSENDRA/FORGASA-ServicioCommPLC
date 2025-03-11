using System;
using System.Data;
using System.Data.SqlClient;

namespace ServicioWindows.Clases
{
    public class SQLServerManager : IDisposable
    {
        private readonly string connectionString;
        private SqlConnection connection;

        public SQLServerManager(string connectionString)
        {
            this.connectionString = connectionString;
            this.connection = new SqlConnection(connectionString);
        }

        private void OpenConnection()
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
        }

        private void CloseConnection()
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }

        private int[] ContarFilasColumnas(string NombreTabla)
        {
            string selectQuery = $"SELECT COUNT(*) AS row_count FROM {NombreTabla}; SELECT TOP 1 * FROM {NombreTabla}";
            int[] Procesos = new int[2];

            OpenConnection();

            using (var command = new SqlCommand(selectQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    // Leemos el resultado de la primer consulta
                    if (reader.Read())
                    {
                        Procesos[0] = reader.GetInt32(0);
                    }

                    // Leemos el resultado de la segunda consulta (una fila de la tabla)
                    if (reader.NextResult() && reader.Read())
                    {
                        Procesos[1] = reader.FieldCount;
                    }
                }
            }

            CloseConnection();

            return Procesos;
        }

        private void InsertTablaProceso(string Tabla, string[] DatosPLC, string ID_OF)
        {
            OpenConnection();
            //Console.WriteLine(Tabla);
            // Crear la sentencia SQL INSERT
            string insertQuery = $"INSERT INTO {Tabla} (ID_OF, ID_Texto, Valor, ID_Unidades, Timestamp) VALUES (@val1, @val2, @val3, @val4, @val5)";

            using (var command = new SqlCommand(insertQuery, connection))
            {
                // Agregar parámetros a la sentencia SQL para evitar inyección de SQL
                command.Parameters.AddWithValue("@val1", ID_OF);
                command.Parameters.AddWithValue("@val2", DatosPLC[0]);
                command.Parameters.AddWithValue("@val3", Math.Round(float.Parse(DatosPLC[1]), 2));
                command.Parameters.AddWithValue("@val4", DatosPLC[2]);
                command.Parameters.AddWithValue("@val5", DatosPLC[5]);

                // Ejecutar la sentencia SQL INSERT
                int rowsAffected = command.ExecuteNonQuery();

                // Verificar que se haya insertado correctamente
                if (rowsAffected > 0)
                {
                    Console.WriteLine("Registro insertado correctamente");
                }
                else
                {
                    Console.WriteLine("No se pudo insertar el registro");
                }
            }

            CloseConnection();
        }

        public string[,] ObtenerValoresTabla(string NombreTabla)
        {
            int[] FilasColumnas = ContarFilasColumnas(NombreTabla);
            int Filas = FilasColumnas[0];
            int Columnas = FilasColumnas[1];
            string[,] Resultado = new string [Filas, Columnas];
            int incremental = 0;

            OpenConnection();

            using (var command = new SqlCommand($"SELECT * FROM {NombreTabla}", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < Columnas; i++)
                        {
                            Resultado[incremental, i] = reader.GetValue(i).ToString();
                            //Console.WriteLine(reader.GetValue(i).ToString());
                        }
                        incremental++;
                    }
                }
            }

            CloseConnection();

            return Resultado;
        }

        public int ObtenerID(string NombreTabla, string WhereColumna, string Condicional)
        {
            int Resultado = 0;

            OpenConnection();

            using (var command = new SqlCommand($"SELECT ID FROM {NombreTabla} WHERE {WhereColumna} = '{Condicional}'", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Resultado = reader.GetInt32(0);
                    }
                }
            }

            CloseConnection();

            Console.WriteLine($"Resultado consulta ID_OF: {Resultado}");
            return Resultado;
        }

        public void RellenarTablasProcesos(string[,] DatosPLC, string[,] TablaBBDD, int ID_OF)
        {
            int nRegistrosPLC = DatosPLC.GetLength(0);
            int nDatosPLC = DatosPLC.GetLength(1);
            int nFilasTablaBBDD = TablaBBDD.GetLength(0);
            string stingID_OF = Convert.ToString(ID_OF);
            string TablaEscritura;
            string [] DatosEscritura = new string[nDatosPLC];

            OpenConnection();

            for (int i = 0; i < (nRegistrosPLC); i++)
            {
                for (int j = 0; j < (nFilasTablaBBDD); j++)
                {
                    if (DatosPLC[i, 3] == TablaBBDD[j, 0])
                    {
                        TablaEscritura = TablaBBDD[j, 1];
                        Console.WriteLine(TablaEscritura);
                        for (int k = 0; k < (nDatosPLC); k++)
                        {
                            DatosEscritura[k] = DatosPLC[i,k];
                            //Console.WriteLine(DatosEscritura[k]);
                        }
                        InsertTablaProceso(TablaEscritura, DatosEscritura, stingID_OF);
                    }
                }
            }

            CloseConnection();
        }

        public void Dispose()
        {
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
        }
    }
}










//private readonly string connectionString;

//public SQLiteManager(string connectionString)
//{
//    this.connectionString = connectionString;
//}

//#region Metodos privados
//private int[] ContarFilasColumnas(string NombreTabla)
//{
//    string selectQuery = $"SELECT COUNT(*) AS row_count FROM {NombreTabla}; SELECT * FROM {NombreTabla} LIMIT 1";
//    int[] Procesos = new int[2];

//    using (var connection = new SQLiteConnection(connectionString))
//    {
//        connection.Open();

//        using (var command = new SQLiteCommand(selectQuery, connection))
//        {
//            using (var reader = command.ExecuteReader())
//            {
//                // Leemos el resultado de la primer consulta
//                if (reader.Read())
//                {
//                    Procesos[0] = reader.GetInt32(0);
//                }

//                // Leemos el resultado de la segunda consulta (una fila de la tabla)
//                if (reader.NextResult() && reader.Read())
//                {
//                    Procesos[1] = reader.FieldCount;
//                }
//            }
//        }
//        connection.Close();
//        return Procesos;
//    }

//}
//private void InsertTablaProceso(string Tabla, string[] DatosPLC, string ID_OF)
//{
//    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
//    {
//        connection.Open();

//        // Crear la sentencia SQL INSERT
//        string insertQuery = $"INSERT INTO {Tabla} (ID_OF, ID_Texto, Valor, ID_Unidades, Timestamp) VALUES (@val1, @val2, @val3, @val4, @val5)";

//        // Crear el objeto SQLiteCommand y asignar la conexión y la sentencia SQL
//        using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
//        {
//            //Console.WriteLine(DatosPLC[1]);
//            // Agregar parámetros a la sentencia SQL para evitar inyección de SQL
//            command.Parameters.AddWithValue("@val1", ID_OF);
//            command.Parameters.AddWithValue("@val2", DatosPLC[0]);
//            command.Parameters.AddWithValue("@val3", Math.Round(float.Parse(DatosPLC[1]), 2));
//            command.Parameters.AddWithValue("@val4", DatosPLC[2]);
//            command.Parameters.AddWithValue("@val5", DatosPLC[5]);

//            // Ejecutar la sentencia SQL INSERT
//            int rowsAffected = command.ExecuteNonQuery();

//            // Verificar que se haya insertado correctamente
//            if (rowsAffected > 0)
//            {
//                Console.WriteLine("Registro insertado correctamente");
//            }
//            else
//            {
//                Console.WriteLine("No se pudo insertar el registro");
//            }
//        }
//        connection.Close();
//    }
//}
//#endregion

//public string[,] ObtenerValoresTabla(string NombreTabla)
//{
//    int[] FilasColumnas = ContarFilasColumnas(NombreTabla);
//    int Filas = FilasColumnas[0];
//    int Columnas = FilasColumnas[1];
//    string[,] Resultado = new string[Filas, Columnas];
//    int incremental = 0;

//    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
//    {
//        connection.Open();

//        SQLiteCommand command = new SQLiteCommand($"SELECT * FROM {NombreTabla}", connection);
//        SQLiteDataReader reader = command.ExecuteReader();

//        while (reader.Read())
//        {
//            for (int i = 0; i < Columnas; i++)
//            {
//                Resultado[incremental, i] = reader.GetValue(i).ToString();
//                //Console.WriteLine(reader.GetValue(i).ToString());
//            }
//            incremental++;
//        }
//        connection.Close();
//    }
//    return Resultado;
//}
//public int ObtenerID(string NombreTabla, string WhereColumna, string Condicional)
//{
//    int Resultado = 0;

//    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
//    {
//        connection.Open();

//        SQLiteCommand command = new SQLiteCommand($"SELECT ID FROM {NombreTabla} WHERE {WhereColumna} = '{Condicional}'", connection);
//        SQLiteDataReader reader = command.ExecuteReader();

//        while (reader.Read())
//        {
//            Resultado = reader.GetInt32(0);
//        }

//        connection.Close();
//    }
//    Console.WriteLine($"Resultado consulta ID_OF: {Resultado}");
//    return Resultado;
//}

//public void RellenarTablasProcesos(string[,] DatosPLC, string[,] TablaBBDD, int ID_OF)
//{
//    int nRegistrosPLC = DatosPLC.GetLength(0);
//    int nDatosPLC = DatosPLC.GetLength(1);
//    int nFilasTablaBBDD = TablaBBDD.GetLength(1);
//    string stingID_OF = Convert.ToString(ID_OF);
//    string TablaEscritura;
//    string[] DatosEscritura = new string[nDatosPLC];

//    for (int i = 0; i < (nRegistrosPLC); i++)
//    {
//        for (int j = 0; j < (nFilasTablaBBDD); j++)
//        {
//            if (DatosPLC[i, 3] == TablaBBDD[j, 0])
//            {
//                TablaEscritura = TablaBBDD[j, 1];
//                for (int k = 0; k < (nDatosPLC); k++)
//                {
//                    DatosEscritura[k] = DatosPLC[i, k];
//                }
//                InsertTablaProceso(TablaEscritura, DatosEscritura, stingID_OF);
//            }
//        }
//    }
//}
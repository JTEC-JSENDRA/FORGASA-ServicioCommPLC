using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using ServicioWindows.Models;

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
                    //Console.WriteLine("Registro insertado correctamente");
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
            string[,] Resultado = new string[Filas, Columnas];
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

            //Console.WriteLine($"Resultado consulta ID_OF: {Resultado}");
            return Resultado;
        }

        public void RellenarTablasProcesos(string[,] DatosPLC, string[,] TablaBBDD, int ID_OF)
        {
            int nRegistrosPLC = DatosPLC.GetLength(0);
            int nDatosPLC = DatosPLC.GetLength(1);
            int nFilasTablaBBDD = TablaBBDD.GetLength(0);
            string stingID_OF = Convert.ToString(ID_OF);
            string TablaEscritura;
            string[] DatosEscritura = new string[nDatosPLC];

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
                            DatosEscritura[k] = DatosPLC[i, k];
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
        public async Task ActualizarOrdenFabricacionMMPP( string Destino, string OF)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                                UPDATE DatosRealesMMPP
                                SET OrdenFabricacion = @OF
                                WHERE Destino = @Destino";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OF", OF);
                    command.Parameters.AddWithValue("@Destino", Destino);

                    await command.ExecuteNonQueryAsync(); // Ejecuta sin devolver resultados
                }
            }
        }
        public async Task ActualizaCantidadMMPP(string Destino, float Solidos1, float Solidos2, float Solidos3, float Agua, float AguaRecu, float Antiespumante, float Ligno, float Potasa)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                                UPDATE DatosRealesMMPP
                                SET Solido1 = @Solidos1,
                                Solido2 = @Solidos2,
                                Solido3 = @Solidos3,
                                Agua = @Agua,
                                AguaRecu = @AguaRecu,
                                Antiespumante = @Antiespumante,
                                Ligno = @Ligno,
                                Potasa = @Potasa
                                WHERE Destino = @Destino";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Solidos1", Solidos1);
                    command.Parameters.AddWithValue("@Solidos2", Solidos2);
                    command.Parameters.AddWithValue("@Solidos3", Solidos3);
                    command.Parameters.AddWithValue("@Agua", Agua);
                    command.Parameters.AddWithValue("@AguaRecu", AguaRecu);
                    command.Parameters.AddWithValue("@Antiespumante", Antiespumante);
                    command.Parameters.AddWithValue("@Ligno", Ligno);
                    command.Parameters.AddWithValue("@Potasa", Potasa);

                    command.Parameters.AddWithValue("@Destino", Destino);

                    await command.ExecuteNonQueryAsync(); // Ejecuta sin devolver resultados
                }
            }
        }

        public async Task<string> ExtraerMMPP_Teoricas(string OF)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                        SELECT 
                            materiaPrima, 
                            cantidad
                        FROM 
                            Materiales
                        WHERE 
                            ordenFabricacion = @OF";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OF", OF);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        // Variables float
                        float Solido_1 = 0;
                        float Solido_2 = 0;
                        float Solido_3 = 0;
                        float Agua = 0;
                        float AguaRecu = 0;
                        float Antiespumante = 0;
                        float Lignosulfonato = 0;
                        float Potasa = 0;

                        while (await reader.ReadAsync())
                        {
                            string materia = reader.GetString(0).Trim().ToUpper();

                            // Asegúrate de que la columna cantidad es FLOAT en SQL Server
                            float cantidad = Convert.ToSingle(reader.GetDouble(1));
                            // ASEGURANOS DE METER EL MISMO NOMBRE UQE LEEMOS DE SAP
                            switch (materia)
                            {
                                case "LC70-01":
                                    Solido_1 = cantidad;
                                    break;
                                case "LC80-01":
                                    Solido_2 = cantidad;
                                    break;
                                case "HL26(10-16)(0-0-8)-01":
                                    Solido_3 = cantidad;
                                    break;
                                case "AGUA":
                                    Agua = cantidad;
                                    break;
                                case "AGUA RECUPERADA":
                                    AguaRecu = cantidad;
                                    break;
                                case "HL PRUEBAS":
                                    Antiespumante = cantidad;
                                    break;
                                case "CALCIO LIGNOSULFONATO SOLIDO":
                                    Lignosulfonato = cantidad;
                                    break;
                                case "POTASA LIQUIDA 50%":
                                    Potasa = cantidad;
                                    break;
                                case "POTASA LIQUIDA 47%":
                                    Potasa = cantidad;
                                    break;
                            }
                        }

                        var datos = new DatosMMPP
                        {
                            OF = OF,
                            Solido_1 = Solido_1,
                            Solido_2 = Solido_2,
                            Solido_3 = Solido_3,
                            Agua = Agua,
                            AguaRecu = AguaRecu,
                            Antiespumante = Antiespumante,
                            Lignosulfonato = Lignosulfonato,
                            Potasa = Potasa
                        };

                        return JsonConvert.SerializeObject(datos);
                    }
                }
            }
        }

        public async Task Trazabilidad_Final(DatosMMPP Resultado_Real, DatosMMPP Resultado_Teorico)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                                INSERT INTO MMPP_Finales (
                                    OrdenFabricacion, Solidos_1_CT, Solidos_1_CR,
                                    Solidos_2_CT, Solidos_2_CR,
                                    Solidos_3_CT, Solidos_3_CR,
                                    Agua_CT, Agua_CR,
                                    Agua_Recu_CT, Agua_Recu_CR,
                                    Antiespumante_CT, Antiespumante_CR,
                                    Ligno_CT, Ligno_CR,
                                    Potasa_CT, Potasa_CR
                                ) VALUES (
                                    @OrdenFabricacion, @Solido_1_CT, @Solido_1_CR,
                                    @Solido_2_CT, @Solido_2_CR,
                                    @Solido_3_CT, @Solido_3_CR,
                                    @Agua_CT, @Agua_CR,
                                    @Agua_Recu_CT, @Agua_Recu_CR,
                                    @Antiespumante_CT, @Antiespumante_CR,
                                    @Ligno_CT, @Ligno_CR,
                                    @Potasa_CT, @Potasa_CR
                                )";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OrdenFabricacion", Resultado_Real.OF);
                    command.Parameters.AddWithValue("@Solido_1_CT", Resultado_Teorico.Solido_1);
                    command.Parameters.AddWithValue("@Solido_1_CR", Resultado_Real.Solido_1);
                    command.Parameters.AddWithValue("@Solido_2_CT", Resultado_Teorico.Solido_2);
                    command.Parameters.AddWithValue("@Solido_2_CR", Resultado_Real.Solido_2);
                    command.Parameters.AddWithValue("@Solido_3_CT", Resultado_Teorico.Solido_3);
                    command.Parameters.AddWithValue("@Solido_3_CR", Resultado_Real.Solido_3);
                    command.Parameters.AddWithValue("@Agua_CT", Resultado_Teorico.Agua);
                    command.Parameters.AddWithValue("@Agua_CR", Resultado_Real.Agua);
                    command.Parameters.AddWithValue("@Agua_Recu_CT", Resultado_Teorico.AguaRecu);
                    command.Parameters.AddWithValue("@Agua_Recu_CR", Resultado_Real.AguaRecu);
                    command.Parameters.AddWithValue("@Antiespumante_CT", Resultado_Teorico.Antiespumante);
                    command.Parameters.AddWithValue("@Antiespumante_CR", Resultado_Real.Antiespumante);
                    command.Parameters.AddWithValue("@Ligno_CT", Resultado_Teorico.Lignosulfonato);
                    command.Parameters.AddWithValue("@Ligno_CR", Resultado_Real.Lignosulfonato);
                    command.Parameters.AddWithValue("@Potasa_CT", Resultado_Teorico.Potasa);
                    command.Parameters.AddWithValue("@Potasa_CR", Resultado_Real.Potasa);

                    await command.ExecuteNonQueryAsync(); // Ejecuta sin devolver resultados
                }
            }
        }

        public async Task<DatosCantidades> ExtraerMMPP_Cantidades(string OF)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                                SELECT 
                                    materiaPrima, 
                                    cantidad
                                FROM 
                                    Materiales
                                WHERE 
                                    ordenFabricacion = @OF";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@OF", OF);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        float Solido_1 = 0, Solido_2 = 0, Solido_3 = 0;
                        float Agua = 0, AguaRecu = 0;
                        float Antiespumante = 0, Lignosulfonato = 0, Potasa = 0;
                        // ASEGURANOS DE METER EL MISMO NOMBRE UQE LEEMOS DE SAP
                        while (await reader.ReadAsync())
                        {
                            string materia = reader.GetString(0).Trim().ToUpper();
                            float cantidad = Convert.ToSingle(reader.GetDouble(1));

                            switch (materia)
                            {
                                case "LC70-01": Solido_1 = cantidad; break;
                                case "LC80-01": Solido_2 = cantidad; break;
                                case "HL26(10-16)(0-0-8)-01": Solido_3 = cantidad; break;
                                case "AGUA": Agua = cantidad; break;
                                case "AGUA RECUPERADA": AguaRecu = cantidad; break;
                                case "HL PRUEBAS": Antiespumante = cantidad; break;
                                case "CALCIO LIGNOSULFONATO SOLIDO": Lignosulfonato = cantidad; break;
                                case "POTASA LIQUIDA 50%":
                                case "POTASA LIQUIDA 47%": Potasa = cantidad; break;
                            }
                        }

                        return new DatosCantidades
                        {
                            Solido_1 = Solido_1,
                            Solido_2 = Solido_2,
                            Solido_3 = Solido_3,
                            Agua = Agua,
                            AguaRecu = AguaRecu,
                            Antiespumante = Antiespumante,
                            Lignosulfonato = Lignosulfonato,
                            Potasa = Potasa
                        };
                    }
                }
            }
        }

    }
}


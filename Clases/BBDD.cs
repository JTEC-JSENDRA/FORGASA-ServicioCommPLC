using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using ServicioWindows.Models;
using GestionRecetas.Models;

namespace ServicioWindows.Clases
{
    public class SQLServerManager : IDisposable
    {
        private readonly string connectionString;
        private SqlConnection connection;

        // ---------------------------------------------------------------------------------------------------------------------------

        // Esta clase gestiona la conexión a una base de datos SQL Server.
        // Se inicializa con una cadena de conexión que contiene los datos necesarios para conectarse.
        // El constructor crea el objeto de conexión que luego se usará para comunicarse con la base de datos.

        public SQLServerManager(string connectionString)
        {
            // Guardamos la cadena de conexión que nos llega como parámetro (servidor, base de datos, usuario, etc.)
            this.connectionString = connectionString;
            // Creamos un nuevo objeto SqlConnection utilizando la cadena de conexión proporcionada
            // Este objeto se usará más adelante para abrir y cerrar la conexión con la base de datos
            this.connection = new SqlConnection(connectionString);
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Este método se asegura de que la conexión con la base de datos esté abierta.
        // Solo abre la conexión si aún no está abierta, evitando errores o reaperturas innecesarias.

        private void OpenConnection()
        {
            // Verificamos si la conexión aún no está abierta
            if (connection.State != ConnectionState.Open)
            {
                // Si está cerrada, la abrimos para poder hacer consultas a la base de datos
                connection.Open();
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Este método cierra la conexión a la base de datos si aún está abierta.
        // Ayuda a liberar recursos y evitar bloqueos en el sistema.
        // Siempre se recomienda cerrar conexiones después de usarlas.

        private void CloseConnection()
        {
            // Verificamos si la conexión aún no está cerrada
            if (connection.State != ConnectionState.Closed)
            {
                // Si está abierta, la cerramos para liberar recursos
                connection.Close();
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Este método cuenta cuántas filas y columnas tiene una tabla en la base de datos.
        // Devuelve un arreglo con dos valores: [0] = número de filas, [1] = número de columnas.
        // Se hace una consulta SQL y se analiza el resultado.

        private int[] ContarFilasColumnas(string NombreTabla)
        {
            // Consulta SQL para contar filas y obtener una fila para saber cuántas columnas hay
            string selectQuery = $"SELECT COUNT(*) AS row_count FROM {NombreTabla}; SELECT TOP 1 * FROM {NombreTabla}";
            // [0] = filas, [1] = columnas
            int[] Procesos = new int[2];

            // Abrimos la conexión a la base de datos
            OpenConnection();

            using (var command = new SqlCommand(selectQuery, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    // Leemos el resultado de la primera consulta: número de filas
                    if (reader.Read())
                    {
                        // Guardamos el número de filas
                        Procesos[0] = reader.GetInt32(0);
                    }

                    // Pasamos al siguiente resultado: una fila para contar columnas
                    if (reader.NextResult() && reader.Read())
                    {
                        // Guardamos el número de columnas
                        Procesos[1] = reader.FieldCount;
                    }
                }
            }
            // Cerramos la conexión después de usarla
            CloseConnection();
            // Devolvemos los datos
            return Procesos;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Este método inserta un nuevo registro en una tabla específica de la base de datos.
        // Recibe el nombre de la tabla, un arreglo con datos del PLC y un ID para la orden de fabricación.
        // Usa parámetros para evitar ataques de inyección SQL y maneja la apertura y cierre de la conexión.

        private void InsertTablaProceso(string Tabla, string[] DatosPLC, string ID_OF)
        {
            // Abrimos la conexión a la base de datos
            OpenConnection();
            
            // Crear la consulta SQL para insertar datos en la tabla indicada
            string insertQuery = $"INSERT INTO {Tabla} (ID_OF, ID_Texto, Valor, ID_Unidades, Timestamp) VALUES (@val1, @val2, @val3, @val4, @val5)";

            using (var command = new SqlCommand(insertQuery, connection))
            {
                // Añadimos los valores a los parámetros de la consulta para evitar problemas de seguridad
                command.Parameters.AddWithValue("@val1", ID_OF);
                command.Parameters.AddWithValue("@val2", DatosPLC[0]);
                command.Parameters.AddWithValue("@val3", Math.Round(float.Parse(DatosPLC[1]), 2));  // Convertir y redondear valor numérico
                command.Parameters.AddWithValue("@val4", DatosPLC[2]);
                command.Parameters.AddWithValue("@val5", DatosPLC[5]);

                // Ejecutamos la consulta para insertar el registro
                int rowsAffected = command.ExecuteNonQuery();

                // Comprobamos si la inserción fue exitosa
                if (rowsAffected > 0)
                {
                    // Registro insertado correctamente (se puede activar el Console.WriteLine si se quiere mostrar)
                }
                else
                {
                    // Mensaje de error si no se inserta nada
                    Console.WriteLine("No se pudo insertar el registro");
                }
            }
            // Cerramos la conexión después de insertar
            CloseConnection();
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Este método lee todos los datos de una tabla en la base de datos y los guarda en una matriz de cadenas.
        // Primero obtiene el número de filas y columnas para crear la matriz, luego abre la conexión y lee cada fila y columna.

        public string[,] ObtenerValoresTabla(string NombreTabla)
        {
            // Obtener el número de filas y columnas de la tabla
            int[] FilasColumnas = ContarFilasColumnas(NombreTabla);
            int Filas = FilasColumnas[0];
            int Columnas = FilasColumnas[1];
            // Crear una matriz para guardar los resultados (filas x columnas)
            string[,] Resultado = new string[Filas, Columnas];
            // Para contar la fila actual al llenar la matriz
            int incremental = 0;

            // Abrimos la conexión a la base de datos
            OpenConnection();

            using (var command = new SqlCommand($"SELECT * FROM {NombreTabla}", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    // Leer cada fila de la tabla
                    while (reader.Read())
                    {
                        // Leer cada columna de la fila actual
                        for (int i = 0; i < Columnas; i++)
                        {
                            // Guardar el valor como texto
                            Resultado[incremental, i] = reader.GetValue(i).ToString();
                            // Console.WriteLine(reader.GetValue(i).ToString()); // (Opcional) Mostrar en consola cada valor
                        }
                        // Pasar a la siguiente fila
                        incremental++;
                    }
                }
            }
            // Cerrar la conexión
            CloseConnection();
            // Devolver la matriz con todos los datos de la tabla
            return Resultado;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método que busca y devuelve el valor del campo "ID" de una tabla,
        // aplicando un filtro (condición) en una columna específica.

        public int ObtenerID(string NombreTabla, string WhereColumna, string Condicional)
        {
            // Variable para guardar el ID encontrado
            int Resultado = 0;

            // Abrimos la conexión a la base de datos
            OpenConnection();

            // Crear la consulta SQL para buscar el ID con la condición dada
            using (var command = new SqlCommand($"SELECT ID FROM {NombreTabla} WHERE {WhereColumna} = '{Condicional}'", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    // Leer los resultados de la consulta (debería ser solo uno)
                    while (reader.Read())
                    {
                        // Obtener el valor de la primera columna (ID)
                        Resultado = reader.GetInt32(0);
                    }
                }
            }
            // Cerrar la conexión a la base de datos
            CloseConnection();
            // Devolver el ID encontrado (0 si no se encontró)
            return Resultado;
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método que compara datos de PLC con tablas de la base de datos,
        // y llama a un método para insertar datos en la tabla correspondiente.

        public void RellenarTablasProcesos(string[,] DatosPLC, string[,] TablaBBDD, int ID_OF)
        {
            int nRegistrosPLC = DatosPLC.GetLength(0);             // Número de filas (registros) en DatosPLC
            int nDatosPLC = DatosPLC.GetLength(1);                 // Número de columnas (datos) en DatosPLC
            int nFilasTablaBBDD = TablaBBDD.GetLength(0);          // Número de filas en TablaBBDD
            string stingID_OF = Convert.ToString(ID_OF);           // Convertir el ID a string para usarlo al insertar
            string TablaEscritura;                                 // Variable para guardar el nombre de la tabla destino
            string[] DatosEscritura = new string[nDatosPLC];       // Array para almacenar datos a insertar

            // Abrir conexión a la base de datos
            OpenConnection();

            // Recorremos cada registro de DatosPLC
            for (int i = 0; i < (nRegistrosPLC); i++)
            {
                // Para cada registro de DatosPLC, recorremos todas las filas de TablaBBDD
                for (int j = 0; j < (nFilasTablaBBDD); j++)
                {
                    // Si el valor en la columna 3 de DatosPLC coincide con la primera columna de TablaBBDD
                    if (DatosPLC[i, 3] == TablaBBDD[j, 0])
                    {
                        // Guardamos el nombre de la tabla destino
                        TablaEscritura = TablaBBDD[j, 1];
                        Console.WriteLine(TablaEscritura);
                        // Copiamos todos los datos del registro PLC a un array temporal
                        for (int k = 0; k < (nDatosPLC); k++)
                        {
                            DatosEscritura[k] = DatosPLC[i, k];                        
                        }
                        // Insertamos los datos en la tabla correspondiente con el ID dado
                        InsertTablaProceso(TablaEscritura, DatosEscritura, stingID_OF);
                    }
                }
            }
            // Cerrar conexión a la base de datos
            CloseConnection();
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método para liberar los recursos usados por la conexión a la base de datos

        public void Dispose()
        {
            // Verifica si la conexión existe
            if (connection != null)
            {
                // Libera los recursos de la conexión
                connection.Dispose();
                // Limpia la referencia para evitar usarla después
                connection = null;
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Actualiza la orden de fabricación para un destino específico en la base de datos de manera asíncrona

        public async Task ActualizarOrdenFabricacionMMPP( string Destino, string OF)
        {
            // Crear una nueva conexión a la base de datos usando la cadena de conexión
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Abrir la conexión de forma asíncrona para no bloquear el programa
                await connection.OpenAsync();

                // Consulta SQL para actualizar la columna OrdenFabricacion en la tabla DatosRealesMMPP
                string query = @"
                                UPDATE DatosRealesMMPP
                                SET OrdenFabricacion = @OF
                                WHERE Destino = @Destino";

                // Crear el comando SQL con la consulta y la conexión abierta
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Añadir los valores a los parámetros para evitar inyección SQL
                    command.Parameters.AddWithValue("@OF", OF);
                    command.Parameters.AddWithValue("@Destino", Destino);

                    // Ejecutar la consulta de forma asíncrona (no devuelve resultados)
                    await command.ExecuteNonQueryAsync(); // Ejecuta sin devolver resultados
                }
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Actualiza las cantidades de varios materiales para un destino específico en la base de datos de manera asíncrona

        public async Task ActualizaCantidadMMPP(string Destino, float Solidos1, float Solidos2, float Solidos3, float Agua, float AguaRecu, float Antiespumante, float Ligno, float Potasa)
        {
            // Crear una nueva conexión a la base de datos usando la cadena de conexión
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Abrir la conexión de forma asíncrona para no bloquear el programa mientras se conecta
                await connection.OpenAsync();

                // Consulta SQL para actualizar las cantidades de los materiales en la tabla DatosRealesMMPP 
                // En caso de nuevas MMPP hay que modificar la query con las nuevas MMPP
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

                // Crear el comando SQL con la consulta y la conexión abierta
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Añadir los valores a los parámetros para evitar inyección SQL
                    command.Parameters.AddWithValue("@Solidos1", Solidos1);
                    command.Parameters.AddWithValue("@Solidos2", Solidos2);
                    command.Parameters.AddWithValue("@Solidos3", Solidos3);
                    command.Parameters.AddWithValue("@Agua", Agua);
                    command.Parameters.AddWithValue("@AguaRecu", AguaRecu);
                    command.Parameters.AddWithValue("@Antiespumante", Antiespumante);
                    command.Parameters.AddWithValue("@Ligno", Ligno);
                    command.Parameters.AddWithValue("@Potasa", Potasa);
                    command.Parameters.AddWithValue("@Destino", Destino);

                    // Ejecutar la consulta de forma asíncrona (no devuelve resultados)
                    await command.ExecuteNonQueryAsync(); 
                }
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método asíncrono para obtener las cantidades teóricas de materias primas según una orden de fabricación (OF)

        public async Task<string> ExtraerMMPP_Teoricas(string OF)
        {
            // Crear y abrir una conexión a la base de datos
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Consulta SQL para obtener materia prima y cantidad según la orden de fabricación
                string query = @"
                        SELECT 
                            materiaPrima, 
                            cantidad
                        FROM 
                            Materiales
                        WHERE 
                            ordenFabricacion = @OF";

                // Crear comando SQL con la consulta y la conexión abierta
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Evitar inyección SQL añadiendo el parámetro de la orden de fabricación
                    command.Parameters.AddWithValue("@OF", OF);

                    // Ejecutar la consulta y obtener el lector de datos de forma asíncrona
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        // Variables para almacenar las cantidades de cada materia prima
                        float Solido_1 = 0;
                        float Solido_2 = 0;
                        float Solido_3 = 0;
                        float Agua = 0;
                        float AguaRecu = 0;
                        float Antiespumante = 0;
                        float Lignosulfonato = 0;
                        float Potasa = 0;

                        // Leer fila por fila mientras haya datos
                        while (await reader.ReadAsync())
                        {
                            // Leer el nombre de la materia prima y convertirlo a mayúsculas para comparación
                            string materia = reader.GetString(0).Trim().ToUpper();

                            // Leer la cantidad, asegurando que es float (se lee como double y se convierte a float)
                            float cantidad = Convert.ToSingle(reader.GetDouble(1));

                            // Asignar la cantidad a la variable correcta según el nombre de la materia prima
                            // Tener en cuenta que si se modifican las MMPP hay que añadirlas con el mismo nombre que recibimos de SAP
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

                        // Crear un objeto con todos los datos recogidos
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

                        // Convertir el objeto a JSON y devolverlo como string
                        return JsonConvert.SerializeObject(datos);
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método asíncrono que guarda en la base de datos la comparación entre las cantidades teóricas y reales de materias primas

        public async Task Trazabilidad_Final(DatosMMPP Resultado_Real, DatosMMPP Resultado_Teorico)
        {
            // Crear y abrir la conexión a la base de datos
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Consulta SQL para insertar los datos en la tabla MMPP_Finales
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

                // Preparar el comando con la consulta y la conexión
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Asignar valores a los parámetros del comando para evitar inyección SQL
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

                    // Ejecutar el comando de inserción sin esperar resultados
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método asíncrono que obtiene las cantidades de materias primas para una orden de fabricación

        public async Task<DatosCantidades> ExtraerMMPP_Cantidades(string OF)
        {
            // Crear y abrir conexión a la base de datos
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Consulta para obtener materia prima y cantidad según la orden de fabricación
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
                    // Añadir parámetro para evitar inyección SQL
                    command.Parameters.AddWithValue("@OF", OF);

                    // Ejecutar consulta y obtener resultado
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        // Variables para guardar las cantidades de cada materia prima
                        float Solido_1 = 0, Solido_2 = 0, Solido_3 = 0;
                        float Agua = 0, AguaRecu = 0;
                        float Antiespumante = 0, Lignosulfonato = 0, Potasa = 0;

                        // ASEGURANOS DE METER EL MISMO NOMBRE UQE LEEMOS DE SAP
                        // Leer todas las filas retornadas
                        while (await reader.ReadAsync())
                        {
                            // Obtener el nombre de la materia prima y convertir a mayúsculas para comparar
                            string materia = reader.GetString(0).Trim().ToUpper();
                            // Obtener la cantidad (asegurarse que en la base de datos es tipo FLOAT o DOUBLE)
                            float cantidad = Convert.ToSingle(reader.GetDouble(1));

                            // Asignar la cantidad a la variable correspondiente según el nombre de la materia prima
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

                        // Crear y devolver un objeto con todas las cantidades recogidas
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

        // ---------------------------------------------------------------------------------------------------------------------------

        // Método asíncrono que obtiene los umbrales de error de cada MMPP

        public async Task<UmbralesRequest> ObtenerUmbrales()
        {
            UmbralesRequest umbrales = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT TOP 1 
                            lc70, lc80, hl26, agua, aguaRecuperada, 
                            antiespumante, ligno, potasa 
                         FROM Umbrales";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        umbrales = new UmbralesRequest
                        {
                            lc70 = reader.GetDouble(reader.GetOrdinal("lc70")),
                            lc80 = reader.GetDouble(reader.GetOrdinal("lc80")),
                            hl26 = reader.GetDouble(reader.GetOrdinal("hl26")),
                            agua = reader.GetDouble(reader.GetOrdinal("agua")),
                            aguaRecuperada = reader.GetDouble(reader.GetOrdinal("aguaRecuperada")),
                            antiespumante = reader.GetDouble(reader.GetOrdinal("antiespumante")),
                            ligno = reader.GetDouble(reader.GetOrdinal("ligno")),
                            potasa = reader.GetDouble(reader.GetOrdinal("potasa"))
                        };
                    }
                }
            }

            return umbrales;
        }

        // ---------------------------------------------------------------------------------------------------------------------------
    }
}


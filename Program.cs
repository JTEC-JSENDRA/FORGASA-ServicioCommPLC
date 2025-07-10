
// 1. Se crea una aplicación como servicio de Windows que puede ejecutarse en segundo plano.
// 2. Se registra la clase Worker como servicio principal que contiene la lógica del programa.
// 3. Se inicia la ejecución del servicio de forma asíncrona al arrancar el sistema.

// Se importa el espacio de nombres del proyecto, donde está definida la clase Worker.
using ServicioWindows;

// Se crea un host (entorno) para ejecutar el servicio.
IHost host = Host.CreateDefaultBuilder(args)     // Crea el host con configuración por defecto (como logs, lectura de config, etc.)
    .ConfigureServices(services =>               // Aquí se configuran los servicios que usará la aplicación.
    {
        // Se agrega la clase Worker como servicio que se ejecutará en segundo plano.
        services.AddHostedService<Worker>();
    })
    .UseWindowsService()                         // Indica que este programa se ejecutará como un servicio de Windows.
    .Build();                                    // Se construye el host con toda la configuración anterior.

// Se arranca el servicio de forma asíncrona (sin bloquear el hilo principal).
await host.RunAsync();

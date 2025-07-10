
// 1. Se crea una aplicaci�n como servicio de Windows que puede ejecutarse en segundo plano.
// 2. Se registra la clase Worker como servicio principal que contiene la l�gica del programa.
// 3. Se inicia la ejecuci�n del servicio de forma as�ncrona al arrancar el sistema.

// Se importa el espacio de nombres del proyecto, donde est� definida la clase Worker.
using ServicioWindows;

// Se crea un host (entorno) para ejecutar el servicio.
IHost host = Host.CreateDefaultBuilder(args)     // Crea el host con configuraci�n por defecto (como logs, lectura de config, etc.)
    .ConfigureServices(services =>               // Aqu� se configuran los servicios que usar� la aplicaci�n.
    {
        // Se agrega la clase Worker como servicio que se ejecutar� en segundo plano.
        services.AddHostedService<Worker>();
    })
    .UseWindowsService()                         // Indica que este programa se ejecutar� como un servicio de Windows.
    .Build();                                    // Se construye el host con toda la configuraci�n anterior.

// Se arranca el servicio de forma as�ncrona (sin bloquear el hilo principal).
await host.RunAsync();

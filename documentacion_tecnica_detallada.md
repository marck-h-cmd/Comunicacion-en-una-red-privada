# Documentación Técnica Detallada (Sustentación del Proyecto)

Este documento contiene la explicación profunda de la lógica de negocio y arquitectura de red de la aplicación. Está diseñado para proveer los argumentos técnicos necesarios para sustentar el proyecto, demostrando el uso de patrones de diseño, principios SOLID, multihilos y sockets asíncronos.

---

## 1. Arquitectura General y Topología
El proyecto implementa una **Topología en Estrella (Cliente-Servidor)**. Los clientes nunca se conectan directamente entre sí (P2P), sino que todas las tramas de red viajan hacia el **Servidor Central**, el cual actúa como un enrutador (Router) para despachar los mensajes al destinatario correspondiente o a los múltiples miembros de un grupo.

La comunicación se basa en el intercambio de **Tramas de 1024 bytes** a través de TCP, garantizando un protocolo estructurado donde los metadatos viajan de forma determinista.

---

## 2. Componentes del Servidor (Server-Side)

### `ServidorBackend.cs`
Es el núcleo de la aplicación servidor. Orquesta todas las conexiones entrantes y el ciclo de vida de los clientes.
- **Descubrimiento UDP (`UdpBroadcaster`):** Ejecuta un hilo en segundo plano que grita constantemente (Broadcast UDP por el puerto 8000) su propia IP. Esto permite que los clientes en la red privada (ej. `192.168.7.x`) encuentren el servidor de manera automática.
- **Enrutamiento (`EnrutarTramaAsync`):** Recibe tramas de 1024 bytes. Analiza el destinatario y lo despacha. Si es un grupo, reescribe el remitente a `GRUPO:id:remitente` y lo reenvía a los miembros.

### `ManejadorCliente.cs`
Representa la conexión física de un cliente individual con el servidor.

**Snippet Clave: Lectura Asíncrona Ininterrumpida**
```csharp
private async Task EscucharClienteAsync()
{
    try
    {
        while (true)
        {
            // Leemos el tamaño de la cadena del destinatario (1 byte)
            byte[] lenBuf = new byte[1];
            int read = await _stream.ReadAsync(lenBuf, 0, 1);
            if (read == 0) break; 
            
            // Leemos exactamente 1024 bytes (Trama fija) sin bloquear otros clientes
            byte[] trama = new byte[1024];
            await _stream.ReadExactlyAsync(trama, 0, 1024);

            await _servidor.EnrutarTramaAsync(NombreUsuario, destinatario, trama);
        }
    }
    catch { /* Desconexión abrupta */ }
}
```
*Por qué se usa:* Utilizar `ReadExactlyAsync` dentro de un bucle permite al servidor atender a cientos de clientes al mismo tiempo usando hilos del *Thread Pool* de .NET, logrando una verdadera concurrencia asíncrona.

---

## 3. Componentes del Cliente (Client-Side)

El cliente utiliza el patrón **Fachada (Facade)** y el **Principio de Responsabilidad Única (SRP)** para evitar tener un código "espagueti".

### `CapaTransporte.cs`
Es la clase que interactúa directamente con la tarjeta de red del cliente. No sabe nada sobre chats o archivos; solo ve "bytes".

**Snippet Clave: Productor-Consumidor (Backpressure)**
```csharp
private readonly SemaphoreSlim _semLimiteColaBaja = new SemaphoreSlim(100, 100);

public void EncolarBajaPrioridad(byte[] trama, string destinatario)
{
    // Si hay más de 100 fragmentos de archivo en cola, el hilo se pausa
    _semLimiteColaBaja.Wait(); 
    lock (_lockColas) 
    { 
        _colaBajaPrioridad.Enqueue(new TramaEnrutada { Trama1024 = trama, Destinatario = destinatario }); 
    }
    _semColas.Release(); // Avisa al bucle despachador que hay trabajo
}
```
*Por qué se usa:* Cuando el usuario envía un archivo pesado, el disco duro lee más rápido de lo que la tarjeta de red puede enviar. El semáforo frena al lector del disco para no desbordar la memoria RAM, manteniendo la aplicación fluida.

### `ParserTramas.cs`
Es el "Recepcionista" o Enrutador interno del cliente. Toma la trama de 1024 bytes y lee el **primer caracter** (el byte `[0]`).
```csharp
public void ProcesarTrama(byte[] trama, string remitente)
{
    string tipo = Encoding.UTF8.GetString(trama, 0, 1);
    switch (tipo)
    {
        case "M": TramaMensajeRecibida?.Invoke(trama, remitente); break;
        case "F": 
        case "A": TramaArchivoRecibida?.Invoke(trama, remitente); break;
        case "G": GrupoRecibido?.Invoke(idGrupo, nombre, creador, miembros); break;
    }
}
```

### `GestorMensajes.cs`
Maneja la lógica de fragmentación y reensamblaje exclusivo de textos grandes. Usa un `StringBuilder` para armar los fragmentos entrantes, disparando un evento a la UI solo cuando termina.

### `ClassTransferenciaArchivo.cs` (GestorTransferencias)
Es el motor de transferencia de archivos binarios (Imágenes, PDF, RAR).

**Snippet Clave: Envío Limpio y Seguro (Prevención de Corrupción)**
```csharp
while (avanceEnvio < TamañoArchivo)
{
    long restante = TamañoArchivo - avanceEnvio;
    int aLeer = restante >= 1019 ? 1019 : (int)restante;

    // Acumulamos exactamente los bytes necesarios para este chunk
    int bytesLeidosTotales = 0;
    while (bytesLeidosTotales < aLeer)
    {
        int leido = leyendoTramaArchivoEnvio.Read(tramaEnvioArchivo, 5 + bytesLeidosTotales, aLeer - bytesLeidosTotales);
        if (leido == 0) break; // EOF inesperado
        bytesLeidosTotales += leido;
    }
    
    avanceEnvio += bytesLeidosTotales;
    EnviarTrama(cabA, bytesLeidosTotales); // Crea un clon del byte[] y lo manda a la red
}
```
*Por qué se usa:* Garantiza que los archivos enviados conserven su integridad. Obliga al hilo a leer exactamente el tamaño del *chunk* y lo envía encapsulado, evitando condiciones de carrera donde la memoria se sobrescriba (lo cual corrompería archivos binarios como RAR o JPG).

### `GestorGrupos.cs` (Cliente y Servidor)
Es la estructura de datos compartida conceptualmente, usada para agrupar lógicamente a los clientes.
- **Lógica principal:** Mantiene un diccionario de ID de grupos contra instancias de `GrupoInfo` (Nombre, Creador, Miembros). 
- *Por qué se usa:* Abstrae la necesidad de que el Servidor o el Cliente sepan de memoria las relaciones de una base de datos (inexistente aquí), manteniéndolas vivas en memoria RAM y sincronizándolas vía las tramas de tipo `'G'`.

### `classComunicacion.cs`
Es el orquestador principal o **Fachada (Facade Pattern)**. Su propósito es "esconder" la complejidad de los componentes anteriores frente a los Formularios visuales.

**Snippet Clave: Delegación de Eventos**
```csharp
// Unimos las tramas parseadas de la red con la lógica de negocio
_parser.TramaArchivoRecibida += (trama, remitente) => GestorArchivos.ProcesarTrama(trama, remitente);
_parser.TramaMensajeRecibida += (trama, remitente) => GestorChat.ProcesarTramaMensaje(trama, remitente);

// Exponemos un evento limpio para que la UI lo dibuje
GestorChat.LlegoMensaje += (remitente, msg) => LlegoMensaje?.Invoke(remitente, msg);
```
*Por qué se usa:* Esto es la representación perfecta del patrón Fachada. El formulario UI (ClientForm) solo se entera cuando `"LlegoMensaje"` ocurre, ignorando por completo que detrás hubo TCP, Fragmentación en pedazos de 1024, Parcheo, y Colas concurrentes.

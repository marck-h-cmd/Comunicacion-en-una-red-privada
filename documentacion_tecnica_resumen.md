# Hoja Resumen de Arquitectura y Lógica de Red

Este documento presenta una síntesis rápida de la arquitectura del proyecto y cómo interactúan las distintas clases sin entrar en código profundo. Ideal para tener una visión macro del sistema.

---

## 1. Topología y Red
- **Modelo:** Cliente-Servidor (Estrella). Los clientes no se ven directamente entre sí; todo pasa por el servidor.
- **Protocolo:** Basado en Sockets TCP asíncronos y Sockets UDP (para descubrimiento).
- **Transporte:** Toda la información (textos, archivos, listas) viaja dividida estrictamente en **tramas de 1024 bytes**.

## 2. El Servidor (Lado Backend)
Su función exclusiva es gestionar conexiones y enrutar. No guarda historiales en base de datos.
- **`ServidorBackend.cs`:** El cerebro. Anuncia su existencia por UDP, acepta conexiones TCP y guarda la lista de clientes activos. Decide a quién mandar cada mensaje (Enrutador).
- **`ManejadorCliente.cs`:** Representa el cable físico conectado a cada usuario. Tiene un bucle infinito que escucha pasivamente. Al atrapar un paquete, se lo pasa de inmediato al `ServidorBackend` para que lo distribuya.

## 3. El Cliente (Lado Frontend/Negocio)
Para mantener un código limpio (desacoplado), la lógica del cliente está dividida en capas específicas.

- **`classComunicacion.cs` (La Fachada):** Es el orquestador principal. Los formularios de interfaz solo hablan con esta clase. Ella se encarga de crear y conectar internamente al resto de componentes.
- **`CapaTransporte.cs` (La Tarjeta de Red):** La capa más baja. Maneja las colas Productor-Consumidor para no congelar la red y recibe/envía bytes crudos. No le importa si está enviando una foto o un "Hola".
- **`ParserTramas.cs` (El Recepcionista):** Analiza el primer byte de cada paquete que llega de la red. Según la letra ('M' para mensajes, 'F' para archivos, 'L' para listas), clasifica el paquete y lo manda al gestor adecuado.
- **`GestorMensajes.cs`:** Toma textos gigantes, los pica en pedazos de 1019 bytes para la red, y al revés, junta los pedazos entrantes hasta formar el mensaje original para la pantalla.
- **`ClassTransferenciaArchivo.cs`:** Maneja operaciones pesadas con discos. Lee archivos locales por trozos y los manda por la red asegurando la integridad de cada byte. Puede pausar, reanudar o cancelar descargas sin bloquear el chat.
- **`GestorGrupos.cs`:** Simple base de datos en RAM que recuerda qué usuarios pertenecen a qué ID de grupo, facilitando el enrutamiento múltiple.

---

### Resumen de Patrones de Software Clave
- **Single Responsibility (SRP):** Un archivo procesa sockets, otro procesa UI, otro procesa textos. Ninguno hace el trabajo del otro.
- **Facade Pattern:** Centralización en `classComunicacion`.
- **Event-Driven (Delegados):** Todo el sistema se comunica "hacia arriba" (de la red a la UI) mediante eventos, evitando congelamientos de pantalla (Multihilos seguros).

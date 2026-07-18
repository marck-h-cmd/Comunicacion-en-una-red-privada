//ClassTransferenciaArchivo.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace winProyComunicacion
{
    public enum EstadoTransferencia
    {
        Esperando,
        Enviando,
        Recibiendo,
        Completado,
        Error,
        Cancelado
    }

    public class GestorTransferencias
    {
        private ClaseTransferenciaArchivo[] listaTransferencias;
        private const int MAX_TRANSFERENCIAS = 5;
        private readonly Action<byte[], string> _escribirTrama;

        // Eventos delegados
        public event Action<string, int, string, long> ArchivoEntrante; // remitente, slot, nombre, tamaño
        public event Action<int, double> ProgresoArchivo;
        public event Action<int, string> TransferenciaCompletada;
        public event Action<int, string> ErrorTransferencia;
        public event Action<int> TransferenciaCancelada;

        public GestorTransferencias(Action<byte[], string> escribirTrama)
        {
            listaTransferencias = new ClaseTransferenciaArchivo[MAX_TRANSFERENCIAS];
            _escribirTrama = escribirTrama;
        }

        public void DetenerTodas()
        {
            for (int i = 0; i < MAX_TRANSFERENCIAS; i++)
            {
                if (listaTransferencias[i] != null)
                {
                    listaTransferencias[i].Cancelar();
                    listaTransferencias[i] = null;
                }
            }
        }

        public void ProcesarTrama(byte[] trama, string remitente)
        {
            string tipo = Encoding.UTF8.GetString(trama, 0, 1);
            switch (tipo)
            {
                case "F": ProcesarAnuncioArchivo(trama, remitente); break;
                case "A": ProcesarChunkArchivo(trama); break;
                case "C": ProcesarCancelacionArchivo(trama); break;
                case "R": ProcesarReanudacionArchivo(trama); break;
            }
        }

        private int ObtenerSlotLibre()
        {
            for (int i = 0; i < MAX_TRANSFERENCIAS; i++)
                if (listaTransferencias[i] == null
                 || listaTransferencias[i].Estado == EstadoTransferencia.Completado
                 || listaTransferencias[i].Estado == EstadoTransferencia.Error)
                    return i;
            return -1;
        }

        public void CancelarTransferencia(int slot)
        {
            if (slot >= 0 && slot < MAX_TRANSFERENCIAS && listaTransferencias[slot] != null)
            {
                listaTransferencias[slot].Cancelar();
            }
        }

        public int EnviarArchivo(string rutaCompleta, string destinatario)
        {
            int slot = ObtenerSlotLibre();
            if (slot < 0)
            {
                MessageBox.Show("Solo se permiten " + MAX_TRANSFERENCIAS +
                                " transferencias simultáneas.\nEspera a que termine alguna.",
                                "Límite alcanzado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return -1;
            }
            try
            {
                var tf = new ClaseTransferenciaArchivo(slot, _escribirTrama);
                tf.Destinatario = destinatario; // Importante para enviar al destinatario
                tf.ProgresoArchivo += (s, p) => ProgresoArchivo?.Invoke(s, p);
                tf.TransferenciaCompletada += (s, n) => TransferenciaCompletada?.Invoke(s, n);
                tf.ErrorTransferencia += (s, d) => ErrorTransferencia?.Invoke(s, d);
                tf.TransferenciaCancelada += (s) => TransferenciaCancelada?.Invoke(s);
                tf.AbrirArchivo(rutaCompleta);
                tf.EsEnvio = true;
                listaTransferencias[slot] = tf;
                tf.InicioTransmicionArchivo();
                return slot;
            }
            catch { return -1; }
        }

        private void ProcesarAnuncioArchivo(byte[] trama, string remitente)
        {
            try
            {
                int slot = int.Parse(Encoding.UTF8.GetString(trama, 1, 1));
                string payload = Encoding.UTF8.GetString(trama, 5, 1019).TrimEnd((char)64);
                int sep = payload.IndexOf('|');
                if (sep < 0) return;

                string nombre = payload.Substring(0, sep);
                long tamaño = long.Parse(payload.Substring(sep + 1));

                var tf = new ClaseTransferenciaArchivo(slot, _escribirTrama)
                {
                    Destinatario = remitente, // Para poder responder (ej. cancelación) va al remitente
                    NombreArchivo = nombre,
                    TamañoArchivoRecepcion = tamaño,
                    Estado = EstadoTransferencia.Recibiendo,
                    EsEnvio = false
                };
                tf.ProgresoArchivo += (s, p) => ProgresoArchivo?.Invoke(s, p);
                tf.TransferenciaCompletada += (s, n) => TransferenciaCompletada?.Invoke(s, n);
                tf.ErrorTransferencia += (s, d) => ErrorTransferencia?.Invoke(s, d);
                tf.TransferenciaCancelada += (s) => TransferenciaCancelada?.Invoke(s);

                listaTransferencias[slot] = tf;
                ArchivoEntrante?.Invoke(remitente, slot, nombre, tamaño);
            }
            catch (Exception ex) { MessageBox.Show("Error en anuncio de archivo: " + ex.Message); }
        }

        public void AceptarArchivoEntrante(int slot, string rutaDestino)
        {
            var tf = listaTransferencias[slot];
            if (tf == null) return;
            tf.CrearArchivoARecibir(rutaDestino, tf.TamañoArchivoRecepcion);
        }

        private void ProcesarChunkArchivo(byte[] trama)
        {
            try
            {
                int slot = int.Parse(Encoding.UTF8.GetString(trama, 1, 1));
                var tf = listaTransferencias[slot];
                if (tf == null || tf.Estado == EstadoTransferencia.Completado) return;
                tf.EscribiendoRecepcionArchivo(trama);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RX] Error al procesar chunk: {ex.Message}");
            }
        }

        private void ProcesarCancelacionArchivo(byte[] trama)
        {
            try
            {
                int slot = int.Parse(Encoding.UTF8.GetString(trama, 1, 1));
                var tf = listaTransferencias[slot];
                if (tf != null)
                {
                    tf.CancelarDesdeRemoto();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RX] Error al procesar cancelación: {ex.Message}");
            }
        }

        private void ProcesarReanudacionArchivo(byte[] trama)
        {
            try
            {
                int slot = int.Parse(Encoding.UTF8.GetString(trama, 1, 1));
                string payload = Encoding.UTF8.GetString(trama, 5, 1019).TrimEnd((char)64);
                int sep = payload.IndexOf('|');
                if (sep < 0) return;

                long offset = long.Parse(payload.Substring(sep + 1));

                var tf = listaTransferencias[slot];
                if (tf != null)
                {
                    if (tf.EsEnvio)
                    {
                        tf.BytesTransferidos = offset;
                        tf.Reanudar(false); // Resume sending
                    }
                    else
                    {
                        tf.ReanudarRecepcion(offset);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RX] Error en reanudación de archivo: {ex.Message}");
            }
        }

        public bool ReanudarTransferencia(int slot)
        {
            if (slot >= 0 && slot < MAX_TRANSFERENCIAS && listaTransferencias[slot] != null)
            {
                var tf = listaTransferencias[slot];
                if (tf.Estado == EstadoTransferencia.Cancelado)
                {
                    tf.Reanudar(true);
                    return true;
                }
            }
            return false;
        }

        public bool ReanudarTransferenciaDesdeReceptor(int slot)
        {
            if (slot >= 0 && slot < MAX_TRANSFERENCIAS && listaTransferencias[slot] != null)
            {
                var tf = listaTransferencias[slot];
                if (tf.Estado == EstadoTransferencia.Cancelado)
                {
                    tf.ReanudarRecepcionLocal();
                    return true;
                }
            }
            return false;
        }
    }

    public class ClaseTransferenciaArchivo
    {
        // ── Metadatos del archivo ──────────────────────────────────────────
        public int Indice { get; private set; }
        public string RutaCompleta { get; private set; }
        public string NombreArchivo { get; set; }
        public string Destinatario { get; set; }
        public long TamañoArchivo { get; private set; }
        public long TamañoArchivoRecepcion { get; set; }
        public bool EsEnvio { get; set; }

        // ── Estado ─────────────────────────────────────────────────────────
        public EstadoTransferencia Estado { get; set; }
        private volatile bool _cancelado = false;

        // ── Progreso ───────────────────────────────────────────────────────
        public long BytesTransferidos { get; set; }
        public double Progreso => (TamañoArchivo > 0) ? ((double)BytesTransferidos / TamañoArchivo) * 100.0 :
                                 (TamañoArchivoRecepcion > 0 ? ((double)BytesTransferidos / TamañoArchivoRecepcion) * 100.0 : 0);

        // ── Flujos y hebras ────────────────────────────────────────────────
        public FileStream FlujoLecturaArchivo { get; private set; }
        private BinaryReader leyendoTramaArchivoEnvio;
        private Thread ProcesoEnvioArchivo;
        private byte[] tramaEnvioArchivo;

        public FileStream FlujoEscrituraArchivo { get; private set; }
        private BinaryWriter EscribiendoTramaArchivoRecepcion;
        private readonly object _lockRecepcion = new object();

        // ── Control de aceptación ──────────────────────────────────────────
        public bool Aceptado { get; set; }
        public Queue<byte[]> ChunksEnEspera { get; private set; }

        // ── Delegado de escritura ──────────────────────────────────────────
        private readonly Action<byte[], string> _escribirTrama;

        // ── Eventos ────────────────────────────────────────────────────────
        public event Action<int, double> ProgresoArchivo;
        public event Action<int, string> TransferenciaCompletada;
        public event Action<int, string> ErrorTransferencia;
        public event Action<int> TransferenciaCancelada;

        private const int DELAY_ENTRE_CHUNKS_MS = 0;

        public ClaseTransferenciaArchivo(int indice, Action<byte[], string> escribirTrama)
        {
            Indice = indice;
            Estado = EstadoTransferencia.Esperando;
            BytesTransferidos = 0;
            Aceptado = false;
            ChunksEnEspera = new Queue<byte[]>();
            tramaEnvioArchivo = new byte[1024];
            _escribirTrama = escribirTrama;
        }

        private void EnviarTrama(byte[] cabecera, int datosLength)
        {
            // tramaEnvioArchivo already has the data in [5..5+datosLength]
            // Fill the rest with '@'
            for (int i = 5 + datosLength; i < 1024; i++)
            {
                tramaEnvioArchivo[i] = 64;
            }
            // Copy header to tramaEnvioArchivo
            Array.Copy(cabecera, 0, tramaEnvioArchivo, 0, cabecera.Length);
            _escribirTrama(tramaEnvioArchivo, Destinatario);
        }

        // ════════════════════════════════════════════════════════════════════
        //  CANCELACIÓN
        // ════════════════════════════════════════════════════════════════════

        public void Cancelar()
        {
            if (Estado == EstadoTransferencia.Completado || Estado == EstadoTransferencia.Cancelado) return;

            try
            {
                _cancelado = true;
                Estado = EstadoTransferencia.Cancelado;

                // Enviar trama de cancelación al receptor/emisor
                byte[] cabC = Encoding.UTF8.GetBytes("C" + Indice + "000");
                EnviarTrama(cabC, 0);

                CerrarFlujos();
                TransferenciaCancelada?.Invoke(Indice);
            }
            catch { }
        }

        public void CancelarDesdeRemoto()
        {
            if (Estado == EstadoTransferencia.Completado || Estado == EstadoTransferencia.Cancelado) return;

            _cancelado = true;
            Estado = EstadoTransferencia.Cancelado;
            CerrarFlujos();
            TransferenciaCancelada?.Invoke(Indice);
        }

        // ════════════════════════════════════════════════════════════════════
        //  ENVÍO DE ARCHIVOS
        // ════════════════════════════════════════════════════════════════════

        public void AbrirArchivo(string ruta)
        {
            RutaCompleta = ruta;
            NombreArchivo = Path.GetFileName(ruta);
            FlujoLecturaArchivo = new FileStream(ruta, FileMode.Open, FileAccess.Read);
            leyendoTramaArchivoEnvio = new BinaryReader(FlujoLecturaArchivo);
            TamañoArchivo = FlujoLecturaArchivo.Length;
        }

        public void InicioTransmicionArchivo()
        {
            Estado = EstadoTransferencia.Enviando;
            _cancelado = false;
            ProcesoEnvioArchivo = new Thread(LeyendoTransmitiendoArchivo) { IsBackground = true };
            ProcesoEnvioArchivo.Start();
        }

        private void LeyendoTransmitiendoArchivo()
        {
            try
            {
                // 1. Anuncio del archivo: F(slot)000[Nombre]|[Tamaño]
                string anuncio = $"{NombreArchivo}|{TamañoArchivo}";
                byte[] anuncioBytes = Encoding.UTF8.GetBytes(anuncio);
                byte[] cabF = Encoding.UTF8.GetBytes("F" + Indice + "000");
                Array.Copy(anuncioBytes, 0, tramaEnvioArchivo, 5, anuncioBytes.Length);
                EnviarTrama(cabF, anuncioBytes.Length);

                if (_cancelado) return;

                // 2. Chunks del archivo
                long avanceEnvio = 0;
                byte[] cabA = Encoding.UTF8.GetBytes("A" + Indice + "000");

                while (TamañoArchivo - avanceEnvio >= 1019)
                {
                    if (_cancelado) break;

                    leyendoTramaArchivoEnvio.Read(tramaEnvioArchivo, 5, 1019);
                    avanceEnvio += 1019;
                    BytesTransferidos = avanceEnvio;

                    EnviarTrama(cabA, 1019);
                    ProgresoArchivo?.Invoke(Indice, Progreso);
                    Thread.Sleep(DELAY_ENTRE_CHUNKS_MS);
                }

                if (_cancelado)
                {
                    Estado = EstadoTransferencia.Cancelado;
                    return;
                }

                // 3. Último trozo
                int ultTam = (int)(TamañoArchivo - avanceEnvio);
                if (ultTam > 0)
                    leyendoTramaArchivoEnvio.Read(tramaEnvioArchivo, 5, ultTam);

                byte[] cabUlt = Encoding.UTF8.GetBytes("A" + Indice + "000");
                EnviarTrama(cabUlt, ultTam);

                BytesTransferidos += ultTam;
                Estado = EstadoTransferencia.Completado;
                TransferenciaCompletada?.Invoke(Indice, NombreArchivo);
            }
            catch (Exception ex)
            {
                Estado = EstadoTransferencia.Error;
                ErrorTransferencia?.Invoke(Indice, ex.Message);
            }
            finally
            {
                CerrarFlujos();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  REANUDACIÓN
        // ════════════════════════════════════════════════════════════════════

        public void Reanudar(bool notificarReceptor)
        {
            if (EsEnvio)
            {
                try
                {
                    if (FlujoLecturaArchivo == null || !FlujoLecturaArchivo.CanRead)
                    {
                        FlujoLecturaArchivo = new FileStream(RutaCompleta, FileMode.Open, FileAccess.Read);
                        leyendoTramaArchivoEnvio = new BinaryReader(FlujoLecturaArchivo);
                    }
                    
                    FlujoLecturaArchivo.Seek(BytesTransferidos, SeekOrigin.Begin);
                    Estado = EstadoTransferencia.Enviando;
                    _cancelado = false;

                    if (notificarReceptor)
                    {
                        // Enviar trama R para que el receptor retome la espera
                        string reanudacion = $"{NombreArchivo}|{BytesTransferidos}";
                        byte[] reanudacionBytes = Encoding.UTF8.GetBytes(reanudacion);
                        byte[] cabR = Encoding.UTF8.GetBytes("R" + Indice + "000");
                        Array.Copy(reanudacionBytes, 0, tramaEnvioArchivo, 5, reanudacionBytes.Length);
                        EnviarTrama(cabR, reanudacionBytes.Length);
                    }

                    ProcesoEnvioArchivo = new Thread(LeyendoTransmitiendoArchivoReanudado) { IsBackground = true };
                    ProcesoEnvioArchivo.Start();
                }
                catch (Exception ex)
                {
                    Estado = EstadoTransferencia.Error;
                    ErrorTransferencia?.Invoke(Indice, ex.Message);
                    CerrarFlujos();
                }
            }
        }

        private void LeyendoTransmitiendoArchivoReanudado()
        {
            try
            {
                long avanceEnvio = BytesTransferidos;
                byte[] cabA = Encoding.UTF8.GetBytes("A" + Indice + "000");

                while (TamañoArchivo - avanceEnvio >= 1019)
                {
                    if (_cancelado) break;

                    leyendoTramaArchivoEnvio.Read(tramaEnvioArchivo, 5, 1019);
                    avanceEnvio += 1019;
                    BytesTransferidos = avanceEnvio;

                    EnviarTrama(cabA, 1019);
                    ProgresoArchivo?.Invoke(Indice, Progreso);
                    Thread.Sleep(DELAY_ENTRE_CHUNKS_MS);
                }

                if (_cancelado)
                {
                    Estado = EstadoTransferencia.Cancelado;
                    return;
                }

                int ultTam = (int)(TamañoArchivo - avanceEnvio);
                if (ultTam > 0)
                    leyendoTramaArchivoEnvio.Read(tramaEnvioArchivo, 5, ultTam);

                byte[] cabUlt = Encoding.UTF8.GetBytes("A" + Indice + "000");
                EnviarTrama(cabUlt, ultTam);

                BytesTransferidos += ultTam;
                Estado = EstadoTransferencia.Completado;
                TransferenciaCompletada?.Invoke(Indice, NombreArchivo);
            }
            catch (Exception ex)
            {
                Estado = EstadoTransferencia.Error;
                ErrorTransferencia?.Invoke(Indice, ex.Message);
            }
            finally
            {
                CerrarFlujos();
            }
        }

        public void ReanudarRecepcionLocal()
        {
            if (!EsEnvio)
            {
                // Enviar trama R para que el emisor retome el envío
                string reanudacion = $"{NombreArchivo}|{BytesTransferidos}";
                byte[] reanudacionBytes = Encoding.UTF8.GetBytes(reanudacion);
                byte[] cabR = Encoding.UTF8.GetBytes("R" + Indice + "000");
                Array.Copy(reanudacionBytes, 0, tramaEnvioArchivo, 5, reanudacionBytes.Length);
                EnviarTrama(cabR, reanudacionBytes.Length);

                ReanudarRecepcion(BytesTransferidos);
            }
        }

        public void ReanudarRecepcion(long offset)
        {
            try
            {
                if (FlujoEscrituraArchivo == null || !FlujoEscrituraArchivo.CanWrite)
                {
                    FlujoEscrituraArchivo = new FileStream(RutaCompleta, FileMode.OpenOrCreate, FileAccess.Write);
                    EscribiendoTramaArchivoRecepcion = new BinaryWriter(FlujoEscrituraArchivo);
                }
                FlujoEscrituraArchivo.Seek(offset, SeekOrigin.Begin);
                BytesTransferidos = offset;
                Estado = EstadoTransferencia.Recibiendo;
                _cancelado = false;
            }
            catch (Exception ex)
            {
                Estado = EstadoTransferencia.Error;
                ErrorTransferencia?.Invoke(Indice, ex.Message);
                CerrarFlujos();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  RECEPCIÓN DE ARCHIVOS
        // ════════════════════════════════════════════════════════════════════

        public void CrearArchivoARecibir(string rutaDestino, long tamaño)
        {
            lock (_lockRecepcion)
            {
                try
                {
                    RutaCompleta = rutaDestino;
                    TamañoArchivoRecepcion = tamaño;
                    FlujoEscrituraArchivo = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write);
                    EscribiendoTramaArchivoRecepcion = new BinaryWriter(FlujoEscrituraArchivo);
                    Aceptado = true;
                    _cancelado = false;
                    Estado = EstadoTransferencia.Recibiendo;

                    while (ChunksEnEspera.Count > 0 && !_cancelado)
                    {
                        byte[] tramaGuardada = ChunksEnEspera.Dequeue();
                        EscribirChunk(tramaGuardada);
                    }
                }
                catch (Exception ex)
                {
                    Estado = EstadoTransferencia.Error;
                    ErrorTransferencia?.Invoke(Indice, ex.Message);
                    CerrarFlujos();
                }
            }
        }

        public void EscribiendoRecepcionArchivo(byte[] trama)
        {
            lock (_lockRecepcion)
            {
                if (_cancelado) return;

                if (!Aceptado)
                {
                    byte[] copia = new byte[trama.Length];
                    Array.Copy(trama, copia, trama.Length);
                    ChunksEnEspera.Enqueue(copia);
                    return;
                }

                EscribirChunk(trama);
            }
        }

        private void EscribirChunk(byte[] trama)
        {
            try
            {
                long restante = TamañoArchivoRecepcion - BytesTransferidos;
                int bytesAEscribir = restante >= 1019 ? 1019 : (int)restante;

                EscribiendoTramaArchivoRecepcion.Write(trama, 5, bytesAEscribir);
                BytesTransferidos += bytesAEscribir;

                ProgresoArchivo?.Invoke(Indice, Progreso);

                if (BytesTransferidos >= TamañoArchivoRecepcion)
                {
                    Estado = EstadoTransferencia.Completado;
                    CerrarFlujos();
                    TransferenciaCompletada?.Invoke(Indice, NombreArchivo);
                }
            }
            catch (Exception ex)
            {
                Estado = EstadoTransferencia.Error;
                ErrorTransferencia?.Invoke(Indice, ex.Message);
                CerrarFlujos();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  UTILIDADES
        // ════════════════════════════════════════════════════════════════════

        private void CerrarFlujos()
        {
            try { EscribiendoTramaArchivoRecepcion?.Close(); } catch { }
            try { FlujoEscrituraArchivo?.Close(); } catch { }
            try { leyendoTramaArchivoEnvio?.Close(); } catch { }
            try { FlujoLecturaArchivo?.Close(); } catch { }
        }
    }
}
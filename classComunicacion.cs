// ClassComunicacion.cs
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Windows.Forms;

namespace winProyComunicacion
{
    public class TramaEnrutada
    {
        public byte[] Trama1024 { get; set; }
        public string Destinatario { get; set; }
    }

    internal class ClassComunicacion
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public event Action<string, string> LlegoMensaje;
        public event Action<string>? ServidorEncontrado; // New event: server IP found

        public event Action<string> NombreUsuarioRecibido;
        // Evento para cuando el servidor manda la lista de usuarios (trama 'L')
        public event Action<List<ContactoItem>> ListaContactosActualizada;
        public event Action<string, string, string, List<string>>? GrupoRecibido;

        public GestorTransferencias GestorArchivos { get; private set; }

        private string MensajeRecibido;
        private StringBuilder mensajeRecibidoCompleto;
        private UdpClient? _udpListener;
        private CancellationTokenSource? _ctsListener;

        // ════════════════════════════════════════════════════════════════════
        //  RECEPCIÓN
        // ════════════════════════════════════════════════════════════════════
        private Thread _hiloRx;
        private volatile bool _recibiendoActivo = false;

        // ════════════════════════════════════════════════════════════════════
        //  ENVÍO — dos colas con prioridad
        // ════════════════════════════════════════════════════════════════════
        private readonly Queue<TramaEnrutada> _colaMensajes = new Queue<TramaEnrutada>();
        private readonly Queue<TramaEnrutada> _colaBajaPrioridad = new Queue<TramaEnrutada>();
        private readonly object _lockColas = new object();
        private readonly SemaphoreSlim _semColas = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _semLimiteColaBaja = new SemaphoreSlim(100, 100); // Máximo 100 frames en cola baja
        private Thread _hiloDespacho;
        private volatile bool _despachando = false;

        private const int DELAY_DESPACHO_MS = 0;

        public ClassComunicacion()
        {
            MensajeRecibido = "";
            mensajeRecibidoCompleto = new StringBuilder();
            GestorArchivos = new GestorTransferencias(EncolarBajaPrioridad);
        }

        // ════════════════════════════════════════════════════════════════════
        //  CONEXIÓN TCP
        // ════════════════════════════════════════════════════════════════════

        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        public async Task<bool> ConectarServidorAsync(string ip, int puerto)
        {
            CerrarConexion();

            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ip, puerto);
                _stream = _tcpClient.GetStream();

                lock (_lockColas) { _colaMensajes.Clear(); _colaBajaPrioridad.Clear(); }
                mensajeRecibidoCompleto.Clear();

                IniciarHiloRx();
                IniciarDespachador();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error conectando al servidor: {ex.Message}");
                return false;
            }
        }

        public string GetLocalEndPoint()
        {
            if (_tcpClient?.Client?.LocalEndPoint is System.Net.IPEndPoint endPoint)
            {
                return endPoint.Port.ToString();
            }
            return "";
        }

        public void StartDiscovery()
        {
            if (_udpListener != null) return;

            _ctsListener = new CancellationTokenSource();
            _udpListener = new UdpClient(8001);
            _udpListener.EnableBroadcast = true;

            _ = Task.Run(async () =>
            {
                while (!_ctsListener.Token.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult result = await _udpListener.ReceiveAsync();
                        string msg = System.Text.Encoding.UTF8.GetString(result.Buffer);
                        if (msg.StartsWith("TATO_SERVER:"))
                        {
                            string[] parts = msg.Split(':');
                            if (parts.Length >= 3)
                            {
                                string serverIp = parts[1];
                                ServidorEncontrado?.Invoke(serverIp);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }
            }, _ctsListener.Token);
        }

        public void StopDiscovery()
        {
            _ctsListener?.Cancel();
            _udpListener?.Close();
            _udpListener = null;
        }

        public void CerrarConexion()
        {
            StopDiscovery();
            DetenerDespachador();
            DetenerHiloRx();
            GestorArchivos?.DetenerTodas();
            try { _stream?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
        }

        // ════════════════════════════════════════════════════════════════════
        //  RECEPCIÓN
        // ════════════════════════════════════════════════════════════════════

        private void IniciarHiloRx()
        {
            _recibiendoActivo = true;
            _hiloRx = new Thread(BucleRx) { IsBackground = true };
            _hiloRx.Start();
        }

        private void DetenerHiloRx()
        {
            _recibiendoActivo = false;
        }

        private async void BucleRx()
        {
            try
            {
                while (_recibiendoActivo && _stream != null)
                {
                    // Leer encabezado: [LenRemitente]
                    int lenRemitente = _stream.ReadByte();
                    if (lenRemitente == -1) break;

                    // Leer [Remitente]
                    byte[] remBytes = new byte[lenRemitente];
                    await _stream.ReadExactlyAsync(remBytes, 0, lenRemitente);
                    string remitente = Encoding.UTF8.GetString(remBytes);

                    // Leer [Trama1024]
                    byte[] trama = new byte[1024];
                    await _stream.ReadExactlyAsync(trama, 0, 1024);

                    ProcesarTramaRx(trama, remitente);
                }
            }
            catch
            {
                // Conexión terminada o error de red
            }
            finally
            {
                CerrarConexion();
            }
        }

        private void ProcesarTramaRx(byte[] trama, string remitente)
        {
            string tipo = ASCIIEncoding.UTF8.GetString(trama, 0, 1);
            switch (tipo)
            {
                case "M":
                    RecibiendoMensaje(trama, remitente);
                    break;

                case "F":
                case "A":
                case "C":
                case "R":
                    GestorArchivos.ProcesarTrama(trama, remitente);
                    break;

                case "N":
                    ProcesarNombreUsuario(trama);
                    break;

                case "L":
                    ProcesarListaContactos(trama);
                    break;

                case "G":
                    ProcesarTramaGrupo(trama);
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"[RX] Trama no reconocida: byte[0]=0x{trama[0]:X2} ('{tipo}')");
                    break;
            }
        }

        private void ProcesarTramaGrupo(byte[] trama)
        {
            try
            {
                char subtipo = (char)trama[1];
                if (subtipo != '2') return; // el cliente solo recibe INFO, nunca CREAR
                int longitud = int.Parse(Encoding.UTF8.GetString(trama, 2, 4));
                string payload = Encoding.UTF8.GetString(trama, 6, longitud);
                var partes = payload.Split('|');
                string idGrupo = partes[0];
                string nombre = partes[1];
                string creador = partes[2];
                var miembros = partes[3].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                GrupoRecibido?.Invoke(idGrupo, nombre, creador, miembros);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RX] Error al procesar trama de grupo: {ex.Message}");
            }
        }

        private void ProcesarListaContactos(byte[] trama)
        {
            try
            {
                int longitud = int.Parse(ASCIIEncoding.UTF8.GetString(trama, 1, 4));
                string csv = ASCIIEncoding.UTF8.GetString(trama, 5, longitud);
                var crudos = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                var items = new List<ContactoItem>();
                foreach (var c in crudos)
                {
                    var partes = c.Split('|');
                    var nombre = partes[0];
                    items.Add(new ContactoItem { Nombre = nombre, MostrarComo = nombre });
                }
                
                ListaContactosActualizada?.Invoke(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RX] Error al procesar lista contactos: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  MENSAJES DE TEXTO
        // ════════════════════════════════════════════════════════════════════

        private void RecibiendoMensaje(byte[] trama, string remitente)
        {
            try
            {
                string longitudStr = ASCIIEncoding.UTF8.GetString(trama, 1, 4);
                if (!int.TryParse(longitudStr, out int longitud) || longitud < 0 || longitud > 1019) return;

                if (longitud > 0)
                    mensajeRecibidoCompleto.Append(ASCIIEncoding.UTF8.GetString(trama, 5, longitud));

                if (longitud < 1019)
                {
                    if (mensajeRecibidoCompleto.Length > 0)
                    {
                        MensajeRecibido = mensajeRecibidoCompleto.ToString();
                        mensajeRecibidoCompleto.Clear();
                        LlegoMensaje?.Invoke(remitente, MensajeRecibido);
                    }
                    else mensajeRecibidoCompleto.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RX] Error al procesar mensaje: {ex.Message}");
                mensajeRecibidoCompleto.Clear();
            }
        }

        public void enviarMensaje(string m, string destinatario)
        {
            byte[] msgBytes = ASCIIEncoding.UTF8.GetBytes(m);
            int maxTrozo = 1019, offset = 0;

            while (offset < msgBytes.Length)
            {
                int remaining = msgBytes.Length - offset;
                int longTrozo = (remaining >= maxTrozo) ? (remaining == maxTrozo ? maxTrozo - 1 : maxTrozo) : remaining;

                byte[] trama = new byte[1024];
                byte[] cab = ASCIIEncoding.UTF8.GetBytes("M" + longTrozo.ToString("D4"));
                Array.Copy(cab, 0, trama, 0, 5);
                Array.Copy(msgBytes, offset, trama, 5, longTrozo);
                for (int i = 5 + longTrozo; i < 1024; i++) trama[i] = 64; // '@'

                EnColarMensaje(trama, destinatario);
                offset += longTrozo;
            }
        }

        public bool CrearGrupo(string nombre, List<string> miembros)
        {
            string payload = $"{nombre}|{string.Join(",", miembros)}";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            if (payloadBytes.Length > 1018)
                return false;

            byte[] trama = new byte[1024];
            byte[] cab = Encoding.UTF8.GetBytes("G1" + payloadBytes.Length.ToString("D4"));
            Array.Copy(cab, 0, trama, 0, 6);
            Array.Copy(payloadBytes, 0, trama, 6, payloadBytes.Length);
            for (int i = 6 + payloadBytes.Length; i < 1024; i++) trama[i] = 64;
            EnColarMensaje(trama, "SERVER");
            return true;
        }

        // ════════════════════════════════════════════════════════════════════
        //  DESPACHADOR CON PRIORIDAD
        // ════════════════════════════════════════════════════════════════════

        private void IniciarDespachador()
        {
            _despachando = true;
            _hiloDespacho = new Thread(BucleDespachador) { IsBackground = true };
            _hiloDespacho.Start();
        }

        private void DetenerDespachador()
        {
            _despachando = false;
            _semColas.Release();
        }

        private void BucleDespachador()
        {
            while (_despachando)
            {
                _semColas.Wait();
                if (!_despachando) break;

                TramaEnrutada envio = null;
                bool esBajaPrioridad = false;
                lock (_lockColas)
                {
                    if (_colaMensajes.Count > 0) 
                    {
                        envio = _colaMensajes.Dequeue();
                        esBajaPrioridad = false;
                    }
                    else if (_colaBajaPrioridad.Count > 0) 
                    {
                        envio = _colaBajaPrioridad.Dequeue();
                        esBajaPrioridad = true;
                    }
                }

                if (esBajaPrioridad)
                {
                    _semLimiteColaBaja.Release(); // Liberar slot cuando decolamos frame de baja prioridad
                }
                if (envio == null) continue;

                try
                {
                    if (_stream != null && _tcpClient.Connected)
                    {
                        byte[] destBytes = Encoding.UTF8.GetBytes(envio.Destinatario);
                        byte[] buffer = new byte[1 + destBytes.Length + 1024];
                        buffer[0] = (byte)destBytes.Length;
                        Array.Copy(destBytes, 0, buffer, 1, destBytes.Length);
                        Array.Copy(envio.Trama1024, 0, buffer, 1 + destBytes.Length, 1024);
                        
                        _stream.Write(buffer, 0, buffer.Length);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al enviar trama a {envio.Destinatario}: {ex.Message}");
                }

                if (_despachando) Thread.Sleep(DELAY_DESPACHO_MS);
            }
        }

        private void EnColarMensaje(byte[] trama1024, string destinatario)
        {
            lock (_lockColas) { _colaMensajes.Enqueue(new TramaEnrutada { Trama1024 = trama1024, Destinatario = destinatario }); }
            _semColas.Release();
        }

        public void EncolarBajaPrioridad(byte[] trama, string destinatario)
        {
            _semLimiteColaBaja.Wait(); // Esperar hasta que haya espacio en la cola
            lock (_lockColas) { _colaBajaPrioridad.Enqueue(new TramaEnrutada { Trama1024 = trama, Destinatario = destinatario }); }
            _semColas.Release();
        }

        public void EnviarNombreUsuario(string nombre)
        {
            byte[] nameBytes = ASCIIEncoding.UTF8.GetBytes(nombre);
            int len = Math.Min(nameBytes.Length, 1019);

            byte[] trama = new byte[1024];
            byte[] cab = ASCIIEncoding.UTF8.GetBytes("N" + len.ToString("D4"));
            Array.Copy(cab, 0, trama, 0, 5);
            Array.Copy(nameBytes, 0, trama, 5, len);
            for (int i = 5 + len; i < 1024; i++) trama[i] = 64; // '@'

            // El registro de nombre va dirigido al servidor
            EnColarMensaje(trama, "SERVER");
        }

        private void ProcesarNombreUsuario(byte[] trama)
        {
            // Solo relevante si el cliente recibe confirmaciones, etc.
            try
            {
                int longitud = int.Parse(ASCIIEncoding.UTF8.GetString(trama, 1, 4));
                if (longitud > 0 && longitud <= 1019)
                {
                    string nombre = ASCIIEncoding.UTF8.GetString(trama, 5, longitud);
                    NombreUsuarioRecibido?.Invoke(nombre);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RX] Error al procesar nombre de usuario: {ex.Message}");
            }
        }
    }

    public class ContactoItem
    {
        public string Nombre { get; set; } = string.Empty;
        public string MostrarComo { get; set; } = string.Empty;
        public bool EsGrupo { get; set; } = false;
        public List<string>? Miembros { get; set; } = null;
        public int UnreadCount { get; set; } = 0; // Contador de mensajes no leídos
        public override string ToString() => MostrarComo;
    }
}
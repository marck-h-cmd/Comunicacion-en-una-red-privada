using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace winProyComunicacion
{
    public class CapaTransporte
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;

        public event Action<byte[], string> TramaRecibida;
        public event Action<string> ServidorEncontrado;

        private UdpClient _udpListener;
        private CancellationTokenSource _ctsListener;

        private Thread _hiloRx;
        private volatile bool _recibiendoActivo = false;

        private readonly Queue<TramaEnrutada> _colaMensajes = new Queue<TramaEnrutada>();
        private readonly Queue<TramaEnrutada> _colaBajaPrioridad = new Queue<TramaEnrutada>();
        private readonly object _lockColas = new object();
        private readonly SemaphoreSlim _semColas = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _semLimiteColaBaja = new SemaphoreSlim(100, 100);
        private Thread _hiloDespacho;
        private volatile bool _despachando = false;

        private const int DELAY_DESPACHO_MS = 0;

        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        public async Task<bool> ConectarAsync(string ip, int puerto)
        {
            CerrarConexion();

            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ip, puerto);
                _stream = _tcpClient.GetStream();

                lock (_lockColas) { _colaMensajes.Clear(); _colaBajaPrioridad.Clear(); }

                IniciarHiloRx();
                IniciarDespachador();
                return true;
            }
            catch (Exception)
            {
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
                        string msg = Encoding.UTF8.GetString(result.Buffer);
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
                    catch { }
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
            try { _stream?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
        }

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
                    int lenRemitente = _stream.ReadByte();
                    if (lenRemitente == -1) break;

                    byte[] remBytes = new byte[lenRemitente];
                    await _stream.ReadExactlyAsync(remBytes, 0, lenRemitente);
                    string remitente = Encoding.UTF8.GetString(remBytes);

                    byte[] trama = new byte[1024];
                    await _stream.ReadExactlyAsync(trama, 0, 1024);

                    TramaRecibida?.Invoke(trama, remitente);
                }
            }
            catch
            {
            }
            finally
            {
                CerrarConexion();
            }
        }

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
                    _semLimiteColaBaja.Release();
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

        public void EncolarMensaje(byte[] trama1024, string destinatario)
        {
            lock (_lockColas) { _colaMensajes.Enqueue(new TramaEnrutada { Trama1024 = trama1024, Destinatario = destinatario }); }
            _semColas.Release();
        }

        public void EncolarBajaPrioridad(byte[] trama, string destinatario)
        {
            _semLimiteColaBaja.Wait();
            lock (_lockColas) { _colaBajaPrioridad.Enqueue(new TramaEnrutada { Trama1024 = trama, Destinatario = destinatario }); }
            _semColas.Release();
        }
    }
}

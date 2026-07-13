//ManejadorCliente.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace winProyComunicacion
{
    public class ManejadorCliente
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly ServidorBackend _servidor;

        public string Alias { get; internal set; }
        public string EndpointId { get; private set; }

        public string NombreUsuario => Alias;
        public string EndPoint => EndpointId;

        public ManejadorCliente(TcpClient tcpClient, ServidorBackend servidor)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            _servidor = servidor;
            EndpointId = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Desconocido";
        }

        public async Task IniciarHandshakeAsync()
        {
            try
            {
                while (true)
                {
                    // El primer mensaje debe ser una trama de tipo 'N' (Nombre) con destinatario "SERVER"
                    // Formato de envoltura: [LenDestinatario (1 byte)][Destinatario][1024 bytes]
                    byte[] lenBuf = new byte[1];
                    int read = await _stream.ReadAsync(lenBuf, 0, 1);
                    if (read == 0) throw new IOException("Conexión cerrada.");
                    int lenDest = lenBuf[0];
                    
                    byte[] destBytes = new byte[lenDest];
                    await _stream.ReadExactlyAsync(destBytes, 0, lenDest);
                    string destinatario = Encoding.UTF8.GetString(destBytes);

                    byte[] trama = new byte[1024];
                    await _stream.ReadExactlyAsync(trama, 0, 1024);

                    if (trama[0] == 'N')
                    {
                        int longitud = int.Parse(Encoding.UTF8.GetString(trama, 1, 4));
                        string aliasCandidato = Encoding.UTF8.GetString(trama, 5, longitud);
                        
                        bool esValido = !string.IsNullOrEmpty(aliasCandidato)
                                        && !aliasCandidato.Contains(":")
                                        && !aliasCandidato.Contains(",")
                                        && !aliasCandidato.Contains("|")
                                        && !aliasCandidato.StartsWith("GRUPO:", StringComparison.OrdinalIgnoreCase);

                        bool registrado = false;
                        if (esValido)
                        {
                            registrado = _servidor.RegistrarCliente(this, aliasCandidato);
                        }

                        if (registrado)
                        {
                            // Iniciar bucle principal de escucha
                            _ = EscucharClienteAsync();
                            break;
                        }
                        else
                        {
                            // Enviar trama 'N' de rechazo indicando el error
                            byte[] remitenteBytes = Encoding.UTF8.GetBytes("SERVER");
                            byte[] tramaRechazo = new byte[1024];
                            tramaRechazo[0] = (byte)'N';
                            string errMsg = esValido ? "ERR_ALIAS_EN_USO" : "ERR_ALIAS_INVALIDO";
                            byte[] errPayload = Encoding.UTF8.GetBytes(errMsg);
                            byte[] lenBytes = Encoding.UTF8.GetBytes(errPayload.Length.ToString("D4"));
                            Array.Copy(lenBytes, 0, tramaRechazo, 1, 4);
                            Array.Copy(errPayload, 0, tramaRechazo, 5, errPayload.Length);
                            for (int i = 5 + errPayload.Length; i < 1024; i++) tramaRechazo[i] = 64;

                            await EnviarTramaAsync(remitenteBytes, tramaRechazo);
                        }
                    }
                    else
                    {
                        Desconectar();
                        break;
                    }
                }
            }
            catch
            {
                Desconectar();
            }
        }

        private async Task EscucharClienteAsync()
        {
            try
            {
                while (true)
                {
                    byte[] lenBuf = new byte[1];
                    int read = await _stream.ReadAsync(lenBuf, 0, 1);
                    if (read == 0) break; // Conexión cerrada
                    int lenDest = lenBuf[0];
                    
                    byte[] destBytes = new byte[lenDest];
                    await _stream.ReadExactlyAsync(destBytes, 0, lenDest);
                    string destinatario = Encoding.UTF8.GetString(destBytes);

                    byte[] trama = new byte[1024];
                    await _stream.ReadExactlyAsync(trama, 0, 1024);

                    await _servidor.EnrutarTramaAsync(NombreUsuario, destinatario, trama);
                }
            }
            catch
            {
                // SocketException o IOException significa que se desconectó
            }
            finally
            {
                _servidor.EliminarCliente(EndpointId, this);
                Desconectar();
            }
        }

        public async Task EnviarTramaAsync(byte[] remitenteBytes, byte[] trama1024)
        {
            try
            {
                byte[] buffer = new byte[1 + remitenteBytes.Length + 1024];
                buffer[0] = (byte)remitenteBytes.Length;
                Array.Copy(remitenteBytes, 0, buffer, 1, remitenteBytes.Length);
                Array.Copy(trama1024, 0, buffer, 1 + remitenteBytes.Length, 1024);

                await _stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                Desconectar();
            }
        }

        public void Desconectar()
        {
            try { _stream?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
        }
    }
}

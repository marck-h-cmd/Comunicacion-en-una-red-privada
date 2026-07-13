//ServidorBackend.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace winProyComunicacion
{
    public class ServidorBackend
    {
        private TcpListener _listener;
        private bool _ejecutando;
        private readonly ConcurrentDictionary<string, ManejadorCliente> _clientes = new ConcurrentDictionary<string, ManejadorCliente>();
        private readonly GestorGrupos _gestorGrupos = new GestorGrupos();

        public event Action<List<string>> ListaClientesActualizada;
        public event Action<string> LogEvent;

        public async Task IniciarServidorAsync(int puerto = 8000)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, puerto);
                _listener.Start();
                _ejecutando = true;
                LogEvent?.Invoke($"Servidor iniciado en el puerto {puerto}. Escuchando conexiones...");

                while (_ejecutando)
                {
                    TcpClient clienteTcp = await _listener.AcceptTcpClientAsync();
                    LogEvent?.Invoke($"Nueva conexión entrante desde {clienteTcp.Client.RemoteEndPoint}");
                    
                    // Instanciar manejador INMEDIATAMENTE para evitar chanque
                    ManejadorCliente manejador = new ManejadorCliente(clienteTcp, this);
                    _ = manejador.IniciarHandshakeAsync(); // Iniciar asíncronamente sin bloquear
                }
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke($"Error en el servidor: {ex.Message}");
            }
        }

        public void DetenerServidor()
        {
            _ejecutando = false;
            _listener?.Stop();
            foreach (var kvp in _clientes)
            {
                kvp.Value.Desconectar();
            }
            _clientes.Clear();
            NotificarActualizacionLista();
        }

        public bool RegistrarCliente(ManejadorCliente cliente, string aliasCandidato)
        {
            // Validar que el alias sea único entre los clientes actualmente conectados
            foreach (var c in _clientes.Values)
            {
                if (string.Equals(c.Alias, aliasCandidato, StringComparison.OrdinalIgnoreCase))
                {
                    return false; // Alias ya en uso
                }
            }

            cliente.Alias = aliasCandidato;

            // Registrar usando EndpointId como la clave del diccionario
            _clientes[cliente.EndpointId] = cliente;
            LogEvent?.Invoke($"Cliente registrado: {cliente.Alias} ({cliente.EndpointId})");
            
            NotificarActualizacionLista();
            EnviarListaContactosATodos();

            // Reenvío de grupos al reconectarse
            var gruposDelAlias = _gestorGrupos.ObtenerGruposDe(cliente.Alias);
            foreach (var grupo in gruposDelAlias)
            {
                _ = EnviarInfoGrupoAsync(grupo, cliente);
            }

            return true;
        }

        public void EliminarCliente(string endpointId, ManejadorCliente clienteInstancia = null)
        {
            if (_clientes.TryGetValue(endpointId, out ManejadorCliente existente))
            {
                if (clienteInstancia != null && existente != clienteInstancia)
                {
                    // Es una instancia vieja que se está desconectando, no eliminar la nueva
                    return;
                }
                
                if (_clientes.TryRemove(endpointId, out _))
                {
                    LogEvent?.Invoke($"Cliente desconectado: {existente.Alias} ({endpointId})");
                    NotificarActualizacionLista();
                    EnviarListaContactosATodos();
                }
            }
        }

        private void NotificarActualizacionLista()
        {
            var ipCounts = new Dictionary<string, int>();
            foreach (var c in _clientes.Values)
            {
                var ipPort = c.EndPoint.Split(':');
                if (ipPort.Length >= 2)
                {
                    var ip = string.Join(":", ipPort, 0, ipPort.Length - 1);
                    if (!ipCounts.ContainsKey(ip)) ipCounts[ip] = 0;
                    ipCounts[ip]++;
                }
            }

            var lista = new List<string>();
            foreach (var c in _clientes.Values)
            {
                var ipPort = c.EndPoint.Split(':');
                if (ipPort.Length >= 2)
                {
                    var ip = string.Join(":", ipPort, 0, ipPort.Length - 1);
                    var port = ipPort[ipPort.Length - 1];
                    if (ipCounts[ip] > 1)
                        lista.Add($"{c.NombreUsuario}|IP: {ip}:{port}");
                    else
                        lista.Add($"{c.NombreUsuario}|"); // Sin detalle extra
                }
                else
                {
                    lista.Add($"{c.NombreUsuario}|");
                }
            }

            ListaClientesActualizada?.Invoke(lista);
        }

        private void EnviarListaContactosATodos()
        {
            var usuarios = _clientes.Values.Select(c => $"{c.NombreUsuario}|{c.EndPoint}").ToList();
            string csv = string.Join(",", usuarios);
            byte[] csvBytes = Encoding.UTF8.GetBytes(csv);

            // Trama especial del servidor para lista de contactos:
            // Estructura: [LongitudDestinatario=0(Broadcast)][Tipo='L'][LongitudCsv(4bytes)][CsvBytes]...
            // Para simplificar, enviaremos la trama encapsulada como remitente "SERVER"
            
            byte[] trama1024 = new byte[1024];
            trama1024[0] = (byte)'L';
            byte[] lenBytes = Encoding.UTF8.GetBytes(csvBytes.Length.ToString("D4"));
            Array.Copy(lenBytes, 0, trama1024, 1, 4);
            Array.Copy(csvBytes, 0, trama1024, 5, Math.Min(csvBytes.Length, 1019));
            for(int i = 5 + csvBytes.Length; i < 1024; i++) trama1024[i] = 64; // padding

            byte[] remitenteBytes = Encoding.UTF8.GetBytes("SERVER");

            foreach (var cliente in _clientes.Values)
            {
                _ = cliente.EnviarTramaAsync(remitenteBytes, trama1024);
            }
        }

        private async Task EnviarInfoGrupoAsync(GrupoInfo grupo, ManejadorCliente cliente)
        {
            try
            {
                string payloadInfo = $"{grupo.Id}|{grupo.Nombre}|{grupo.Creador}|{string.Join(",", grupo.Miembros)}";
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadInfo);
                if (payloadBytes.Length > 1018)
                {
                    LogEvent?.Invoke($"Advertencia: La información del grupo {grupo.Nombre} ({grupo.Id}) excede los 1018 bytes y no se puede enviar a {cliente.Alias}.");
                    return;
                }

                byte[] tramaInfo = new byte[1024];
                tramaInfo[0] = (byte)'G';
                tramaInfo[1] = (byte)'2';
                byte[] lenBytes = Encoding.UTF8.GetBytes(payloadBytes.Length.ToString("D4"));
                Array.Copy(lenBytes, 0, tramaInfo, 2, 4);
                Array.Copy(payloadBytes, 0, tramaInfo, 6, payloadBytes.Length);
                for (int i = 6 + payloadBytes.Length; i < 1024; i++) tramaInfo[i] = 64; // '@' padding

                byte[] remitenteBytes = Encoding.UTF8.GetBytes("SERVER");
                await cliente.EnviarTramaAsync(remitenteBytes, tramaInfo);
            }
            catch (Exception ex)
            {
                LogEvent?.Invoke($"Error al enviar info de grupo {grupo.Nombre} a {cliente.Alias}: {ex.Message}");
            }
        }

        public async Task EnrutarTramaAsync(string remitente, string destinatario, byte[] trama1024)
        {
            if (destinatario.StartsWith("GRUPO:"))
            {
                // El id del grupo es el segundo segmento separado por ':'.
                // Esto tolera tanto "GRUPO:a1b2c3d4" (cuando el cliente EMISOR arma el destinatario)
                // como "GRUPO:a1b2c3d4:RICARDO" (cuando un cliente RECEPTOR reenvía una cancelación
                // usando como destinatario el remitente compuesto que él mismo recibió — ver Paso 5).
                string idGrupo = destinatario.Split(':')[1];
                var grupo = _gestorGrupos.ObtenerGrupo(idGrupo);
                if (grupo == null)
                {
                    LogEvent?.Invoke($"Grupo no encontrado: {idGrupo}");
                    return;
                }

                string remitenteReescrito = $"GRUPO:{idGrupo}:{remitente}";
                byte[] remBytes = Encoding.UTF8.GetBytes(remitenteReescrito);

                foreach (var aliasDestino in grupo.Miembros)
                {
                    if (string.Equals(aliasDestino, remitente, StringComparison.OrdinalIgnoreCase))
                        continue; // no reenviar al que mandó el mensaje

                    ManejadorCliente? clienteDestino = null;
                    foreach (var c in _clientes.Values)
                    {
                        if (string.Equals(c.Alias, aliasDestino, StringComparison.OrdinalIgnoreCase))
                        {
                            clienteDestino = c;
                            break;
                        }
                    }
                    if (clienteDestino != null)
                        await clienteDestino.EnviarTramaAsync(remBytes, trama1024);
                }
                return; // no seguir con la lógica de enrutamiento 1 a 1 normal
            }

            if (trama1024[0] == 'G')
            {
                char subtipo = (char)trama1024[1];
                if (subtipo == '1' && destinatario == "SERVER") // CREAR
                {
                    try
                    {
                        int longitud = int.Parse(Encoding.UTF8.GetString(trama1024, 2, 4));
                        string payload = Encoding.UTF8.GetString(trama1024, 6, longitud);
                        var partes = payload.Split('|');
                        string nombreGrupo = partes[0];
                        List<string> miembros = partes[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                        var grupo = _gestorGrupos.CrearGrupo(nombreGrupo, remitente, miembros);
                        LogEvent?.Invoke($"Grupo creado: {grupo.Nombre} ({grupo.Id}) por {grupo.Creador}");

                        // enviar la trama 'G' subtipo '2' (INFO) a cada miembro
                        foreach (var miembro in grupo.Miembros)
                        {
                            ManejadorCliente? clienteDestino = null;
                            foreach (var c in _clientes.Values)
                            {
                                if (string.Equals(c.Alias, miembro, StringComparison.OrdinalIgnoreCase))
                                {
                                    clienteDestino = c;
                                    break;
                                }
                            }
                            if (clienteDestino != null)
                            {
                                await EnviarInfoGrupoAsync(grupo, clienteDestino);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogEvent?.Invoke($"Error al procesar creación de grupo de {remitente}: {ex.Message}");
                    }
                }
                return;
            }

            if (destinatario == "BROADCAST")
            {
                // Enviar a todos menos al remitente
                byte[] remitenteBytes = Encoding.UTF8.GetBytes(remitente);
                foreach (var cliente in _clientes.Values)
                {
                    if (!string.Equals(cliente.Alias, remitente, StringComparison.OrdinalIgnoreCase))
                    {
                        await cliente.EnviarTramaAsync(remitenteBytes, trama1024);
                    }
                }
            }
            else
            {
                ManejadorCliente clienteDestino = null;
                foreach (var c in _clientes.Values)
                {
                    if (string.Equals(c.Alias, destinatario, StringComparison.OrdinalIgnoreCase))
                    {
                        clienteDestino = c;
                        break;
                    }
                }

                if (clienteDestino != null)
                {
                    byte[] remitenteBytes = Encoding.UTF8.GetBytes(remitente);
                    await clienteDestino.EnviarTramaAsync(remitenteBytes, trama1024);
                }
                else
                {
                    LogEvent?.Invoke($"No se pudo enrutar trama de {remitente} a {destinatario} (No encontrado)");
                }
            }
        }
    }
}

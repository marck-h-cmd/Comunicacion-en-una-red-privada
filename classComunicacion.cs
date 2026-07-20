// ClassComunicacion.cs
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace winProyComunicacion
{
    public class TramaEnrutada
    {
        public byte[] Trama1024 { get; set; }
        public string Destinatario { get; set; }
    }

    public class ContactoItem
    {
        public string Nombre { get; set; } = string.Empty;
        public string MostrarComo { get; set; } = string.Empty;
        public bool EsGrupo { get; set; } = false;
        public List<string> Miembros { get; set; } = null;
        public int UnreadCount { get; set; } = 0;
        public override string ToString() => MostrarComo;
    }

    internal class ClassComunicacion
    {
        private CapaTransporte _transporte;
        private ParserTramas _parser;

        public event Action<string, string> LlegoMensaje;
        public event Action<string> ServidorEncontrado;
        public event Action<string> NombreUsuarioRecibido;
        public event Action<List<ContactoItem>> ListaContactosActualizada;
        public event Action<string, string, string, List<string>> GrupoRecibido;

        public GestorTransferencias GestorArchivos { get; private set; }
        public GestorMensajes GestorChat { get; private set; }

        public bool IsConnected => _transporte.IsConnected;

        public ClassComunicacion()
        {
            _transporte = new CapaTransporte();
            _parser = new ParserTramas();

            GestorArchivos = new GestorTransferencias(_transporte.EncolarBajaPrioridad);
            GestorChat = new GestorMensajes(_transporte.EncolarMensaje);

            // Suscribir eventos del transporte
            _transporte.ServidorEncontrado += (ip) => ServidorEncontrado?.Invoke(ip);
            _transporte.TramaRecibida += (trama, remitente) => _parser.ProcesarTrama(trama, remitente);

            // Suscribir eventos del parser
            GestorChat.LlegoMensaje += (remitente, msg) => LlegoMensaje?.Invoke(remitente, msg);
            _parser.TramaMensajeRecibida += (trama, remitente) => GestorChat.ProcesarTrama(trama, remitente);
            _parser.NombreUsuarioRecibido += (nombre) => NombreUsuarioRecibido?.Invoke(nombre);
            _parser.ListaContactosActualizada += (lista) => ListaContactosActualizada?.Invoke(lista);
            _parser.GrupoRecibido += (id, nombre, creador, miembros) => GrupoRecibido?.Invoke(id, nombre, creador, miembros);
            _parser.TramaArchivoRecibida += (trama, remitente) => GestorArchivos.ProcesarTrama(trama, remitente);
        }

        public async Task<bool> ConectarServidorAsync(string ip, int puerto)
        {
            _parser.LimpiarEstado();
            GestorChat.LimpiarEstado();
            bool result = await _transporte.ConectarAsync(ip, puerto);
            if (!result)
            {
                MessageBox.Show("Error conectando al servidor.");
            }
            return result;
        }

        public void CerrarConexion()
        {
            GestorArchivos?.DetenerTodas();
            _transporte.CerrarConexion();
            _parser.LimpiarEstado();
            GestorChat?.LimpiarEstado();
        }

        public string GetLocalEndPoint() => _transporte.GetLocalEndPoint();
        public void StartDiscovery() => _transporte.StartDiscovery();
        public void StopDiscovery() => _transporte.StopDiscovery();

        public void EnviarNombreUsuario(string nombre)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(nombre);
            int len = Math.Min(nameBytes.Length, 1019);

            byte[] trama = new byte[1024];
            byte[] cab = Encoding.UTF8.GetBytes("N" + len.ToString("D4"));
            Array.Copy(cab, 0, trama, 0, 5);
            Array.Copy(nameBytes, 0, trama, 5, len);
            for (int i = 5 + len; i < 1024; i++) trama[i] = 64;

            _transporte.EncolarMensaje(trama, "SERVER");
        }



        public bool CrearGrupo(string nombre, List<string> miembros)
        {
            string payload = $"{nombre}|{string.Join(",", miembros)}";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            if (payloadBytes.Length > 1018) return false;

            byte[] trama = new byte[1024];
            byte[] cab = Encoding.UTF8.GetBytes("G1" + payloadBytes.Length.ToString("D4"));
            Array.Copy(cab, 0, trama, 0, 6);
            Array.Copy(payloadBytes, 0, trama, 6, payloadBytes.Length);
            for (int i = 6 + payloadBytes.Length; i < 1024; i++) trama[i] = 64;
            
            _transporte.EncolarMensaje(trama, "SERVER");
            return true;
        }
    }
}
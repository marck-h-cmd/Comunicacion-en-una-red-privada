using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace winProyComunicacion
{
    public class ParserTramas
    {
        public event Action<string> NombreUsuarioRecibido;
        public event Action<List<ContactoItem>> ListaContactosActualizada;
        public event Action<string, string, string, List<string>> GrupoRecibido;
        public event Action<byte[], string> TramaArchivoRecibida;
        public event Action<byte[], string> TramaMensajeRecibida;



        public void ProcesarTrama(byte[] trama, string remitente)
        {
            string tipo = Encoding.UTF8.GetString(trama, 0, 1);
            switch (tipo)
            {
                case "M":
                    TramaMensajeRecibida?.Invoke(trama, remitente);
                    break;
                case "F":
                case "A":
                case "C":
                case "R":
                    TramaArchivoRecibida?.Invoke(trama, remitente);
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



        private void ProcesarNombreUsuario(byte[] trama)
        {
            try
            {
                int longitud = int.Parse(Encoding.UTF8.GetString(trama, 1, 4));
                if (longitud > 0 && longitud <= 1019)
                {
                    string nombre = Encoding.UTF8.GetString(trama, 5, longitud);
                    NombreUsuarioRecibido?.Invoke(nombre);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RX] Error al procesar nombre de usuario: {ex.Message}");
            }
        }

        private void ProcesarListaContactos(byte[] trama)
        {
            try
            {
                int longitud = int.Parse(Encoding.UTF8.GetString(trama, 1, 4));
                string csv = Encoding.UTF8.GetString(trama, 5, longitud);
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

        private void ProcesarTramaGrupo(byte[] trama)
        {
            try
            {
                char subtipo = (char)trama[1];
                if (subtipo != '2') return;
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

        public void LimpiarEstado()
        {
        }
    }
}

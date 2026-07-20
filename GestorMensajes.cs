using System;
using System.Text;

namespace winProyComunicacion
{
    public class GestorMensajes
    {
        private readonly Action<byte[], string> _enviarTrama;
        private StringBuilder _mensajeRecibidoCompleto = new StringBuilder();

        public event Action<string, string> LlegoMensaje;

        public GestorMensajes(Action<byte[], string> enviarTrama)
        {
            _enviarTrama = enviarTrama;
        }

        public void EnviarMensaje(string m, string destinatario)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(m);
            int maxTrozo = 1019, offset = 0;

            while (offset < msgBytes.Length)
            {
                int remaining = msgBytes.Length - offset;
                int longTrozo = (remaining >= maxTrozo) ? (remaining == maxTrozo ? maxTrozo - 1 : maxTrozo) : remaining;

                byte[] trama = new byte[1024];
                byte[] cab = Encoding.UTF8.GetBytes("M" + longTrozo.ToString("D4"));
                Array.Copy(cab, 0, trama, 0, 5);
                Array.Copy(msgBytes, offset, trama, 5, longTrozo);
                for (int i = 5 + longTrozo; i < 1024; i++) trama[i] = 64;

                _enviarTrama(trama, destinatario);
                offset += longTrozo;
            }
        }

        public void ProcesarTrama(byte[] trama, string remitente)
        {
            try
            {
                string longitudStr = Encoding.UTF8.GetString(trama, 1, 4);
                if (!int.TryParse(longitudStr, out int longitud) || longitud < 0 || longitud > 1019) return;

                if (longitud > 0)
                    _mensajeRecibidoCompleto.Append(Encoding.UTF8.GetString(trama, 5, longitud));

                if (longitud < 1019)
                {
                    if (_mensajeRecibidoCompleto.Length > 0)
                    {
                        string mensajeFinal = _mensajeRecibidoCompleto.ToString();
                        _mensajeRecibidoCompleto.Clear();
                        LlegoMensaje?.Invoke(remitente, mensajeFinal);
                    }
                    else
                    {
                        _mensajeRecibidoCompleto.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RX] Error al procesar mensaje: {ex.Message}");
                _mensajeRecibidoCompleto.Clear();
            }
        }

        public void LimpiarEstado()
        {
            _mensajeRecibidoCompleto.Clear();
        }
    }
}

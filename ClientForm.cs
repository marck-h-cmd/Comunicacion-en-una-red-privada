using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace winProyComunicacion
{
    public partial class ClientForm : Form
    {
        // Clases auxiliares definidas dentro de ClientForm (nested classes)
        private class BubbleInfo
        {
            public string Texto { get; set; }
            public string Hora { get; set; }
            public bool EsEnviado { get; set; }
            public bool EsArchivo { get; set; }
            public string RutaArchivo { get; set; }
            public string RemitenteReal { get; set; }
            public bool IsCompleted { get; set; }
            public double Progreso { get; set; }
        }
        
        private class FileBubbleInfo
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public long FileSize { get; set; }
            public bool IsCompleted { get; set; }
            public Panel BubblePanel { get; set; }
        }
        
        private ClassComunicacion _enlace;
        private string _miNombre;
        private string _contactoSeleccionado = "";
        private List<ContactoItem> _misGrupos = new List<ContactoItem>();

        private readonly Color _colorFondoLista = Color.White;
        private readonly Color _colorHeader = Color.FromArgb(0, 128, 105);
        private readonly Color _colorSeleccionado = Color.FromArgb(165, 214, 201);
        private readonly Color _colorBordeAbajo = Color.FromArgb(230, 230, 230);
        private readonly Color _colorChatFondo = Color.FromArgb(240, 240, 240);
        private readonly Color _colorBurbujaEnviado = Color.FromArgb(226, 226, 226);
        private readonly Color _colorBurbujaRecibido = Color.FromArgb(196, 232, 225);

        // Diccionario: NombreContacto -> Lista de controles (burbujas)
        private Dictionary<string, List<Control>> _historialChats = new Dictionary<string, List<Control>>();
        
        // Diccionario para trackear la información de los archivos
        private Dictionary<int, FileBubbleInfo> _fileBubbles = new Dictionary<int, FileBubbleInfo>();
        private int _nextFileSlot = 0;
        
        // Timer para animación de carga
        private System.Windows.Forms.Timer _cargaTimer;
        private float _anguloAnimacion = 0;

        public ClientForm(string nombreInicial)
        {
            InitializeComponent();
            _miNombre = nombreInicial;

            _txtNombre.Text = string.IsNullOrEmpty(_miNombre) ? "User" + new Random().Next(100) : _miNombre;
            _btnConectar.Click += BtnConectar_Click;
            _lstContactos.DrawItem += LstContactos_DrawItem;
            _lstContactos.SelectedIndexChanged += LstContactos_SelectedIndexChanged;

            pnlAvatarHeader.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var grayBrush = new SolidBrush(Color.FromArgb(180, 180, 180)))
                    {
                        e.Graphics.FillEllipse(grayBrush, 0, 0, 40, 40);
                    }
                    using (var whitePen = new Pen(Color.White, 2))
                    {
                        e.Graphics.DrawArc(whitePen, 8, 20, 24, 24, 180, 180);
                    }
                    e.Graphics.FillEllipse(Brushes.White, 12, 6, 16, 16);
                    e.Graphics.FillEllipse(Brushes.LightGreen, 28, 28, 10, 10);
                };

            _pnlChatHistory.Resize += (s, e) =>
            {
                foreach (Control c in _pnlChatHistory.Controls) c.Width = _pnlChatHistory.ClientSize.Width - 40;
            };

            _txtInput.TextChanged += TxtInput_TextChanged;

            _txtInput.Enter += (s, e) => 
            { 
                if (_txtInput.Text == "Type a message...") 
                { 
                    _txtInput.Text = ""; 
                    _txtInput.ForeColor = Color.Black; 
                } 
            };
            _txtInput.Leave += (s, e) => 
            { 
                if (_txtInput.Text == "") 
                { 
                    _txtInput.Text = "Type a message..."; 
                    _txtInput.ForeColor = Color.Gray; 
                } 
            };
            _btnFile.Click += BtnFile_Click;
            _btnSend.Click += BtnSend_Click;
            _btnNuevoGrupo.Click += BtnNuevoGrupo_Click;

            _enlace = new ClassComunicacion();
            _enlace.LlegoMensaje += Enlace_LlegoMensaje;
            _enlace.ListaContactosActualizada += Enlace_ListaContactosActualizada;
            _enlace.GestorArchivos.ArchivoEntrante += Enlace_ArchivoEntrante;
            _enlace.GestorArchivos.ProgresoArchivo += Enlace_ProgresoArchivo;
            _enlace.GestorArchivos.TransferenciaCompletada += Enlace_TransferenciaCompletada;
            _enlace.NombreUsuarioRecibido += Enlace_NombreUsuarioRecibido;
            _enlace.GrupoRecibido += Enlace_GrupoRecibido;
            _enlace.ServidorEncontrado += async (serverIp) =>
            {
                Invoke(async () =>
                {
                    if (!_enlace.IsConnected)
                    {
                        _txtIp.Text = serverIp;
                        BtnConectar_Click(this, EventArgs.Empty);
                    }
                });
            };
            _enlace.StartDiscovery();

            // Inicializar Timer para animación de carga
            _cargaTimer = new System.Windows.Forms.Timer();
            _cargaTimer.Interval = 50; // 20 fps
            _cargaTimer.Tick += (s, e) =>
            {
                _anguloAnimacion += 5f; // Aumentar el ángulo
                if (_anguloAnimacion > 360) _anguloAnimacion -= 360;
                
                // Repintar todas las burbujas de archivos en progreso
                foreach (var fileInfo in _fileBubbles.Values)
                {
                    if (!fileInfo.IsCompleted && fileInfo.BubblePanel != null)
                    {
                        fileInfo.BubblePanel.Invalidate();
                    }
                }
            };
            _cargaTimer.Start();
            
            // Forzar layout y repintado al cargar la ventana
            this.Load += (s, e) => 
            {
                // Establecer el foco en otro control primero para evitar que el textbox se active
                _lstContactos.Focus();
                // Forzar todos los layouts
                pnlBottom.PerformLayout();
                pnlInputWrap.PerformLayout();
                _txtInput.PerformLayout();
                _btnFile.PerformLayout();
                _btnSend.PerformLayout();
                // Refrescar todo
                pnlBottom.Refresh();
                pnlInputWrap.Refresh();
                _txtInput.Refresh();
                _btnFile.Refresh();
                _btnSend.Refresh();
                this.Invalidate(true);
                this.Update();
                // Forzar un pequeño retraso y volver a repintar para asegurar
                System.Threading.Thread.Sleep(50);
                this.Invalidate(true);
                this.Update();
                
                // Centrar las etiquetas del panel de bienvenida
                CentrarEtiquetasBienvenida();
            };
            
            // Centrar las etiquetas cuando cambie el tamaño del panel de bienvenida
            pnlWelcome.Resize += (s, e) => 
            {
                CentrarEtiquetasBienvenida();
            };
            
            // Limpiar el Timer cuando se cierre el formulario
            this.FormClosing += (s, e) => 
            {
                _cargaTimer?.Stop();
                _cargaTimer?.Dispose();
            };
        }

        // Método para centrar las etiquetas del panel de bienvenida
        private void CentrarEtiquetasBienvenida()
        {
            // Centrar la etiqueta principal
            lblWelcome.Left = (pnlWelcome.ClientSize.Width - lblWelcome.Width) / 2;
            lblWelcome.Top = (pnlWelcome.ClientSize.Height - lblWelcome.Height - lblWelcomeSub.Height - 20) / 2;
            
            // Centrar la etiqueta secundaria debajo de la principal
            lblWelcomeSub.Left = (pnlWelcome.ClientSize.Width - lblWelcomeSub.Width) / 2;
            lblWelcomeSub.Top = lblWelcome.Bottom + 20;
        }
        
        private void TxtInput_TextChanged(object sender, EventArgs e)
        {
            // No hay ajuste automático de altura - la barra queda fija
        }

        private void LstContactos_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            ContactoItem item = _lstContactos.Items[e.Index] as ContactoItem;
            if (item == null) return;
            string nombre = item.MostrarComo;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isSelected = e.State.HasFlag(DrawItemState.Selected);
            using (SolidBrush b = new SolidBrush(isSelected ? _colorSeleccionado : _colorFondoLista))
            {
                g.FillRectangle(b, e.Bounds);
            }

            int avatarSize = 50;
            int avatarX = e.Bounds.Left + 15;
            int avatarY = e.Bounds.Top + 10;

            if (item.EsGrupo)
            {
                // Draw group avatar: Blue/Cyan circle
                using (SolidBrush brushAvatar = new SolidBrush(Color.FromArgb(0, 100, 150)))
                {
                    g.FillEllipse(brushAvatar, avatarX, avatarY, avatarSize, avatarSize);
                }
                // Draw group representation: two overlapping small circles
                using (SolidBrush brushPerson = new SolidBrush(Color.White))
                {
                    // Left person
                    g.FillEllipse(brushPerson, avatarX + 10, avatarY + 12, 14, 14);
                    g.FillEllipse(brushPerson, avatarX + 5, avatarY + 28, 24, 16);

                    // Right person (drawn slightly offset/behind or in front)
                    g.FillEllipse(Brushes.LightGray, avatarX + 24, avatarY + 15, 12, 12);
                    g.FillEllipse(Brushes.LightGray, avatarX + 20, avatarY + 29, 20, 14);
                }
            }
            else
            {
                // Single person avatar: Gray circle
                using (SolidBrush brushAvatar = new SolidBrush(Color.FromArgb(180, 180, 180)))
                {
                    g.FillEllipse(brushAvatar, avatarX, avatarY, avatarSize, avatarSize);
                }
                using (Pen whitePen = new Pen(Color.White, 3))
                {
                    g.DrawArc(whitePen, avatarX + 10, avatarY + 25, 30, 30, 180, 180);
                    g.FillEllipse(Brushes.White, avatarX + 15, avatarY + 8, 20, 20);
                }

                int dotSize = 12;
                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(37, 211, 102)))
                {
                    g.FillEllipse(dotBrush, avatarX + avatarSize - dotSize - 2, avatarY + avatarSize - dotSize - 2, dotSize, dotSize);
                }
            }

            // Draw unread count badge if there are unread messages
            if (item.UnreadCount > 0)
            {
                int badgeSize = 24;
                int badgeX = e.Bounds.Right - badgeSize - 20;
                int badgeY = e.Bounds.Top + (e.Bounds.Height - badgeSize) / 2;

                using (SolidBrush badgeBrush = new SolidBrush(Color.FromArgb(0, 150, 136)))
                {
                    g.FillEllipse(badgeBrush, badgeX, badgeY, badgeSize, badgeSize);
                }

                using (Font badgeFont = new Font("Segoe UI", 10, FontStyle.Bold))
                using (StringFormat badgeFormat = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(item.UnreadCount.ToString(), badgeFont, Brushes.White, new RectangleF(badgeX, badgeY, badgeSize, badgeSize), badgeFormat);
                }
            }

            int textX = avatarX + avatarSize + 15;
            int maxTextWidth = item.UnreadCount > 0 ? e.Bounds.Right - textX - 60 : e.Bounds.Width - textX - 5; // Leave space for badge
            RectangleF rectText = new RectangleF(textX, e.Bounds.Top + 15, maxTextWidth, 25);
            using (Font fName = new Font("Segoe UI", 12, FontStyle.Regular))
            using (StringFormat sf = new StringFormat() { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                g.DrawString(nombre, fName, Brushes.Black, rectText, sf);
            }

            RectangleF rectStatus = new RectangleF(textX, e.Bounds.Top + 38, maxTextWidth, 20);
            using (Font fStatus = new Font("Segoe UI", 9, FontStyle.Regular))
            using (StringFormat sf = new StringFormat() { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                string statusText = item.EsGrupo ? (item.Miembros != null ? string.Join(", ", item.Miembros) : "Grupo") : "online";
                g.DrawString(statusText, fStatus, Brushes.Gray, rectStatus, sf);
            }

            using (Pen sepPen = new Pen(_colorBordeAbajo))
            {
                g.DrawLine(sepPen, textX, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        private async void BtnConectar_Click(object sender, EventArgs e)
        {
            _miNombre = _txtNombre.Text.Trim();
            if (string.IsNullOrEmpty(_miNombre)) return;

            if (_miNombre.Contains(":") || _miNombre.Contains(",") || _miNombre.Contains("|") || _miNombre.StartsWith("GRUPO:", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("El alias elegido no es válido. No debe contener ':', ',' o '|', ni empezar con 'GRUPO:'.", "Nombre inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool ok = true;
            if (!_enlace.IsConnected)
            {
                _misGrupos.Clear();
                _historialChats.Clear();
                _pnlChatHistory.Controls.Clear();
                _contactoSeleccionado = "";
                _lblHeaderChatNombre.Text = "SELECCIONA UN CONTACTO";
                _lblHeaderChatStatus.Text = "";

                ok = await _enlace.ConectarServidorAsync(_txtIp.Text, 8000);
            }

            if (ok)
            {
                _btnConectar.Enabled = false;
                _txtIp.Enabled = false;
                _txtNombre.Enabled = false;
                _btnNuevoGrupo.Enabled = true;
                _enlace.EnviarNombreUsuario(_miNombre);
                this.Text = "TCP IP CHAT CLIENT - " + _miNombre;

                string localPort = _enlace.GetLocalEndPoint();
                if (!string.IsNullOrEmpty(localPort))
                {
                    lblLocalPort.Text = "Puerto Local: " + localPort;
                }
                
                // Mostrar el panel de bienvenida cuando se conecta
                pnlWelcome.Visible = true;
                pnlChatHeader.Visible = false;
                _pnlChatHistory.Visible = false;
                pnlBottom.Visible = false;
            }
        }

        private void LstContactos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lstContactos.SelectedItem is ContactoItem ci)
            {
                _contactoSeleccionado = ci.Nombre;
                // Resetear contador de mensajes no leídos
                ci.UnreadCount = 0;
                _lstContactos.Invalidate();
                
                _lblHeaderChatNombre.Text = ci.MostrarComo;
                if (ci.EsGrupo)
                {
                    _lblHeaderChatStatus.Text = ci.Miembros != null ? string.Join(", ", ci.Miembros) : "";
                }
                else
                {
                    _lblHeaderChatStatus.Text = "online";
                }
                CargarHistorial(_contactoSeleccionado);
                
                // Ocultar panel de bienvenida y mostrar el chat
                pnlWelcome.Visible = false;
                pnlChatHeader.Visible = true;
                _pnlChatHistory.Visible = true;
                pnlBottom.Visible = true;
            }
            else
            {
                // Si no hay contacto seleccionado, mostrar panel de bienvenida
                pnlWelcome.Visible = true;
                pnlChatHeader.Visible = false;
                _pnlChatHistory.Visible = false;
                pnlBottom.Visible = false;
            }
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_contactoSeleccionado)) return;
            string msg = _txtInput.Text;
            if (string.IsNullOrEmpty(msg) || msg == "Type a message...") return;

            _enlace.enviarMensaje(msg, _contactoSeleccionado);
            AgregarBurbuja(msg, DateTime.Now.ToString("HH:mm"), true, _contactoSeleccionado, false, null);
            _txtInput.Clear();
        }

        private void BtnFile_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_contactoSeleccionado)) return;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true; // Permitir selección de múltiples archivos
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (ofd.FileNames.Length > 5)
                    {
                        MessageBox.Show("Has seleccionado más de 5 archivos, se enviarán los primeros 5.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    
                    int maxFiles = Math.Min(ofd.FileNames.Length, 5);
                    for (int i = 0; i < maxFiles; i++)
                    {
                        string filePath = ofd.FileNames[i];
                        _enlace.GestorArchivos.EnviarArchivo(filePath, _contactoSeleccionado);
                        AgregarBurbuja("", DateTime.Now.ToString("HH:mm"), true, _contactoSeleccionado, true, filePath, null, true);
                    }
                }
            }
        }

        private (bool esGrupo, string? idGrupo, string remitenteReal) ParsearRemitente(string remitente)
        {
            if (remitente.StartsWith("GRUPO:"))
            {
                var partes = remitente.Split(':');
                if (partes.Length >= 3)
                {
                    return (true, partes[1], partes[2]);
                }
            }
            return (false, null, remitente);
        }

        private void Enlace_LlegoMensaje(string remitente, string m)
        {
            Invoke((MethodInvoker)(() =>
            {
                var (esGrupo, idGrupo, remitenteReal) = ParsearRemitente(remitente);
                string claveContacto = esGrupo ? "GRUPO:" + idGrupo : remitente;
                
                // Incrementar contador de no leídos si el contacto no está seleccionado
                if (claveContacto != _contactoSeleccionado)
                {
                    foreach (ContactoItem item in _lstContactos.Items)
                    {
                        if (item.Nombre == claveContacto)
                        {
                            item.UnreadCount++;
                            break;
                        }
                    }
                    _lstContactos.Invalidate(); // Redibujar lista de contactos
                }

                if (esGrupo)
                {
                    AgregarBurbuja(m, DateTime.Now.ToString("HH:mm"), false, claveContacto, false, null, remitenteReal);
                }
                else
                {
                    AgregarBurbuja(m, DateTime.Now.ToString("HH:mm"), false, claveContacto, false);
                }
            }));
        }

        private void Enlace_NombreUsuarioRecibido(string status)
        {
            if (status == "ERR_ALIAS_EN_USO")
            {
                Invoke((MethodInvoker)(() =>
                {
                    MessageBox.Show("El alias elegido ya está en uso. Por favor, elige otro.", "Nombre duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _btnConectar.Enabled = true;
                    _txtNombre.Enabled = true;
                    _txtNombre.Focus();
                }));
            }
            else if (status == "ERR_ALIAS_INVALIDO")
            {
                Invoke((MethodInvoker)(() =>
                {
                    MessageBox.Show("El alias elegido no es válido. No debe contener ':', ',' o '|', ni empezar con 'GRUPO:'.", "Nombre inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _btnConectar.Enabled = true;
                    _txtNombre.Enabled = true;
                    _txtNombre.Focus();
                }));
            }
        }

        private void Enlace_ListaContactosActualizada(List<ContactoItem> lista)
        {
            Invoke((MethodInvoker)(() =>
            {
                string prevSel = _contactoSeleccionado;
                _lstContactos.Items.Clear();

                foreach (var item in lista)
                {
                    if (item.Nombre != _miNombre)
                    {
                        _lstContactos.Items.Add(item);
                    }
                }

                // Repoblar grupos conocidos
                foreach (var grupo in _misGrupos)
                {
                    _lstContactos.Items.Add(grupo);
                }

                var bcItem = new ContactoItem { Nombre = "BROADCAST", MostrarComo = "BROADCAST" };
                _lstContactos.Items.Add(bcItem);

                if (!string.IsNullOrEmpty(prevSel))
                {
                    foreach (ContactoItem ci in _lstContactos.Items)
                    {
                        if (ci.Nombre == prevSel)
                        {
                            _lstContactos.SelectedItem = ci;
                            break;
                        }
                    }
                }
            }));
        }

        private void Enlace_ArchivoEntrante(string remitente, int slot, string nombre, long tamaño)
        {
            Invoke((MethodInvoker)(() =>
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TatoText");
                Directory.CreateDirectory(dir);

                // Resolver colisión de nombre de archivo
                string nombreSinExt = Path.GetFileNameWithoutExtension(nombre);
                string ext = Path.GetExtension(nombre);
                int contador = 1;
                string rutaLocal = Path.Combine(dir, nombre);
                while (File.Exists(rutaLocal))
                {
                    rutaLocal = Path.Combine(dir, $"{nombreSinExt} ({contador}){ext}");
                    contador++;
                }

                // Usar el nombre de archivo final para la burbuja de chat y el log
                string nombreFinal = Path.GetFileName(rutaLocal);

                // Primero agregamos la burbuja de archivo directamente
                var (esGrupo, idGrupo, remitenteReal) = ParsearRemitente(remitente);
                string contactoAsociado = esGrupo ? "GRUPO:" + idGrupo : remitente;
                AgregarBurbuja("", DateTime.Now.ToString("HH:mm"), false, contactoAsociado, true, rutaLocal, remitenteReal, false);

                // Guardamos la referencia a la burbuja para actualizarla después
                if (_historialChats.ContainsKey(contactoAsociado) && _historialChats[contactoAsociado].Count > 0)
                {
                    Panel lastBubble = _historialChats[contactoAsociado][_historialChats[contactoAsociado].Count - 1] as Panel;
                    if (lastBubble != null)
                    {
                        _fileBubbles[slot] = new FileBubbleInfo
                        {
                            FilePath = rutaLocal,
                            FileName = nombreFinal,
                            FileSize = tamaño,
                            IsCompleted = false,
                            BubblePanel = lastBubble
                        };
                    }
                }

                _enlace.GestorArchivos.AceptarArchivoEntrante(slot, rutaLocal);
            }));
        }

        private void Enlace_ProgresoArchivo(int slot, double prog)
        {
            Invoke((MethodInvoker)(() =>
            {
                if (_fileBubbles.ContainsKey(slot))
                {
                    var fileInfo = _fileBubbles[slot];
                    if (fileInfo.BubblePanel != null && fileInfo.BubblePanel.Tag is BubbleInfo info)
                    {
                        info.Progreso = prog;
                        fileInfo.BubblePanel.Invalidate();
                    }
                }
            }));
        }

        private void Enlace_TransferenciaCompletada(int slot, string nombre)
        {
            Invoke((MethodInvoker)(() =>
            {
                if (_fileBubbles.ContainsKey(slot))
                {
                    var fileInfo = _fileBubbles[slot];
                    fileInfo.IsCompleted = true;

                    // Actualizamos el Tag del panel con el estado completado
                    if (fileInfo.BubblePanel.Tag is BubbleInfo info)
                    {
                        info.IsCompleted = true;
                    }

                    // Actualizamos la burbuja - volvemos a dibujarla
                    fileInfo.BubblePanel.Invalidate();
                }
            }));
        }

        private void CargarHistorial(string contacto)
        {
            _pnlChatHistory.Controls.Clear();
            if (_historialChats.ContainsKey(contacto))
            {
                foreach (var control in _historialChats[contacto])
                {
                    _pnlChatHistory.Controls.Add(control);
                }
                _pnlChatHistory.ScrollControlIntoView(_historialChats[contacto][_historialChats[contacto].Count - 1]);
            }
        }

        // Método auxiliar para formatear el tamaño del archivo
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        // Método para previsualizar el archivo
        // Métodos auxiliares para dibujar rectángulos redondeados
        private void DrawRoundedRectangle(Graphics g, Pen pen, float x, float y, float width, float height, float radius)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(x, y, radius, radius, 180, 90);
                path.AddArc(x + width - radius, y, radius, radius, 270, 90);
                path.AddArc(x + width - radius, y + height - radius, radius, radius, 0, 90);
                path.AddArc(x, y + height - radius, radius, radius, 90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            }
        }

        private void FillRoundedRectangle(Graphics g, Brush brush, float x, float y, float width, float height, float radius)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(x, y, radius, radius, 180, 90);
                path.AddArc(x + width - radius, y, radius, radius, 270, 90);
                path.AddArc(x + width - radius, y + height - radius, radius, radius, 0, 90);
                path.AddArc(x, y + height - radius, radius, radius, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }

        private void PreviewFile(string filePath, bool isCompleted)
        {
            if (!File.Exists(filePath) || !isCompleted)
            {
                MessageBox.Show("El archivo aún se está descargando o no está disponible.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se puede abrir el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel AgregarBurbuja(string texto, string hora, bool esEnviado, string contactoAsociado, bool esArchivo, string? rutaArchivo = null, string? remitenteReal = null, bool? isCompleted = null)
        {
            Panel p = new Panel { Width = _pnlChatHistory.ClientSize.Width - 40, Height = 60, Margin = new Padding(0, 0, 0, 10) };
            
            // Guardar información de la burbuja en el Tag
            BubbleInfo info = new BubbleInfo
            {
                Texto = texto,
                Hora = hora,
                EsEnviado = esEnviado,
                EsArchivo = esArchivo,
                RutaArchivo = rutaArchivo,
                RemitenteReal = remitenteReal,
                IsCompleted = isCompleted ?? false,
                Progreso = 0 // Inicia en 0
            };
            p.Tag = info;

            if (esArchivo && !string.IsNullOrEmpty(rutaArchivo))
            {
                p.Cursor = Cursors.Hand;
                p.Click += (s, e) => 
                {
                    if (p.Tag is BubbleInfo currentInfo)
                    {
                        PreviewFile(currentInfo.RutaArchivo, currentInfo.IsCompleted);
                    }
                };
            }

            p.Paint += (s, e) =>
            {
                if (!(p.Tag is BubbleInfo info)) return;
                
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int maxBurbujaWidth = p.Width - 80;

                using Font fTxt = new Font("Segoe UI", 11);
                using Font fHora = new Font("Segoe UI", 8);
                using Font fIcon = new Font("Segoe UI", 18);
                
                string displayText = info.Texto;
                
                if (info.EsArchivo && !string.IsNullOrEmpty(info.RutaArchivo))
                {
                    string currentFileName = Path.GetFileName(info.RutaArchivo);
                    string currentFileSize = "";
                    if (File.Exists(info.RutaArchivo))
                    {
                        FileInfo fi = new FileInfo(info.RutaArchivo);
                        currentFileSize = FormatFileSize(fi.Length);
                    }
                    displayText = $"{currentFileName}\n{currentFileSize}";
                }

                SizeF sizeTxt = e.Graphics.MeasureString(displayText, fTxt, maxBurbujaWidth - 60);

                int nameHeight = 0;
                int nameWidth = 0;
                Font? fSender = null;
                SolidBrush? brushSender = null;
                if (!string.IsNullOrEmpty(info.RemitenteReal) && !info.EsEnviado)
                {
                    fSender = new Font("Segoe UI", 9, FontStyle.Bold);
                    brushSender = new SolidBrush(Color.FromArgb(30, 100, 180));
                    nameHeight = 18;
                    nameWidth = (int)e.Graphics.MeasureString(info.RemitenteReal, fSender).Width + 40;
                }

                int iconWidth = info.EsArchivo ? 40 : 0;
                int burbujaW = Math.Max((int)sizeTxt.Width + 60 + iconWidth, nameWidth);
                burbujaW = Math.Max(burbujaW, 120);

                int burbujaH = (int)sizeTxt.Height + 30 + nameHeight;
                p.Height = burbujaH + 10;

                int x = info.EsEnviado ? p.Width - burbujaW - 10 : 10;
                int y = 0;

                using GraphicsPath path = new GraphicsPath();
                int rad = 10;
                path.AddArc(x, y, rad, rad, 180, 90);
                path.AddArc(x + burbujaW - rad, y, rad, rad, 270, 90);
                path.AddArc(x + burbujaW - rad, y + burbujaH - rad, rad, rad, 0, 90);
                path.AddArc(x, y + burbujaH - rad, rad, rad, 90, 90);
                path.CloseFigure();

                Color fill = info.EsEnviado ? _colorBurbujaEnviado : _colorBurbujaRecibido;
                using (var fillBrush = new SolidBrush(fill))
                {
                    e.Graphics.FillPath(fillBrush, path);
                }

                // Cola de burbuja
                using (var tailBrush = new SolidBrush(fill))
                {
                    if (info.EsEnviado) 
                        e.Graphics.FillPolygon(tailBrush, new Point[] { new Point(x + burbujaW - 5, y), new Point(x + burbujaW + 10, y), new Point(x + burbujaW - 5, y + 15) });
                    else 
                        e.Graphics.FillPolygon(tailBrush, new Point[] { new Point(x + 5, y), new Point(x - 10, y), new Point(x + 5, y + 15) });
                }

                if (!string.IsNullOrEmpty(info.RemitenteReal) && !info.EsEnviado && fSender != null && brushSender != null)
                {
                    e.Graphics.DrawString(info.RemitenteReal, fSender, brushSender, x + 10, y + 5);
                }

                if (info.EsArchivo)
                {
                    // Dibujar icono de archivo o círculo de carga
                    if (!info.IsCompleted)
                    {
                        // Círculo de carga animado
                        float centroX = x + 30;
                        float centroY = y + 30 + nameHeight;
                        float radio = 12;
                        
                        // Fondo del círculo
                        using (var fondoCirculo = new SolidBrush(Color.FromArgb(240, 240, 240)))
                        {
                            e.Graphics.FillEllipse(fondoCirculo, centroX - radio, centroY - radio, radio * 2, radio * 2);
                        }
                        
                        // Círculo animado
                        using (var penAnimado = new Pen(Color.FromArgb(0, 150, 136), 3))
                        {
                            // Círculo principal con el progreso
                            float anguloInicio = _anguloAnimacion - 90;
                            float anguloProgreso = ((float)info.Progreso / 100f) * 360;
                            e.Graphics.DrawArc(penAnimado, centroX - radio, centroY - radio, radio * 2, radio * 2, anguloInicio, anguloProgreso);
                            
                            // Pequeño círculo animado extra
                            using (var penExtra = new Pen(Color.FromArgb(0, 180, 160), 2))
                            {
                                e.Graphics.DrawArc(penExtra, centroX - radio + 3, centroY - radio + 3, (radio - 3) * 2, (radio - 3) * 2, -anguloInicio, 120);
                            }
                        }
                        
                        // Dibujar el porcentaje en el centro del círculo
                        using (var fPorcentaje = new Font("Segoe UI", 7, FontStyle.Bold))
                        {
                            string textoPorcentaje = $"{info.Progreso:0}";
                            SizeF sizePorcentaje = e.Graphics.MeasureString(textoPorcentaje, fPorcentaje);
                            float textoX = centroX - sizePorcentaje.Width / 2;
                            float porcentajeY = centroY - sizePorcentaje.Height / 2;
                            e.Graphics.DrawString(textoPorcentaje, fPorcentaje, Brushes.DarkSlateGray, textoX, porcentajeY);
                        }
                    }
                    else
                    {
                        // Usar emoji con fuente que lo soporte correctamente
                        using Font emojiFont = new Font("Segoe UI Emoji", 16, FontStyle.Regular, GraphicsUnit.Pixel);
                        Brush emojiBrush = Brushes.DimGray;
                        string emoji = "📄";
                        
                        // Medir el tamaño del emoji para centrarlo
                        SizeF emojiSize = e.Graphics.MeasureString(emoji, emojiFont);
                        float iconX = x + 15; // Un poco más a la derecha para centrar
                        float iconY = y + 15 + nameHeight;
                        
                        e.Graphics.DrawString(emoji, emojiFont, emojiBrush, iconX, iconY);
                    }
                    
                    // Dibujar texto (nombre y tamaño)
                    float textoY = y + 8 + nameHeight;
                    float textoHeight = sizeTxt.Height;
                    e.Graphics.DrawString(displayText, fTxt, Brushes.Black, new RectangleF(x + 50, textoY, burbujaW - 60, textoHeight));
                    
                    // Si no está completado, dibujar barra de progreso debajo
                    if (!info.IsCompleted)
                    {
                        float barraX = x + 50;
                        float barraY = textoY + textoHeight + 8;
                        float barraWidth = burbujaW - 60;
                        float barraHeight = 10;
                        
                        // Fondo de la barra
                        using (var fondoBarra = new SolidBrush(Color.FromArgb(220, 220, 220)))
                        {
                            FillRoundedRectangle(e.Graphics, fondoBarra, barraX, barraY, barraWidth, barraHeight, 5);
                        }
                        
                        // Progreso de la barra
                        float progresoWidth = ((float)info.Progreso / 100f) * barraWidth;
                        using (var progresoBarra = new SolidBrush(Color.FromArgb(0, 150, 136)))
                        {
                            FillRoundedRectangle(e.Graphics, progresoBarra, barraX, barraY, progresoWidth, barraHeight, 5);
                        }
                        
                        // Texto de progreso a la derecha
                        using (var fProgreso = new Font("Segoe UI", 8, FontStyle.Bold))
                        {
                            string textoProgreso = $"{info.Progreso:0}%";
                            SizeF sizeProgreso = e.Graphics.MeasureString(textoProgreso, fProgreso);
                            float progresoTextX = barraX + barraWidth - sizeProgreso.Width - 5;
                            float progresoTextY = barraY + (barraHeight - sizeProgreso.Height) / 2f;
                            e.Graphics.DrawString(textoProgreso, fProgreso, Brushes.DarkSlateGray, progresoTextX, progresoTextY);
                        }
                    }
                }
                else
                {
                    e.Graphics.DrawString(displayText, fTxt, Brushes.Black, new RectangleF(x + 10, y + 5 + nameHeight, burbujaW - 20, burbujaH - 25 - nameHeight));
                }

                string horaStr = info.Hora + (info.EsEnviado ? " ✔" : "");
                e.Graphics.DrawString(horaStr, fHora, Brushes.Gray, x + burbujaW - 50, y + burbujaH - 20);

                if (fSender != null) fSender.Dispose();
                if (brushSender != null) brushSender.Dispose();
            };

            if (!_historialChats.ContainsKey(contactoAsociado)) _historialChats[contactoAsociado] = new List<Control>();
            _historialChats[contactoAsociado].Add(p);

            if (contactoAsociado == _contactoSeleccionado)
            {
                _pnlChatHistory.Controls.Add(p);
                _pnlChatHistory.ScrollControlIntoView(p);
            }

            return p;
        }

        private void BtnNuevoGrupo_Click(object? sender, EventArgs e)
        {
            List<string> contactosDisponibles = new List<string>();
            foreach (ContactoItem item in _lstContactos.Items)
            {
                if (!item.EsGrupo && item.Nombre != "BROADCAST" && item.Nombre != _miNombre)
                {
                    contactosDisponibles.Add(item.Nombre);
                }
            }

            using (FormCrearGrupo dlg = new FormCrearGrupo(contactosDisponibles))
            {
                while (true)
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        bool ok = _enlace.CrearGrupo(dlg.NombreGrupo, dlg.MiembrosSeleccionados);
                        if (ok)
                        {
                            break;
                        }
                        else
                        {
                            MessageBox.Show("El tamaño de la información del grupo es demasiado grande (supera los 1018 bytes). Intente con menos miembros o nombres más cortos.", "Error al crear grupo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void Enlace_GrupoRecibido(string idGrupo, string nombre, string creador, List<string> miembros)
        {
            Invoke((MethodInvoker)(() =>
            {
                var item = new ContactoItem
                {
                    Nombre = "GRUPO:" + idGrupo,
                    MostrarComo = nombre,
                    EsGrupo = true,
                    Miembros = miembros
                };

                bool yaExisteEnMisGrupos = false;
                foreach (var g in _misGrupos)
                {
                    if (g.Nombre == item.Nombre)
                    {
                        g.MostrarComo = nombre;
                        g.Miembros = miembros;
                        yaExisteEnMisGrupos = true;
                        break;
                    }
                }
                if (!yaExisteEnMisGrupos)
                {
                    _misGrupos.Add(item);
                }

                bool yaExisteVisual = false;
                foreach (ContactoItem ci in _lstContactos.Items)
                {
                    if (ci.Nombre == item.Nombre) { yaExisteVisual = true; break; }
                }
                if (!yaExisteVisual)
                {
                    _lstContactos.Items.Add(item);
                }
            }));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _enlace?.CerrarConexion();
            base.OnFormClosing(e);
        }

        private void _txtInput_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

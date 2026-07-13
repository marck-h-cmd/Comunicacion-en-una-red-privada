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

        public ClientForm(string nombreInicial)
        {
            InitializeComponent();
            _miNombre = nombreInicial;
            
            _txtNombre.Text = string.IsNullOrEmpty(_miNombre) ? "User" + new Random().Next(100) : _miNombre;
            _btnConectar.Click += BtnConectar_Click;
            _lstContactos.DrawItem += LstContactos_DrawItem;
            _lstContactos.SelectedIndexChanged += LstContactos_SelectedIndexChanged;
            
            pnlAvatarHeader.Paint += (s,e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(180,180,180)), 0,0,40,40);
                e.Graphics.DrawArc(new Pen(Color.White, 2), 8, 20, 24, 24, 180, 180);
                e.Graphics.FillEllipse(Brushes.White, 12, 6, 16, 16);
                e.Graphics.FillEllipse(Brushes.LightGreen, 28, 28, 10, 10);
            };
            
            _pnlChatHistory.Resize += (s, e) => 
            {
                foreach (Control c in _pnlChatHistory.Controls) c.Width = _pnlChatHistory.ClientSize.Width - 40;
            };
            
            _txtInput.Multiline = true;
            _txtInput.WordWrap = true;
            _txtInput.ScrollBars = ScrollBars.None;
            _txtInput.TextChanged += TxtInput_TextChanged;

            _txtInput.Enter += (s,e) => { if (_txtInput.Text == "Type a message...") { _txtInput.Text = ""; _txtInput.ForeColor = Color.Black; } };
            _txtInput.Leave += (s,e) => { if (_txtInput.Text == "") { _txtInput.Text = "Type a message..."; _txtInput.ForeColor = Color.Gray; } };
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
        }

        private void TxtInput_TextChanged(object sender, EventArgs e)
        {
            int lineCount = 1;
            if (_txtInput.Text != "Type a message...")
            {
                lineCount = _txtInput.GetLineFromCharIndex(Math.Max(0, _txtInput.TextLength - 1)) + 1;
            }

            int lineHeight = _txtInput.Font.Height;
            if (lineCount > 4)
            {
                _txtInput.ScrollBars = ScrollBars.Vertical;
                lineCount = 4;
            }
            else
            {
                _txtInput.ScrollBars = ScrollBars.None;
            }

            // 40 is padding + margins inside pnlBottom and pnlInputWrap
            int newBottomHeight = (lineCount * lineHeight) + 40;
            if (pnlBottom.Height != newBottomHeight)
            {
                pnlBottom.Height = newBottomHeight;
            }
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

            int textX = avatarX + avatarSize + 15;
            RectangleF rectText = new RectangleF(textX, e.Bounds.Top + 15, e.Bounds.Width - textX - 5, 25);
            using (Font fName = new Font("Segoe UI", 12, FontStyle.Regular))
            using (StringFormat sf = new StringFormat() { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                g.DrawString(nombre, fName, Brushes.Black, rectText, sf);
            }
            
            RectangleF rectStatus = new RectangleF(textX, e.Bounds.Top + 38, e.Bounds.Width - textX - 5, 20);
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
            }
        }

        private void LstContactos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lstContactos.SelectedItem is ContactoItem ci)
            {
                _contactoSeleccionado = ci.Nombre;
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
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _enlace.GestorArchivos.EnviarArchivo(ofd.FileName, _contactoSeleccionado);
                    AgregarBurbuja($"Archivo: {Path.GetFileName(ofd.FileName)}", DateTime.Now.ToString("HH:mm"), true, _contactoSeleccionado, true, ofd.FileName);
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
            Invoke((MethodInvoker)(() => {
                var (esGrupo, idGrupo, remitenteReal) = ParsearRemitente(remitente);
                if (esGrupo)
                {
                    string claveGrupo = "GRUPO:" + idGrupo;
                    AgregarBurbuja(m, DateTime.Now.ToString("HH:mm"), false, claveGrupo, false, null, remitenteReal);
                }
                else
                {
                    AgregarBurbuja(m, DateTime.Now.ToString("HH:mm"), false, remitente, false);
                }
            }));
        }

        private void Enlace_NombreUsuarioRecibido(string status)
        {
            if (status == "ERR_ALIAS_EN_USO")
            {
                Invoke((MethodInvoker)(() => {
                    MessageBox.Show("El alias elegido ya está en uso. Por favor, elige otro.", "Nombre duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _btnConectar.Enabled = true;
                    _txtNombre.Enabled = true;
                    _txtNombre.Focus();
                }));
            }
            else if (status == "ERR_ALIAS_INVALIDO")
            {
                Invoke((MethodInvoker)(() => {
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
            Invoke((MethodInvoker)(() => {
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
                string msg = $"Recibiendo {nombreFinal} ({tamaño} bytes)...";

                var (esGrupo, idGrupo, remitenteReal) = ParsearRemitente(remitente);
                if (esGrupo)
                {
                    string claveGrupo = "GRUPO:" + idGrupo;
                    AgregarBurbuja(msg, DateTime.Now.ToString("HH:mm"), false, claveGrupo, true, rutaLocal, remitenteReal);
                }
                else
                {
                    AgregarBurbuja(msg, DateTime.Now.ToString("HH:mm"), false, remitente, true, rutaLocal);
                }
                
                _enlace.GestorArchivos.AceptarArchivoEntrante(slot, rutaLocal);
            }));
        }

        private void Enlace_ProgresoArchivo(int slot, double prog)
        {
        }

        private void Enlace_TransferenciaCompletada(int slot, string nombre)
        {
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

        private void AgregarBurbuja(string texto, string hora, bool esEnviado, string contactoAsociado, bool esArchivo, string? rutaArchivo = null, string? remitenteReal = null)
        {
            Panel p = new Panel { Width = _pnlChatHistory.ClientSize.Width - 40, Height = 60, Margin = new Padding(0,0,0,10) };
            
            if (esArchivo && !esEnviado && !string.IsNullOrEmpty(rutaArchivo))
            {
                p.Cursor = Cursors.Hand;
                p.Click += (s, e) => {
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.FileName = Path.GetFileName(rutaArchivo);
                        sfd.Title = "Guardar archivo recibido";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                if (File.Exists(rutaArchivo))
                                {
                                    File.Copy(rutaArchivo, sfd.FileName, true);
                                    MessageBox.Show("Archivo guardado con éxito.", "Descarga", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    MessageBox.Show("El archivo aún se está descargando o no se encuentra.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Error al guardar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                };
            }
            
            p.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int maxBurbujaWidth = p.Width - 80;
                
                Font fTxt = new Font("Segoe UI", 11);
                Font fHora = new Font("Segoe UI", 8);
                SizeF sizeTxt = e.Graphics.MeasureString(texto, fTxt, maxBurbujaWidth - 20);
                
                int nameHeight = 0;
                int nameWidth = 0;
                Font? fSender = null;
                Brush? brushSender = null;
                if (remitenteReal != null && !esEnviado)
                {
                    fSender = new Font("Segoe UI", 9, FontStyle.Bold);
                    brushSender = new SolidBrush(Color.FromArgb(30, 100, 180));
                    nameHeight = 18;
                    nameWidth = (int)e.Graphics.MeasureString(remitenteReal, fSender).Width + 40;
                }

                int burbujaW = Math.Max((int)sizeTxt.Width + 40, nameWidth);
                burbujaW = Math.Max(burbujaW, 70);

                int burbujaH = (int)sizeTxt.Height + 25 + nameHeight;
                p.Height = burbujaH + 10; 
                
                int x = esEnviado ? p.Width - burbujaW - 10 : 10;
                int y = 0;

                GraphicsPath path = new GraphicsPath();
                int rad = 10;
                path.AddArc(x, y, rad, rad, 180, 90);
                path.AddArc(x + burbujaW - rad, y, rad, rad, 270, 90);
                path.AddArc(x + burbujaW - rad, y + burbujaH - rad, rad, rad, 0, 90);
                path.AddArc(x, y + burbujaH - rad, rad, rad, 90, 90);
                path.CloseFigure();

                Color fill = esEnviado ? _colorBurbujaEnviado : _colorBurbujaRecibido;
                e.Graphics.FillPath(new SolidBrush(fill), path);
                
                // Cola de burbuja
                if (esEnviado) e.Graphics.FillPolygon(new SolidBrush(fill), new Point[] { new Point(x + burbujaW - 5, y), new Point(x + burbujaW + 10, y), new Point(x + burbujaW - 5, y + 15) });
                else e.Graphics.FillPolygon(new SolidBrush(fill), new Point[] { new Point(x + 5, y), new Point(x - 10, y), new Point(x + 5, y + 15) });

                if (remitenteReal != null && !esEnviado && fSender != null && brushSender != null)
                {
                    e.Graphics.DrawString(remitenteReal, fSender, brushSender, x + 10, y + 5);
                }

                e.Graphics.DrawString(texto, fTxt, Brushes.Black, new RectangleF(x + 10, y + 5 + nameHeight, burbujaW - 20, burbujaH - 25 - nameHeight));
                
                string horaStr = hora + (esEnviado ? " ✔" : "");
                e.Graphics.DrawString(horaStr, fHora, Brushes.Gray, x + burbujaW - 50, y + burbujaH - 20);
                
                if (esArchivo)
                {
                    e.Graphics.DrawString("📎", new Font("Segoe UI", 16), Brushes.DimGray, x + 5, y + 5 + nameHeight);
                }

                fTxt.Dispose();
                fHora.Dispose();
                fSender?.Dispose();
                brushSender?.Dispose();
            };

            if (!_historialChats.ContainsKey(contactoAsociado)) _historialChats[contactoAsociado] = new List<Control>();
            _historialChats[contactoAsociado].Add(p);

            if (contactoAsociado == _contactoSeleccionado)
            {
                _pnlChatHistory.Controls.Add(p);
                _pnlChatHistory.ScrollControlIntoView(p);
            }
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
            Invoke((MethodInvoker)(() => {
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
    }
}
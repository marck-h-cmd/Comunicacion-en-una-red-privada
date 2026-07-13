using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace winProyComunicacion
{
    public partial class ServerForm : Form
    {
        private ServidorBackend _backend;

        // Colores
        private readonly Color _colorFondoLista = Color.White;
        private readonly Color _colorHeader = Color.FromArgb(0, 128, 105);
        private readonly Color _colorBordeAbajo = Color.FromArgb(230, 230, 230);
        private readonly Color _colorVerdeOnline = Color.FromArgb(37, 211, 102);

        public ServerForm()
        {
            InitializeComponent();
            _lstClientes.DrawItem += LstClientes_DrawItem;
        }

        private void btnNuevoCliente_Click(object sender, EventArgs e)
        {
            new ClientForm("").Show();
        }

        private void LstClientes_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            string rawItem = _lstClientes.Items[e.Index].ToString();
            var partes = rawItem.Split('|');
            string nombre = partes[0];
            string detalle = partes.Length > 1 ? partes[1] : "";
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            e.DrawBackground();
            using (SolidBrush b = new SolidBrush(e.State.HasFlag(DrawItemState.Selected) ? Color.FromArgb(240, 240, 240) : _colorFondoLista))
            {
                g.FillRectangle(b, e.Bounds);
            }

            int avatarSize = 50;
            int avatarX = e.Bounds.Left + 15;
            int avatarY = e.Bounds.Top + 10;
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
            int dotX = avatarX + avatarSize - dotSize - 2;
            int dotY = avatarY + avatarSize - dotSize - 2;
            using (SolidBrush dotBrush = new SolidBrush(_colorVerdeOnline))
            {
                g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
                g.DrawEllipse(Pens.White, dotX, dotY, dotSize, dotSize); 
            }

            int textX = avatarX + avatarSize + 15;
            using (Font fName = new Font("Segoe UI", 12, FontStyle.Regular))
            {
                g.DrawString(nombre, fName, Brushes.Black, textX, e.Bounds.Top + 15);
                if (!string.IsNullOrEmpty(detalle))
                {
                    SizeF nameSize = g.MeasureString(nombre, fName);
                    using (Font fDetalle = new Font("Segoe UI", 8, FontStyle.Regular))
                    {
                        g.DrawString(detalle, fDetalle, Brushes.Gray, textX + nameSize.Width + 5, e.Bounds.Top + 20);
                    }
                }
            }
            using (Font fStatus = new Font("Segoe UI", 9, FontStyle.Regular))
            {
                g.DrawString("online", fStatus, Brushes.Gray, textX, e.Bounds.Top + 38);
            }

            using (Pen pGreen = new Pen(Color.FromArgb(100, 200, 100), 2))
            {
                g.DrawArc(pGreen, e.Bounds.Right - 30, e.Bounds.Top + 25, 12, 12, 120, 300);
                g.DrawLine(pGreen, e.Bounds.Right - 24, e.Bounds.Top + 22, e.Bounds.Right - 24, e.Bounds.Top + 28);
            }

            using (Pen sepPen = new Pen(_colorBordeAbajo))
            {
                g.DrawLine(sepPen, textX, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _backend = new ServidorBackend();
            _backend.LogEvent += msg => Invoke((MethodInvoker)(() => {
                _lstLogs.Items.Add(msg);
                _lstLogs.TopIndex = _lstLogs.Items.Count - 1;
            }));
            _backend.ListaClientesActualizada += clientes => Invoke((MethodInvoker)(() => {
                _lstClientes.Items.Clear();
                foreach(var c in clientes) _lstClientes.Items.Add(c); 
            }));
            
            try
            {
                await _backend.IniciarServidorAsync(8000);
            }
            catch (System.Net.Sockets.SocketException)
            {
                // Si el puerto ya está en uso, significa que ya hay un Servidor corriendo en esta PC.
                // Cerramos silenciosamente esta ventana y dejamos que la app continúe solo como Cliente.
                this.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _backend?.DetenerServidor();
            base.OnFormClosing(e);
        }
    }
}

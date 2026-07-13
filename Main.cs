using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;

namespace winProyComunicacion
{
    public partial class Main : Form
    {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public Main()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 242, 245);
            
            BuildUI();
        }

        private void BuildUI()
        {
            // Title bar
            Panel pnlTitle = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(0, 128, 105) // WhatsApp teal
            };
            
            pnlTitle.MouseDown += (s, e) => { dragging = true; dragCursorPoint = Cursor.Position; dragFormPoint = this.Location; };
            pnlTitle.MouseMove += (s, e) => { if (dragging) { Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint)); this.Location = Point.Add(dragFormPoint, new Size(dif)); } };
            pnlTitle.MouseUp += (s, e) => dragging = false;

            Label lblTitle = new Label
            {
                Text = "TCP IP NETWORKING LAUNCHER",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblTitle.MouseDown += (s, e) => { dragging = true; dragCursorPoint = Cursor.Position; dragFormPoint = this.Location; };
            lblTitle.MouseMove += (s, e) => { if (dragging) { Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint)); this.Location = Point.Add(dragFormPoint, new Size(dif)); } };
            lblTitle.MouseUp += (s, e) => dragging = false;

            // Minimize button
            Label lblMin = new Label
            {
                Text = "—",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(this.Width - 80, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            lblMin.MouseEnter += (s, e) => lblMin.BackColor = Color.FromArgb(0, 100, 85);
            lblMin.MouseLeave += (s, e) => lblMin.BackColor = Color.Transparent;

            // Close button
            Label lblClose = new Label
            {
                Text = "✕",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(40, 40),
                Location = new Point(this.Width - 40, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblClose.Click += (s, e) => Application.Exit();
            lblClose.MouseEnter += (s, e) => lblClose.BackColor = Color.FromArgb(0, 100, 85);
            lblClose.MouseLeave += (s, e) => lblClose.BackColor = Color.Transparent;

            pnlTitle.Controls.Add(lblMin);
            pnlTitle.Controls.Add(lblClose);
            pnlTitle.Controls.Add(lblTitle);
            
            // Subtitle
            Label lblSelect = new Label
            {
                Text = "SELECCIONE",
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Location = new Point(140, 60)
            };
            
            // Buttons
            NeumorphicButton btnServer = new NeumorphicButton("SERVIDOR", true)
            {
                Location = new Point(70, 110),
                Size = new Size(210, 210)
            };
            btnServer.Click += BtnServer_Click;

            NeumorphicButton btnClient = new NeumorphicButton("CLIENTE", false)
            {
                Location = new Point(320, 110),
                Size = new Size(210, 210)
            };
            btnClient.Click += BtnClient_Click;

            this.Controls.Add(btnServer);
            this.Controls.Add(btnClient);
            this.Controls.Add(lblSelect);
            this.Controls.Add(pnlTitle);
            
            pnlTitle.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.White, 2))
                {
                    e.Graphics.DrawEllipse(p, 10, 8, 22, 22);
                    e.Graphics.DrawLine(p, 10, 25, 6, 30);
                    e.Graphics.DrawLine(p, 6, 30, 14, 28);
                    e.Graphics.DrawArc(p, 15, 13, 12, 12, 135, 180);
                }
            };
        }

        private void BtnServer_Click(object sender, EventArgs e)
        {
            this.Hide();
            ServerForm f = new ServerForm();
            f.FormClosed += (s, args) => this.Close();
            f.Show();
        }

        private void BtnClient_Click(object sender, EventArgs e)
        {
            this.Hide();
            
            string nombreInicial = "";
            string rutaConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TatoText", "config.json");
            try {
                if (File.Exists(rutaConfig)) {
                    string json = File.ReadAllText(rutaConfig);
                    using (var doc = System.Text.Json.JsonDocument.Parse(json)) {
                        if (doc.RootElement.TryGetProperty("NombreUsuario", out var prop)) {
                            nombreInicial = prop.GetString() ?? "";
                        }
                    }
                }
            } catch { }

            ClientForm f = new ClientForm(nombreInicial);
            f.FormClosed += (s, args) => this.Close();
            f.Show();
        }
    }

    public class NeumorphicButton : Control
    {
        private string _text;
        private bool _isServer;
        private bool _hover;
        private bool _pressed;

        public NeumorphicButton(string text, bool isServer)
        {
            _text = text;
            _isServer = isServer;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            int radius = 30;
            Rectangle r = new Rectangle(15, 15, this.Width - 30, this.Height - 30);
            
            if (_pressed)
            {
                DrawRoundedRect(g, new SolidBrush(Color.FromArgb(235, 238, 240)), r, radius);
            }
            else
            {
                for(int i = 1; i <= 8; i++)
                {
                    int alphaD = 25 - (i * 3);
                    int alphaL = 40 - (i * 5);
                    if (alphaD < 0) alphaD = 0;
                    if (alphaL < 0) alphaL = 0;
                    
                    DrawRoundedRect(g, new SolidBrush(Color.FromArgb(alphaD, Color.Black)), 
                                    new Rectangle(r.X + i + (_hover ? 2 : 0), r.Y + i + (_hover ? 2 : 0), r.Width, r.Height), radius);
                    DrawRoundedRect(g, new SolidBrush(Color.FromArgb(alphaL, Color.White)), 
                                    new Rectangle(r.X - i - (_hover ? 1 : 0), r.Y - i - (_hover ? 1 : 0), r.Width, r.Height), radius);
                }
                
                DrawRoundedRect(g, new SolidBrush(Color.FromArgb(240, 242, 245)), r, radius);
            }

            int avX = this.Width / 2 - 40;
            int avY = 40;
            g.FillEllipse(new SolidBrush(Color.FromArgb(170, 175, 180)), avX, avY, 80, 80);
            
            g.FillEllipse(Brushes.White, avX + 25, avY + 15, 30, 30);
            using(GraphicsPath p = new GraphicsPath()) {
                p.AddArc(avX + 10, avY + 45, 60, 60, 180, 180);
                g.FillPath(Brushes.White, p);
            }

            g.FillEllipse(new SolidBrush(Color.FromArgb(85, 185, 75)), avX + 60, avY + 60, 18, 18);
            g.DrawEllipse(new Pen(Color.FromArgb(240, 242, 245), 3), avX + 60, avY + 60, 18, 18);

            using (Pen greenPen = new Pen(Color.FromArgb(100, 180, 75), 3))
            {
                int iconX = avX + 90;
                int iconY = avY + 35;
                if (_isServer)
                {
                    g.DrawArc(greenPen, iconX, iconY, 14, 14, -60, 300);
                    g.DrawLine(greenPen, iconX + 7, iconY - 2, iconX + 7, iconY + 7);
                }
                else
                {
                    using (Pen p2 = new Pen(Color.FromArgb(40, 60, 60), 3))
                    {
                        g.DrawArc(p2, iconX, iconY, 10, 10, 45, 180);
                        g.DrawArc(p2, iconX + 5, iconY + 5, 10, 10, -135, 180);
                    }
                }
            }

            using (Font f = new Font("Segoe UI", 16, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(_text, f);
                g.DrawString(_text, f, new SolidBrush(Color.FromArgb(10, 25, 30)), (this.Width - size.Width) / 2, 145);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius, radius, 180, 90);
            path.AddArc(r.X + r.Width - radius, r.Y, radius, radius, 270, 90);
            path.AddArc(r.X + r.Width - radius, r.Y + r.Height - radius, radius, radius, 0, 90);
            path.AddArc(r.X, r.Y + r.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DrawRoundedRect(Graphics g, Brush b, Rectangle r, int radius)
        {
            g.FillPath(b, GetRoundedPath(r, radius));
        }
    }
}

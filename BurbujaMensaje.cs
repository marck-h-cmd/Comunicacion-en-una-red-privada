// BurbujaMensaje.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace winProyComunicacion
{
    public class BurbujaMensaje : Control
    {
        public bool EsMio { get; set; }
        public string Remitente { get; set; }
        public string Texto { get; set; }
        public string Hora { get; set; }
        public bool EsArchivo { get; set; }
        public double Progreso { get; set; } = -1;
        public EstadoTransferencia EstadoArchivo { get; set; } = EstadoTransferencia.Esperando;
        public int IndiceArchivo { get; set; } = -1;
        public string RutaArchivo { get; set; } = string.Empty;

        private readonly Color ColorTx = Color.FromArgb(220, 248, 198);
        private readonly Color ColorRx = Color.White;
        private readonly Color ColorBordeTx = Color.FromArgb(190, 230, 160);
        private readonly Color ColorBordeRx = Color.FromArgb(210, 210, 210);
        private readonly Color ColorTexto = Color.FromArgb(30, 30, 30);
        private readonly Color ColorHora = Color.FromArgb(110, 110, 110);

        private Button btnCancelar;
        private Button btnReenviar;

        public event Action<int> BotonCancelarClick;
        public event Action<int> BotonReenviarClick;
        public event Action<string> ArchivoClickeado;

        public BurbujaMensaje()
        {
            this.ResizeRedraw = true;
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);
            // Fondo transparente: el área fuera de la burbuja dibujada
            // muestra el color del wrapper (= fondo del formulario), sin
            // ningún artífacto gris.
            BackColor = Color.Transparent;
            Margin = new Padding(0);

            // ── Responsive: la burbuja se extiende con el panel contenedor ──
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            // Inicializar botones planos modernos circulares
            btnCancelar = new Button
            {
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnCancelar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool isHover = btnCancelar.ClientRectangle.Contains(btnCancelar.PointToClient(Cursor.Position));
                Color circleColor = isHover ? Color.FromArgb(7, 94, 84) : Color.FromArgb(18, 140, 126);
                using (var brush = new SolidBrush(circleColor))
                {
                    g.FillEllipse(brush, 0, 0, 28, 28);
                }
                using (var pen = new Pen(Color.White, 2f))
                {
                    // Draw a crisp cross ✕
                    g.DrawLine(pen, 9, 9, 19, 19);
                    g.DrawLine(pen, 19, 9, 9, 19);
                }
            };
            btnCancelar.MouseEnter += (s, e) => btnCancelar.Invalidate();
            btnCancelar.MouseLeave += (s, e) => btnCancelar.Invalidate();
            btnCancelar.Click += (s, e) => BotonCancelarClick?.Invoke(IndiceArchivo);

            btnReenviar = new Button
            {
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnReenviar.FlatAppearance.BorderSize = 0;
            btnReenviar.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnReenviar.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnReenviar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool isHover = btnReenviar.ClientRectangle.Contains(btnReenviar.PointToClient(Cursor.Position));
                Color circleColor = isHover ? Color.FromArgb(7, 94, 84) : Color.FromArgb(18, 140, 126);
                using (var brush = new SolidBrush(circleColor))
                {
                    g.FillEllipse(brush, 0, 0, 28, 28);
                }
                using (var brush = new SolidBrush(Color.White))
                {
                    // Draw play icon ▶
                    var pts = new Point[] { new Point(11, 8), new Point(11, 20), new Point(20, 14) };
                    g.FillPolygon(brush, pts);
                }
            };
            btnReenviar.MouseEnter += (s, e) => btnReenviar.Invalidate();
            btnReenviar.MouseLeave += (s, e) => btnReenviar.Invalidate();
            btnReenviar.Click += (s, e) => BotonReenviarClick?.Invoke(IndiceArchivo);

            Controls.Add(btnCancelar);
            Controls.Add(btnReenviar);

            // Agregar evento de clic para abrir archivos
            this.Click += (s, e) =>
            {
                if (EsArchivo && EstadoArchivo == EstadoTransferencia.Completado && !string.IsNullOrEmpty(RutaArchivo))
                {
                    ArchivoClickeado?.Invoke(RutaArchivo);
                }
            };

            // Cambiar el cursor cuando el archivo está completo
            this.MouseMove += (s, e) =>
            {
                if (EsArchivo && EstadoArchivo == EstadoTransferencia.Completado && !string.IsNullOrEmpty(RutaArchivo))
                    this.Cursor = Cursors.Hand;
                else
                    this.Cursor = Cursors.Default;
            };
        }

        // ── Auto-ajuste de altura al cambiar el ancho ──────────────────────
        // Cuando el panel padre cambia de tamaño, Anchor=L|R actualiza Width;
        // OnResize recalcula la altura para que la burbuja no se recorte.
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 0)
            {
                int nuevaAltura = CalcularAltura(Width) + 8;
                if (Height != nuevaAltura)
                    Height = nuevaAltura;
            }
        }

        private enum TipoIcono
        {
            Documento, Pdf, Hoja, Presentacion, Imagen,
            Video, Audio, Comprimido, Codigo, Ejecutable, Generico
        }

        private TipoIcono ClasificarExtension(string ext) => ext.ToLower() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => TipoIcono.Imagen,
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".wmv" => TipoIcono.Video,
            ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" => TipoIcono.Audio,
            ".pdf" => TipoIcono.Pdf,
            ".doc" or ".docx" or ".odt" or ".txt" or ".rtf" => TipoIcono.Documento,
            ".xls" or ".xlsx" or ".csv" or ".ods" => TipoIcono.Hoja,
            ".ppt" or ".pptx" or ".odp" => TipoIcono.Presentacion,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => TipoIcono.Comprimido,
            ".cs" or ".py" or ".js" or ".ts" or ".cpp" or ".java" => TipoIcono.Codigo,
            ".exe" or ".msi" or ".bat" or ".sh" => TipoIcono.Ejecutable,
            _ => TipoIcono.Generico
        };

        private Color ColorDeTipo(TipoIcono tipo) => tipo switch
        {
            TipoIcono.Imagen => Color.FromArgb(210, 140, 20),
            TipoIcono.Video => Color.FromArgb(120, 40, 180),
            TipoIcono.Audio => Color.FromArgb(20, 160, 180),
            TipoIcono.Pdf => Color.FromArgb(210, 55, 45),
            TipoIcono.Documento => Color.FromArgb(40, 90, 200),
            TipoIcono.Hoja => Color.FromArgb(30, 145, 75),
            TipoIcono.Presentacion => Color.FromArgb(210, 90, 30),
            TipoIcono.Comprimido => Color.FromArgb(140, 95, 35),
            TipoIcono.Codigo => Color.FromArgb(80, 55, 200),
            TipoIcono.Ejecutable => Color.FromArgb(60, 60, 60),
            _ => Color.FromArgb(120, 120, 120)
        };

        public int CalcularAltura(int anchoPanel)
        {
            int bubbleWidth = Math.Min(500, (int)(anchoPanel * 0.72));
            int w = bubbleWidth - 28;
            using var g = CreateGraphics();
            var fRemit = new Font("Segoe UI", 8f, FontStyle.Bold);
            var fTexto = new Font("Segoe UI", 10f);
            int hRem = (int)g.MeasureString(Remitente ?? " ", fRemit, w).Height;
            int hNombre = EsArchivo ? 44 : 0;  // icono(36) + nombre(20) combinados
            string textoMedir = EsArchivo
                ? (Texto ?? "").Split('\n').Length > 1 ? (Texto ?? "").Split('\n')[1] : " "
                : (string.IsNullOrEmpty(Texto) ? " " : Texto);
            int hTxt = (int)g.MeasureString(textoMedir, fTexto, w).Height;
            int hBarra = (Progreso >= 0) ? 20 : 0;
            int hBotones = (EsArchivo && (EstadoArchivo == EstadoTransferencia.Enviando || EstadoArchivo == EstadoTransferencia.Recibiendo || EstadoArchivo == EstadoTransferencia.Cancelado)) ? 34 : 0;
            return hRem + hNombre + hTxt + hBarra + hBotones + 36;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(this.Parent?.BackColor ?? Color.FromArgb(253, 246, 236));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int ancho = Math.Min(500, (int)(Width * 0.72));
            int x = EsMio ? Width - ancho - 14 : 14;

            var fTexto = new Font("Segoe UI", 10f);
            var fRemit = new Font("Segoe UI", 8f, FontStyle.Bold);
            var fHora = new Font("Segoe UI", 7.5f);
            var fPct = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            var fNombre = new Font("Segoe UI", 9f);

            int wTxt = ancho - 24;
            var szRem = g.MeasureString(Remitente ?? " ", fRemit, wTxt);

            string nombreArch = "", tamañoArch = "";
            TipoIcono tipo = TipoIcono.Generico;
            if (EsArchivo)
            {
                var partes = (Texto ?? "").Split('\n');
                nombreArch = partes.Length > 0 ? partes[0] : "";
                tamañoArch = partes.Length > 1 ? partes[1] : "";
                tipo = ClasificarExtension(System.IO.Path.GetExtension(nombreArch));
            }

            string textoMensaje = EsArchivo ? tamañoArch
                                            : (Texto ?? "");
            var szTxt = g.MeasureString(string.IsNullOrEmpty(textoMensaje) ? " " : textoMensaje,
                                        fTexto, wTxt);
            int hNombre = EsArchivo ? 44 : 0;
            int hBarra = (Progreso >= 0) ? 20 : 0;
            bool mostrarBotones = EsArchivo && (EstadoArchivo == EstadoTransferencia.Enviando || EstadoArchivo == EstadoTransferencia.Recibiendo || EstadoArchivo == EstadoTransferencia.Cancelado);
            int hBotones = mostrarBotones ? 34 : 0;
            int altura = (int)(szRem.Height + szTxt.Height) + hNombre + hBarra + hBotones + 28;

            // ── Fondo burbuja ──────────────────────────────────────────────
            Color fondoColor = EsMio ? ColorTx : ColorRx;
            Color bordeColor = EsMio ? ColorBordeTx : ColorBordeRx;
            using var path = RoundedRect(x, 4, ancho, altura, 12, !EsMio, EsMio);
            g.FillPath(new SolidBrush(fondoColor), path);
            g.DrawPath(new Pen(bordeColor, 1f), path);
            DibujarCola(g, x, ancho, fondoColor, bordeColor);

            int tx = x + 12;
            int ty = 10;

            // ── Remitente ──────────────────────────────────────────────────
            g.DrawString(Remitente, fRemit,
                EsMio ? new SolidBrush(Color.FromArgb(0, 130, 80))
                      : new SolidBrush(Color.FromArgb(30, 100, 180)),
                new RectangleF(tx, ty, wTxt, szRem.Height));
            ty += (int)szRem.Height + 4;

            // ── Icono + nombre de archivo ──────────────────────────────────
            if (EsArchivo)
            {
                Color colorIco = ColorDeTipo(tipo);
                int icoSize = 36;

                // Cuadrado redondeado de fondo del icono
                var rectIco = new Rectangle(tx, ty, icoSize, icoSize);
                using var pathIco = RoundedRectSimple(rectIco, 7);
                g.FillPath(new SolidBrush(colorIco), pathIco);

                // Figura interior según tipo
                DibujarFiguraIcono(g, tipo, tx, ty, icoSize);

                // Nombre del archivo a la derecha del icono
                int nombreX = tx + icoSize + 8;
                int nombreW = wTxt - icoSize - 8;
                g.DrawString(nombreArch, fNombre, new SolidBrush(ColorTexto),
                    new RectangleF(nombreX, ty + 2, nombreW, icoSize));

                ty += icoSize + 4;

                // Tamaño
                g.DrawString(tamañoArch, fTexto,
                    new SolidBrush(Color.FromArgb(90, 90, 90)),
                    new RectangleF(tx, ty, wTxt, szTxt.Height));
                ty += (int)szTxt.Height + 4;   // ← reducido de 8 a 4
            }
            else
            {
                g.DrawString(Texto, fTexto, new SolidBrush(ColorTexto),
                    new RectangleF(tx, ty, wTxt, szTxt.Height));
                ty += (int)szTxt.Height + 8;
            }

            // ── Barra de progreso ──────────────────────────────────────────
            if (Progreso >= 0)
            {
                int pctAncho = 46;
                int barraAncho = wTxt - pctAncho - 6;
                int barraAlto = 4;
                int barraY = ty + 6;        // ← pegado al tamaño

                using (var trackBrush = new SolidBrush(Color.FromArgb(224, 224, 224)))
                {
                    g.FillRectangle(trackBrush, tx, barraY, barraAncho, barraAlto);
                }

                int fill = (int)(barraAncho * Math.Min(1.0, Math.Max(0.0, Progreso) / 100.0));
                if (fill > 0)
                {
                    using (var fillBrush = new SolidBrush(Color.FromArgb(18, 140, 126))) // WhatsApp green #128C7E
                    {
                        g.FillRectangle(fillBrush, tx, barraY, fill, barraAlto);
                    }
                }

                g.DrawString(((int)Progreso) + "%", fPct,
                    new SolidBrush(Color.FromArgb(90, 90, 90)),
                    tx + barraAncho + 6, barraY - 6);

                ty += 16;
            }

            // ── Botones ────────────────────────────────────────────────────
            if (mostrarBotones)
            {
                int btnX = x + ancho - 34; // 28 ancho + 6 padding
                int btnY = ty + 2;

                btnCancelar.Location = new Point(btnX, btnY);
                btnReenviar.Location = new Point(btnX, btnY);

                btnCancelar.Visible = (EstadoArchivo == EstadoTransferencia.Enviando || EstadoArchivo == EstadoTransferencia.Recibiendo);
                btnReenviar.Visible = (EstadoArchivo == EstadoTransferencia.Cancelado);

                ty += 30;
            }
            else
            {
                btnCancelar.Visible = false;
                btnReenviar.Visible = false;
            }

            // ── Hora y Checks ──────────────────────────────────────────────
            if (EsMio)
            {
                string checkStr = "\u2713\u2713";
                var szHora = g.MeasureString(Hora, fHora);
                var szCheck = g.MeasureString(checkStr, fHora);
                
                float hx = x + ancho - szHora.Width - szCheck.Width - 12;
                g.DrawString(Hora, fHora, new SolidBrush(ColorHora), hx, ty);
                g.DrawString(checkStr, fHora, new SolidBrush(Color.FromArgb(52, 183, 241)), hx + szHora.Width + 2, ty); // WhatsApp blue #34B7F1
            }
            else
            {
                g.DrawString(Hora, fHora, new SolidBrush(ColorHora), tx, ty);
            }
        }

        // ── Figuras GDI+ por tipo ──────────────────────────────────────────
        private void DibujarFiguraIcono(Graphics g, TipoIcono tipo, int x, int y, int s)
        {
            int m = 7; // margen interno
            var p = new Pen(Color.White, 1.8f);
            var b = new SolidBrush(Color.White);
            int cx = x + s / 2, cy = y + s / 2;

            switch (tipo)
            {
                case TipoIcono.Imagen:
                    // Marco de cámara / foto
                    g.DrawRectangle(p, x + m, y + m + 2, s - m * 2, s - m * 2 - 2);
                    // Círculo = lente
                    g.DrawEllipse(p, cx - 5, cy - 3, 10, 10);
                    // Montaña dentro
                    g.DrawLine(p, x + m + 1, y + s - m - 1,
                                  x + m + (s - m * 2) / 3, cy - 1);
                    g.DrawLine(p, x + m + (s - m * 2) / 3, cy - 1,
                                  x + m + (int)((s - m * 2) * 0.62), cy + 4);
                    g.DrawLine(p, x + m + (int)((s - m * 2) * 0.62), cy + 4,
                                  x + s - m - 1, y + s - m - 1);
                    break;

                case TipoIcono.Video:
                    // Triángulo play centrado
                    var play = new Point[]
                    {
                        new Point(cx - 6, cy - 8),
                        new Point(cx - 6, cy + 8),
                        new Point(cx + 8, cy)
                    };
                    g.FillPolygon(b, play);
                    // Círculo exterior
                    g.DrawEllipse(p, x + m, y + m, s - m * 2, s - m * 2);
                    break;

                case TipoIcono.Audio:
                    // Nota musical
                    g.FillEllipse(b, cx - 7, cy + 2, 8, 6);   // cabeza nota
                    g.DrawLine(new Pen(Color.White, 2f), cx + 1, cy + 5, cx + 1, cy - 7);
                    g.DrawLine(new Pen(Color.White, 2f), cx + 1, cy - 7, cx + 8, cy - 4);
                    g.DrawLine(new Pen(Color.White, 2f), cx + 8, cy - 4, cx + 8, cy + 1);
                    break;

                case TipoIcono.Pdf:
                    // Letras "P D F" simplificadas → solo líneas horizontales gruesas
                    var pPdf = new Pen(Color.White, 2f);
                    g.DrawLine(pPdf, x + m, y + m + 2, x + s - m, y + m + 2);
                    g.DrawLine(pPdf, x + m, y + m + 8, x + s - m, y + m + 8);
                    g.DrawLine(pPdf, x + m, y + m + 14, x + s - m - 4, y + m + 14);
                    g.DrawLine(pPdf, x + m, y + m + 20, x + s - m, y + m + 20);
                    break;

                case TipoIcono.Documento:
                    // Hoja con líneas de texto
                    g.DrawRectangle(p, x + m, y + m, s - m * 2, s - m * 2);
                    g.DrawLine(p, x + m + 3, y + m + 6, x + s - m - 3, y + m + 6);
                    g.DrawLine(p, x + m + 3, y + m + 11, x + s - m - 3, y + m + 11);
                    g.DrawLine(p, x + m + 3, y + m + 16, x + s - m - 3, y + m + 16);
                    g.DrawLine(p, x + m + 3, y + m + 21, x + s - m - 8, y + m + 21);
                    break;

                case TipoIcono.Hoja:
                    // Cuadrícula tipo spreadsheet
                    g.DrawRectangle(p, x + m, y + m, s - m * 2, s - m * 2);
                    int col = x + m + (s - m * 2) / 2;
                    int fil = y + m + (s - m * 2) / 3;
                    g.DrawLine(p, col, y + m, col, y + s - m);
                    g.DrawLine(p, x + m, fil, x + s - m, fil);
                    g.DrawLine(p, x + m, fil * 2 - y + 2, x + s - m, fil * 2 - y + 2);
                    break;

                case TipoIcono.Presentacion:
                    // Pantalla con triángulo play
                    g.DrawRectangle(p, x + m, y + m, s - m * 2, s - m * 2 - 4);
                    var pPlay = new Point[]
                    {
                        new Point(cx - 4, cy - 5),
                        new Point(cx - 4, cy + 5),
                        new Point(cx + 6, cy)
                    };
                    g.FillPolygon(b, pPlay);
                    break;

                case TipoIcono.Comprimido:
                    // Caja con cremallera (líneas alternas)
                    g.DrawRectangle(p, x + m, y + m, s - m * 2, s - m * 2);
                    for (int i = 0; i < 4; i++)
                    {
                        int yy = y + m + 4 + i * 6;
                        if (i % 2 == 0)
                            g.FillRectangle(b, cx - 3, yy, 6, 4);
                        else
                            g.DrawRectangle(p, cx - 3, yy, 6, 4);
                    }
                    break;

                case TipoIcono.Codigo:
                    // Brackets < >
                    var pCod = new Pen(Color.White, 2f);
                    g.DrawLine(pCod, cx - 3, cy - 7, cx - 9, cy);
                    g.DrawLine(pCod, cx - 9, cy, cx - 3, cy + 7);
                    g.DrawLine(pCod, cx + 3, cy - 7, cx + 9, cy);
                    g.DrawLine(pCod, cx + 9, cy, cx + 3, cy + 7);
                    break;

                case TipoIcono.Ejecutable:
                    // Engranaje simplificado → círculo + dientes
                    g.DrawEllipse(p, cx - 5, cy - 5, 10, 10);
                    for (int i = 0; i < 4; i++)
                    {
                        double ang = i * Math.PI / 2;
                        int dx = (int)(Math.Cos(ang) * 9);
                        int dy = (int)(Math.Sin(ang) * 9);
                        g.DrawLine(new Pen(Color.White, 2.5f),
                            cx + (int)(Math.Cos(ang) * 5), cy + (int)(Math.Sin(ang) * 5),
                            cx + dx, cy + dy);
                    }
                    break;

                default:
                    // Hoja genérica con doblez
                    var pGe = new Point[]
                    {
                        new Point(x + m,      y + m),
                        new Point(x + s-m-6,  y + m),
                        new Point(x + s-m,    y + m+6),
                        new Point(x + s-m,    y + s-m),
                        new Point(x + m,      y + s-m),
                    };
                    g.DrawPolygon(p, pGe);
                    g.DrawLine(p, x + s - m - 6, y + m, x + s - m - 6, y + m + 6);
                    g.DrawLine(p, x + s - m - 6, y + m + 6, x + s - m, y + m + 6);
                    break;
            }
        }

        private void DibujarCola(Graphics g, int x, int ancho, Color fondo, Color borde)
        {
            Point[] pts = EsMio
                ? new[] { new Point(x + ancho - 1, 4),
                           new Point(x + ancho + 8, 4),
                           new Point(x + ancho - 1, 14) }
                : new[] { new Point(x + 1, 4),
                           new Point(x - 8, 4),
                           new Point(x + 1, 14) };

            // Fill tail polygon
            using (var brush = new SolidBrush(fondo))
            {
                g.FillPolygon(brush, pts);
            }

            // Draw outer border lines only (no inner vertical boundary)
            using (var pen = new Pen(borde, 1f))
            {
                if (EsMio)
                {
                    g.DrawLine(pen, x + ancho - 1, 4, x + ancho + 8, 4);
                    g.DrawLine(pen, x + ancho + 8, 4, x + ancho - 1, 14);
                }
                else
                {
                    g.DrawLine(pen, x + 1, 4, x - 8, 4);
                    g.DrawLine(pen, x - 8, 4, x + 1, 14);
                }
            }
        }

        private GraphicsPath RoundedRect(int x, int y, int w, int h, int r,
                                          bool esquinaIzqArriba, bool esquinaDerArriba)
        {
            var path = new GraphicsPath();
            if (esquinaIzqArriba) path.AddLine(x, y, x + r, y);
            else path.AddArc(x, y, r * 2, r * 2, 180, 90);
            if (esquinaDerArriba) path.AddLine(x + w - r, y, x + w, y);
            else path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private GraphicsPath RoundedRectSimple(Rectangle r, int radio)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radio * 2, radio * 2, 180, 90);
            path.AddArc(r.Right - radio * 2, r.Y, radio * 2, radio * 2, 270, 90);
            path.AddArc(r.Right - radio * 2, r.Bottom - radio * 2, radio * 2, radio * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radio * 2, radio * 2, radio * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
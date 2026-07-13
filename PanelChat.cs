// PanelChat.cs
// Panel personalizado que habilita scroll vertical pero NUNCA muestra
// la barra de scroll horizontal, sin importar el ancho de sus hijos.
// Esto evita el scroll horizontal cuando las burbujas están ajustadas
// mediante Anchor=Left|Right y su Width == ClientSize.Width del panel.

using System.Windows.Forms;

namespace winProyComunicacion
{
    /// <summary>
    /// Panel con scroll vertical automático que suprime el scrollbar horizontal.
    /// Reemplaza al <see cref="Panel"/> estándar en pnlChat.
    /// </summary>
    internal class PanelChat : Panel
    {
        public PanelChat()
        {
            AutoScroll = true;
        }

        protected override System.Drawing.Point ScrollToControl(Control activeControl)
        {
            // Devolver el punto de desplazamiento actual para evitar que el panel
            // haga scroll automático innecesario al ganar foco un control hijo.
            return DisplayRectangle.Location;
        }

        protected override void WndProc(ref Message m)
        {
            // WM_HSCROLL = 0x0114  → ignorar todos los mensajes de scroll horizontal
            if (m.Msg == 0x0114) return;
            base.WndProc(ref m);
        }

        // Forzar que AutoScrollMinSize.Width siempre sea 0
        // para que WinForms nunca active la barra horizontal.
        protected override void OnSizeChanged(System.EventArgs e)
        {
            base.OnSizeChanged(e);
            if (AutoScrollMinSize.Width != 0)
                AutoScrollMinSize = new System.Drawing.Size(0, AutoScrollMinSize.Height);
        }
    }
}

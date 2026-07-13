using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace winProyComunicacion
{
    public class FormCrearGrupo : Form
    {
        private TextBox _txtNombreGrupo;
        private CheckedListBox _clbMiembros;
        private Button _btnCrear;
        private Button _btnCancelar;

        public string NombreGrupo { get; private set; } = string.Empty;
        public List<string> MiembrosSeleccionados { get; private set; } = new List<string>();

        public FormCrearGrupo(List<string> contactosDisponibles)
        {
            this.Text = "Crear Nuevo Grupo";
            this.Size = new Size(360, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 240);

            // 1. Panel de Encabezado (Teal/Emerald green)
            Panel pnlHeader = new Panel
            {
                BackColor = Color.FromArgb(0, 128, 105),
                Dock = DockStyle.Top,
                Height = 65
            };

            Label lblHeaderTitle = new Label
            {
                Text = "NUEVO GRUPO",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 18),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderTitle);

            // 2. Etiqueta y Contenedor para Nombre del Grupo
            Label lblNombre = new Label
            {
                Text = "Nombre del Grupo:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                Location = new Point(20, 85),
                Size = new Size(150, 18)
            };

            Panel pnlNombreWrap = new Panel
            {
                BackColor = Color.White,
                Location = new Point(20, 105),
                Size = new Size(305, 32),
                Padding = new Padding(6, 6, 6, 6)
            };

            _txtNombreGrupo = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.Black
            };
            pnlNombreWrap.Controls.Add(_txtNombreGrupo);

            // 3. Etiqueta y CheckedListBox de Miembros
            Label lblMiembros = new Label
            {
                Text = "Seleccionar Miembros:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64),
                Location = new Point(20, 155),
                Size = new Size(180, 18)
            };

            // Contenedor para el CheckedListBox con borde sutil
            Panel pnlListWrap = new Panel
            {
                BackColor = Color.White,
                Location = new Point(20, 175),
                Size = new Size(305, 230),
                Padding = new Padding(1) // Border effect
            };
            pnlListWrap.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlListWrap.ClientRectangle, Color.LightGray, ButtonBorderStyle.Solid);
            };

            _clbMiembros = new CheckedListBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                CheckOnClick = true,
                BackColor = Color.White
            };
            pnlListWrap.Controls.Add(_clbMiembros);

            foreach (var contacto in contactosDisponibles)
            {
                _clbMiembros.Items.Add(contacto);
            }

            // 4. Botones
            _btnCrear = new Button
            {
                Text = "Crear",
                BackColor = Color.FromArgb(0, 128, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(130, 428),
                Size = new Size(90, 32),
                DialogResult = DialogResult.OK
            };
            _btnCrear.FlatAppearance.BorderSize = 0;

            _btnCancelar = new Button
            {
                Text = "Cancelar",
                BackColor = Color.White,
                ForeColor = Color.FromArgb(64, 64, 64),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Location = new Point(235, 428),
                Size = new Size(90, 32),
                DialogResult = DialogResult.Cancel
            };
            _btnCancelar.FlatAppearance.BorderColor = Color.LightGray;

            // Agregar todos los controles al formulario
            this.Controls.Add(pnlHeader);
            this.Controls.Add(lblNombre);
            this.Controls.Add(pnlNombreWrap);
            this.Controls.Add(lblMiembros);
            this.Controls.Add(pnlListWrap);
            this.Controls.Add(_btnCrear);
            this.Controls.Add(_btnCancelar);

            this.AcceptButton = _btnCrear;
            this.CancelButton = _btnCancelar;

            _btnCrear.Click += BtnCrear_Click;
        }

        private void BtnCrear_Click(object? sender, EventArgs e)
        {
            string nombre = _txtNombreGrupo.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("Por favor, ingrese el nombre del grupo.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None; // Prevent closing
                return;
            }

            if (nombre.Contains("|") || nombre.Contains(","))
            {
                MessageBox.Show("El nombre del grupo no puede contener caracteres como '|' o ','.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (_clbMiembros.CheckedItems.Count == 0)
            {
                MessageBox.Show("Debe seleccionar al menos un miembro para crear el grupo.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            NombreGrupo = nombre;
            foreach (var item in _clbMiembros.CheckedItems)
            {
                if (item != null)
                {
                    MiembrosSeleccionados.Add(item.ToString()!);
                }
            }
        }
    }
}

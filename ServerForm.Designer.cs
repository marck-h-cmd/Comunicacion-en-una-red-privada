namespace winProyComunicacion
{
    partial class ServerForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Button btnNuevoCliente;
        private System.Windows.Forms.ListBox _lstClientes;
        private System.Windows.Forms.ListBox _lstLogs;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnNuevoCliente = new System.Windows.Forms.Button();
            this._lstClientes = new System.Windows.Forms.ListBox();
            this._lstLogs = new System.Windows.Forms.ListBox();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(128)))), ((int)(((byte)(105)))));
            this.pnlHeader.Controls.Add(this.btnNuevoCliente);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(350, 60);
            this.pnlHeader.TabIndex = 0;
            // 
            // btnNuevoCliente
            // 
            this.btnNuevoCliente.Location = new System.Drawing.Point(230, 18);
            this.btnNuevoCliente.Name = "btnNuevoCliente";
            this.btnNuevoCliente.Size = new System.Drawing.Size(100, 25);
            this.btnNuevoCliente.TabIndex = 1;
            this.btnNuevoCliente.Text = "Nuevo Cliente";
            this.btnNuevoCliente.UseVisualStyleBackColor = true;
            this.btnNuevoCliente.Click += new System.EventHandler(this.btnNuevoCliente_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(10, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(199, 21);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "TCP IP NETWORK SERVER";
            // 
            // _lstClientes
            // 
            this._lstClientes.BackColor = System.Drawing.Color.White;
            this._lstClientes.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._lstClientes.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lstClientes.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this._lstClientes.IntegralHeight = false;
            this._lstClientes.ItemHeight = 70;
            this._lstClientes.Location = new System.Drawing.Point(0, 60);
            this._lstClientes.Name = "_lstClientes";
            this._lstClientes.Size = new System.Drawing.Size(350, 440);
            this._lstClientes.TabIndex = 1;
            // 
            // _lstLogs
            // 
            this._lstLogs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._lstLogs.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._lstLogs.Font = new System.Drawing.Font("Consolas", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this._lstLogs.FormattingEnabled = true;
            this._lstLogs.ItemHeight = 13;
            this._lstLogs.Location = new System.Drawing.Point(0, 500);
            this._lstLogs.Name = "_lstLogs";
            this._lstLogs.Size = new System.Drawing.Size(350, 100);
            this._lstLogs.TabIndex = 2;
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(350, 600);
            this.Controls.Add(this._lstClientes);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this._lstLogs);
            this.Name = "ServerForm";
            this.Text = "TCP IP NETWORK SERVER";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
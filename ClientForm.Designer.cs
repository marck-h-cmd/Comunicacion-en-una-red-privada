namespace winProyComunicacion
{
    partial class ClientForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel _pnlLogin;
        private System.Windows.Forms.TextBox _txtIp;
        private System.Windows.Forms.TextBox _txtNombre;
        private System.Windows.Forms.Button _btnConectar;
        private System.Windows.Forms.Button _btnNuevoGrupo;
        private System.Windows.Forms.Label lblIpTitle;
        private System.Windows.Forms.Label lblNombreTitle;
        private System.Windows.Forms.Label lblLocalPort;
        
        private System.Windows.Forms.SplitContainer _split;
        private System.Windows.Forms.ListBox _lstContactos;
        
        private System.Windows.Forms.Panel pnlChatHeader;
        private System.Windows.Forms.Panel pnlAvatarHeader;
        private System.Windows.Forms.Label _lblHeaderChatNombre;
        private System.Windows.Forms.Label _lblHeaderChatStatus;
        
        private System.Windows.Forms.FlowLayoutPanel _pnlChatHistory;
        
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Panel pnlInputWrap;
        private System.Windows.Forms.TextBox _txtInput;
        private System.Windows.Forms.Button _btnFile;
        private System.Windows.Forms.Panel pnlSpacer;
        private System.Windows.Forms.Button _btnSend;

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
            _pnlLogin = new Panel();
            lblIpTitle = new Label();
            _txtIp = new TextBox();
            lblNombreTitle = new Label();
            _txtNombre = new TextBox();
            _btnConectar = new Button();
            _btnNuevoGrupo = new Button();
            lblLocalPort = new Label();
            _split = new SplitContainer();
            _lstContactos = new ListBox();
            _pnlChatHistory = new FlowLayoutPanel();
            pnlChatHeader = new Panel();
            pnlAvatarHeader = new Panel();
            _lblHeaderChatNombre = new Label();
            _lblHeaderChatStatus = new Label();
            pnlBottom = new Panel();
            pnlInputWrap = new Panel();
            _txtInput = new TextBox();
            _btnFile = new Button();
            pnlSpacer = new Panel();
            _btnSend = new Button();
            _pnlLogin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
            _split.Panel1.SuspendLayout();
            _split.Panel2.SuspendLayout();
            _split.SuspendLayout();
            pnlChatHeader.SuspendLayout();
            pnlBottom.SuspendLayout();
            pnlInputWrap.SuspendLayout();
            SuspendLayout();
            // 
            // _pnlLogin
            // 
            _pnlLogin.BackColor = Color.FromArgb(0, 128, 105);
            _pnlLogin.Controls.Add(lblIpTitle);
            _pnlLogin.Controls.Add(_txtIp);
            _pnlLogin.Controls.Add(lblNombreTitle);
            _pnlLogin.Controls.Add(_txtNombre);
            _pnlLogin.Controls.Add(_btnConectar);
            _pnlLogin.Controls.Add(_btnNuevoGrupo);
            _pnlLogin.Controls.Add(lblLocalPort);
            _pnlLogin.Dock = DockStyle.Top;
            _pnlLogin.Location = new Point(0, 0);
            _pnlLogin.Name = "_pnlLogin";
            _pnlLogin.Size = new Size(900, 50);
            _pnlLogin.TabIndex = 1;
            // 
            // lblIpTitle
            // 
            lblIpTitle.AutoSize = true;
            lblIpTitle.ForeColor = Color.White;
            lblIpTitle.Location = new Point(10, 18);
            lblIpTitle.Name = "lblIpTitle";
            lblIpTitle.Size = new Size(20, 15);
            lblIpTitle.TabIndex = 0;
            lblIpTitle.Text = "IP:";
            // 
            // _txtIp
            // 
            _txtIp.Location = new Point(40, 15);
            _txtIp.Name = "_txtIp";
            _txtIp.Size = new Size(100, 23);
            _txtIp.TabIndex = 1;
            _txtIp.Text = "127.0.0.1";
            // 
            // lblNombreTitle
            // 
            lblNombreTitle.AutoSize = true;
            lblNombreTitle.ForeColor = Color.White;
            lblNombreTitle.Location = new Point(150, 18);
            lblNombreTitle.Name = "lblNombreTitle";
            lblNombreTitle.Size = new Size(54, 15);
            lblNombreTitle.TabIndex = 2;
            lblNombreTitle.Text = "Nombre:";
            // 
            // _txtNombre
            // 
            _txtNombre.Location = new Point(210, 15);
            _txtNombre.Name = "_txtNombre";
            _txtNombre.Size = new Size(100, 23);
            _txtNombre.TabIndex = 3;
            // 
            // _btnConectar
            // 
            _btnConectar.BackColor = Color.White;
            _btnConectar.FlatStyle = FlatStyle.Flat;
            _btnConectar.Location = new Point(320, 13);
            _btnConectar.Name = "_btnConectar";
            _btnConectar.Size = new Size(75, 23);
            _btnConectar.TabIndex = 4;
            _btnConectar.Text = "Conectar";
            _btnConectar.UseVisualStyleBackColor = false;
            // 
            // _btnNuevoGrupo
            // 
            _btnNuevoGrupo.BackColor = Color.White;
            _btnNuevoGrupo.Enabled = false;
            _btnNuevoGrupo.FlatStyle = FlatStyle.Flat;
            _btnNuevoGrupo.Location = new Point(410, 13);
            _btnNuevoGrupo.Name = "_btnNuevoGrupo";
            _btnNuevoGrupo.Size = new Size(100, 23);
            _btnNuevoGrupo.TabIndex = 6;
            _btnNuevoGrupo.Text = "Nuevo Grupo";
            _btnNuevoGrupo.UseVisualStyleBackColor = false;
            // 
            // lblLocalPort
            // 
            lblLocalPort.AutoSize = true;
            lblLocalPort.ForeColor = Color.White;
            lblLocalPort.Location = new Point(530, 18);
            lblLocalPort.Name = "lblLocalPort";
            lblLocalPort.Size = new Size(0, 15);
            lblLocalPort.TabIndex = 5;
            // 
            // _split
            // 
            _split.BackColor = Color.LightGray;
            _split.Dock = DockStyle.Fill;
            _split.Location = new Point(0, 50);
            _split.Name = "_split";
            // 
            // _split.Panel1
            // 
            _split.Panel1.Controls.Add(_lstContactos);
            // 
            // _split.Panel2
            // 
            _split.Panel2.Controls.Add(_pnlChatHistory);
            _split.Panel2.Controls.Add(pnlChatHeader);
            _split.Panel2.Controls.Add(pnlBottom);
            _split.Size = new Size(900, 600);
            _split.SplitterDistance = 250;
            _split.SplitterWidth = 1;
            _split.TabIndex = 0;
            // 
            // _lstContactos
            // 
            _lstContactos.BackColor = Color.White;
            _lstContactos.BorderStyle = BorderStyle.None;
            _lstContactos.Dock = DockStyle.Fill;
            _lstContactos.DrawMode = DrawMode.OwnerDrawFixed;
            _lstContactos.IntegralHeight = false;
            _lstContactos.ItemHeight = 70;
            _lstContactos.Location = new Point(0, 0);
            _lstContactos.Name = "_lstContactos";
            _lstContactos.Size = new Size(250, 600);
            _lstContactos.TabIndex = 0;
            // 
            // _pnlChatHistory
            // 
            _pnlChatHistory.AutoScroll = true;
            _pnlChatHistory.BackColor = Color.FromArgb(240, 240, 240);
            _pnlChatHistory.Dock = DockStyle.Fill;
            _pnlChatHistory.FlowDirection = FlowDirection.TopDown;
            _pnlChatHistory.Location = new Point(0, 60);
            _pnlChatHistory.Name = "_pnlChatHistory";
            _pnlChatHistory.Padding = new Padding(20);
            _pnlChatHistory.Size = new Size(649, 480);
            _pnlChatHistory.TabIndex = 0;
            _pnlChatHistory.WrapContents = false;
            // 
            // pnlChatHeader
            // 
            pnlChatHeader.BackColor = Color.FromArgb(0, 128, 105);
            pnlChatHeader.Controls.Add(pnlAvatarHeader);
            pnlChatHeader.Controls.Add(_lblHeaderChatNombre);
            pnlChatHeader.Controls.Add(_lblHeaderChatStatus);
            pnlChatHeader.Dock = DockStyle.Top;
            pnlChatHeader.Location = new Point(0, 0);
            pnlChatHeader.Name = "pnlChatHeader";
            pnlChatHeader.Size = new Size(649, 60);
            pnlChatHeader.TabIndex = 1;
            // 
            // pnlAvatarHeader
            // 
            pnlAvatarHeader.Location = new Point(10, 10);
            pnlAvatarHeader.Name = "pnlAvatarHeader";
            pnlAvatarHeader.Size = new Size(40, 40);
            pnlAvatarHeader.TabIndex = 0;
            // 
            // _lblHeaderChatNombre
            // 
            _lblHeaderChatNombre.AutoSize = true;
            _lblHeaderChatNombre.Font = new Font("Segoe UI", 12F);
            _lblHeaderChatNombre.ForeColor = Color.White;
            _lblHeaderChatNombre.Location = new Point(60, 12);
            _lblHeaderChatNombre.Name = "_lblHeaderChatNombre";
            _lblHeaderChatNombre.Size = new Size(212, 21);
            _lblHeaderChatNombre.TabIndex = 1;
            _lblHeaderChatNombre.Text = "SELECCIONA UN CONTACTO";
            // 
            // _lblHeaderChatStatus
            // 
            _lblHeaderChatStatus.AutoSize = true;
            _lblHeaderChatStatus.Font = new Font("Segoe UI", 9F);
            _lblHeaderChatStatus.ForeColor = Color.FromArgb(220, 220, 220);
            _lblHeaderChatStatus.Location = new Point(60, 35);
            _lblHeaderChatStatus.Name = "_lblHeaderChatStatus";
            _lblHeaderChatStatus.Size = new Size(0, 15);
            _lblHeaderChatStatus.TabIndex = 2;
            // 
            // pnlBottom
            // 
            pnlBottom.BackColor = Color.FromArgb(240, 240, 240);
            pnlBottom.Controls.Add(pnlInputWrap);
            pnlBottom.Controls.Add(pnlSpacer);
            pnlBottom.Controls.Add(_btnSend);
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Location = new Point(0, 540);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Padding = new Padding(15);
            pnlBottom.Size = new Size(649, 60);
            pnlBottom.TabIndex = 2;
            // 
            // pnlInputWrap
            // 
            pnlInputWrap.BackColor = Color.White;
            pnlInputWrap.Controls.Add(_txtInput);
            pnlInputWrap.Controls.Add(_btnFile);
            pnlInputWrap.Dock = DockStyle.Fill;
            pnlInputWrap.Location = new Point(15, 15);
            pnlInputWrap.Name = "pnlInputWrap";
            pnlInputWrap.Padding = new Padding(5);
            pnlInputWrap.Size = new Size(559, 30);
            pnlInputWrap.TabIndex = 0;
            // 
            // _txtInput
            // 
            _txtInput.BorderStyle = BorderStyle.None;
            _txtInput.Dock = DockStyle.Fill;
            _txtInput.Font = new Font("Segoe UI", 12F);
            _txtInput.ForeColor = Color.Gray;
            _txtInput.Location = new Point(45, 5);
            _txtInput.Name = "_txtInput";
            _txtInput.Size = new Size(509, 22);
            _txtInput.TabIndex = 0;
            _txtInput.Text = "Type a message...";
            // 
            // _btnFile
            // 
            _btnFile.BackColor = Color.White;
            _btnFile.Dock = DockStyle.Left;
            _btnFile.FlatAppearance.BorderSize = 0;
            _btnFile.FlatStyle = FlatStyle.Flat;
            _btnFile.Font = new Font("Segoe UI", 14F);
            _btnFile.ForeColor = Color.Gray;
            _btnFile.Location = new Point(5, 5);
            _btnFile.Name = "_btnFile";
            _btnFile.Size = new Size(40, 20);
            _btnFile.TabIndex = 1;
            _btnFile.Text = "📎";
            _btnFile.UseVisualStyleBackColor = false;
            // 
            // pnlSpacer
            // 
            pnlSpacer.Dock = DockStyle.Right;
            pnlSpacer.Location = new Point(574, 15);
            pnlSpacer.Name = "pnlSpacer";
            pnlSpacer.Size = new Size(10, 30);
            pnlSpacer.TabIndex = 1;
            // 
            // _btnSend
            // 
            _btnSend.BackColor = Color.FromArgb(0, 128, 105);
            _btnSend.Cursor = Cursors.Hand;
            _btnSend.Dock = DockStyle.Right;
            _btnSend.FlatAppearance.BorderSize = 0;
            _btnSend.FlatStyle = FlatStyle.Flat;
            _btnSend.Font = new Font("Segoe UI", 16F);
            _btnSend.ForeColor = Color.White;
            _btnSend.Location = new Point(584, 15);
            _btnSend.Name = "_btnSend";
            _btnSend.Size = new Size(50, 30);
            _btnSend.TabIndex = 2;
            _btnSend.Text = "➤";
            _btnSend.UseVisualStyleBackColor = false;
            // 
            // ClientForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(240, 240, 240);
            ClientSize = new Size(900, 650);
            Controls.Add(_split);
            Controls.Add(_pnlLogin);
            MinimumSize = new Size(600, 400);
            Name = "ClientForm";
            Text = "TCP IP CHAT CLIENT";
            _pnlLogin.ResumeLayout(false);
            _pnlLogin.PerformLayout();
            _split.Panel1.ResumeLayout(false);
            _split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_split).EndInit();
            _split.ResumeLayout(false);
            pnlChatHeader.ResumeLayout(false);
            pnlChatHeader.PerformLayout();
            pnlBottom.ResumeLayout(false);
            pnlInputWrap.ResumeLayout(false);
            pnlInputWrap.PerformLayout();
            ResumeLayout(false);
        }
    }
}

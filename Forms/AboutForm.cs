using System;
using System.Drawing;
using System.Windows.Forms;

namespace ExcelReplacement.Forms
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponents();
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeComponents()
        {
            this.Text = "About";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(400, 200);

            var label = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Document Template Processor\nVersion 1.0",
                AutoSize = false
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom,
                Width = 80,
                Height = 30,
                Top = 150,
                Left = (this.ClientSize.Width - 80) / 2
            };

            this.Controls.Add(label);
            this.Controls.Add(okButton);
        }
    }
}

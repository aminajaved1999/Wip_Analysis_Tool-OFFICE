using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WIPAT.DAL;
using WIPAT.Entities;
using WIPAT.Helpers;

namespace WIPAT
{
    public partial class LoginForm : Form
    {
        #region Fields
        private readonly string usernamePlaceholder = "Username";
        private readonly string passwordPlaceholder = "Password";
        private readonly UserRepository _userRepository;

        public User AuthenticatedUser { get; private set; }

        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;
        #endregion

        #region Constructor & Initialization
        public LoginForm(UserRepository userRepository)
        {
            InitializeComponent();
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

            ApplyTheme();

            SetPlaceholder(usernameTextBox, usernamePlaceholder);
            SetPlaceholder(passwordTextBox, passwordPlaceholder, true);
        }

        private void ApplyTheme()
        {
            UITheme.SetFormIcon(this);
            this.BackColor = UITheme.SurfaceWhite;

            if (leftPanel != null)
            {
                leftPanel.BackColor = UITheme.SurfaceWhite;
            }

            if (signInButton != null)
            {
                UITheme.StyleButton(signInButton, AppButtonStyle.SignIn);
            }

            if (closeButton != null)
            {
                UITheme.StyleButton(closeButton, AppButtonStyle.SignIn);
            }
        }
        #endregion

        #region Drop Shadow
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }
        #endregion

        #region Dynamic UI Painting
        private void LeftPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color userLineColor = usernameTextBox.Focused ? UITheme.Upload_Color : UITheme.GridBorder;
            Color passLineColor = passwordTextBox.Focused ? UITheme.Upload_Color : UITheme.GridBorder;

            using (Pen userPen = new Pen(userLineColor, usernameTextBox.Focused ? 2 : 1))
            using (Pen passPen = new Pen(passLineColor, passwordTextBox.Focused ? 2 : 1))
            {
                e.Graphics.DrawLine(userPen, usernameTextBox.Left, usernameTextBox.Bottom + 4, usernameTextBox.Right, usernameTextBox.Bottom + 4);
                e.Graphics.DrawLine(passPen, passwordTextBox.Left, passwordTextBox.Bottom + 4, passwordTextBox.Right, passwordTextBox.Bottom + 4);
            }
        }
        #endregion

        #region Sign-In Flow
        private void signInButton_Click(object sender, EventArgs e)
        {
            try
            {
                string username = usernameTextBox.Text == usernamePlaceholder ? "" : usernameTextBox.Text.Trim();
                string password = passwordTextBox.Text == passwordPlaceholder ? "" : passwordTextBox.Text;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please enter both username and password.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var response = _userRepository.ValidateUser(username, password);

                if (response.Success)
                {
                    this.AuthenticatedUser = response.Data;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"{response.Message}", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    passwordTextBox.Text = "";
                    passwordTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while signing in: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        #endregion

        #region Placeholder Helpers
        private void SetPlaceholder(TextBox tb, string placeholder, bool isPassword = false)
        {
            tb.Text = placeholder;
            tb.ForeColor = UITheme.TextSecondaryColor;
            if (isPassword) tb.PasswordChar = '\0';
        }

        private void RemovePlaceholderText(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            if ((tb == usernameTextBox && tb.Text == usernamePlaceholder) ||
                (tb == passwordTextBox && tb.Text == passwordPlaceholder))
            {
                tb.Text = "";
                tb.ForeColor = UITheme.GridRowText;
                if (tb == passwordTextBox) tb.PasswordChar = '●';
            }

            leftPanel.Invalidate();
        }

        private void AddPlaceholderText(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                if (tb == usernameTextBox) SetPlaceholder(tb, usernamePlaceholder);
                else if (tb == passwordTextBox) SetPlaceholder(tb, passwordPlaceholder, true);
            }

            leftPanel.Invalidate();
        }
        #endregion

        #region Window Dragging
        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                dragCursorPoint = Cursor.Position;
                dragFormPoint = this.Location;
            }
        }

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(diff));
            }
        }

        private void Form_MouseUp(object sender, MouseEventArgs e) => dragging = false;
        #endregion
    }
}
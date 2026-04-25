using System;
using System.Drawing;
using System.Windows.Forms;
using WIPAT.DAL;
using WIPAT.Entities;

namespace WIPAT
{
    public partial class LoginForm : Form
    {
        #region Fields

        private readonly string usernamePlaceholder = "Username";
        private readonly string passwordPlaceholder = "Password";
        private readonly UserRepository _userRepository;
        // Program.cs will read this property to get the logged-in user
        public User AuthenticatedUser { get; private set; }

        #endregion

        #region Constructor & Initialization

        public LoginForm(UserRepository userRepository)
        {
            InitializeComponent();

            // Validate dependencies
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

            // Add outlines to panels
            if (this.leftPanel != null) AddOutlineToPanel(this, this.leftPanel);
            if (this.rightPanel != null) AddOutlineToPanel(this, this.rightPanel);

            // Set initial placeholder text
            SetPlaceholder(usernameTextBox, usernamePlaceholder);
            SetPlaceholder(passwordTextBox, passwordPlaceholder, isPassword: true);

            // Attach placeholder handlers
            usernameTextBox.GotFocus += RemovePlaceholderText;
            usernameTextBox.LostFocus += AddPlaceholderText;

            passwordTextBox.GotFocus += RemovePlaceholderText;
            passwordTextBox.LostFocus += AddPlaceholderText;

            // Smooth painting
            this.DoubleBuffered = true;

            // Set "Enter" key to trigger Sign In
            this.AcceptButton = signInButton;

            // Drag handlers
            this.MouseDown += Form_MouseDown;
            this.MouseMove += Form_MouseMove;
            this.MouseUp += Form_MouseUp;
        }

        #endregion

        #region Sign-In Flow

        private void signInButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Ignore placeholder text when checking input
                string username = usernameTextBox.Text == usernamePlaceholder ? "" : usernameTextBox.Text.Trim();
                string password = passwordTextBox.Text == passwordPlaceholder ? "" : passwordTextBox.Text;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please enter both username and password.", "Input Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validate against DB
                var response = _userRepository.ValidateUser(username, password);

                if (response.Success)
                {
                    // 1. Store the successful user object
                    this.AuthenticatedUser = response.Data;

                    // 2. Set the result to OK so Program.cs knows to proceed
                    this.DialogResult = DialogResult.OK;

                    // 3. Close this form (Program.cs will resume execution)
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"{response.Message}", "Login Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // Clear password field for retry
                    passwordTextBox.Text = "";
                    passwordTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception occurred: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            // Set result to Cancel so Program.cs exits the app
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        #endregion

        #region Placeholder Helpers

        private void SetPlaceholder(TextBox tb, string placeholder, bool isPassword = false)
        {
            tb.Text = placeholder;
            tb.ForeColor = Color.Gray;
            if (isPassword)
            {
                tb.PasswordChar = '\0'; // show text for placeholder
            }
        }

        private void RemovePlaceholderText(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            if ((tb == usernameTextBox && tb.Text == usernamePlaceholder) ||
                (tb == passwordTextBox && tb.Text == passwordPlaceholder))
            {
                tb.Text = "";
                tb.ForeColor = Color.Black;
                if (tb == passwordTextBox)
                {
                    tb.PasswordChar = '●'; // mask password
                }
            }
        }

        private void AddPlaceholderText(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                if (tb == usernameTextBox)
                {
                    SetPlaceholder(tb, usernamePlaceholder);
                }
                else if (tb == passwordTextBox)
                {
                    SetPlaceholder(tb, passwordPlaceholder, isPassword: true);
                }
            }
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

        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        #endregion

        #region UI Helpers

        private void AddOutlineToPanel(Form form, Panel panel)
        {
            // Ensure that the Paint event for the panel is properly handled
            panel.Paint += (sender, e) =>
            {
                using (Pen borderPen = new Pen(Color.Gray, 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };
        }

        #endregion

        #region Keyboard Handlers

        // Since we set this.AcceptButton = signInButton, these specific handlers 
        // are technically redundant for the "Enter" key, but good to keep if you add other logic later.
        private void usernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                signInButton.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void passwordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                signInButton.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        #endregion
    }
}
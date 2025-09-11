using System;
using System.Drawing;
using System.Windows.Forms;
using WIPAT.BLL;
using WIPAT.DAL;

namespace WIPAT
{
    public partial class LoginForm : Form
    {
        private readonly string usernamePlaceholder = "Username";
        private readonly string passwordPlaceholder = "Password";
        private AuthManager authManager;
        private UserRepository userRepository;

        public LoginForm()
        {
            InitializeComponent();
            authManager = new AuthManager();
            userRepository = new UserRepository();

            // Set initial placeholder text
            SetPlaceholder(usernameTextBox, usernamePlaceholder);
            SetPlaceholder(passwordTextBox, passwordPlaceholder, isPassword: true);

            // Attach event handlers for username
            usernameTextBox.GotFocus += RemovePlaceholderText;
            usernameTextBox.LostFocus += AddPlaceholderText;

            // Attach event handlers for password
            passwordTextBox.GotFocus += RemovePlaceholderText;
            passwordTextBox.LostFocus += AddPlaceholderText;


        }

        private void SetPlaceholder(TextBox tb, string placeholder, bool isPassword = false)
        {
            tb.Text = placeholder;
            tb.ForeColor = Color.Gray;
            if (isPassword)
            {
                tb.PasswordChar = '\0';  // show text for placeholder
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

        private void signInButton_Click(object sender, EventArgs e)
        {
            // Ignore placeholder text when checking input
            string username = usernameTextBox.Text == usernamePlaceholder ? "" : usernameTextBox.Text.Trim();
            string password = passwordTextBox.Text == passwordPlaceholder ? "" : passwordTextBox.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var response = userRepository.ValidateUser(username, password);

            if (response.Success)
            {
                MainForm mainForm = new MainForm();
                mainForm.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show($"{response.Message}", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                passwordTextBox.Clear();
                passwordTextBox.Focus();
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Drag event handlers
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

    }

}

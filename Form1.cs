using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ChatApplication
{
    public partial class Form1 : Form
    {
        #region 1. الثوابت والمتغيرات العامة (Fields & Variables)

        // نص الاتصال بقاعدة البيانات المحلية
        private readonly string con = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\CahtApplication.mdf;Integrated Security=True";

        // ألوان الواجهة للأزرار والتبويبات
        private readonly Color colorActive = Color.SpringGreen;
        private readonly Color colorInactive = Color.FromArgb(137, 142, 140);

        // متغير حفظ إيميل المستخدم الناجح في تسجيل الدخول
        private string loggedInEmail = "";

        // عداد شريط التحميل الدائري
        private int splashCounter = 0;

        #endregion

        #region 2. مشيد الكلاس وبدء التشغيل (Constructor & Load)

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // فتح تبويب تسجيل الدخول تلقائياً كواجهة افتراضية
            buttonLogin.PerformClick();
        }

        #endregion

        #region 3. إدارة التبويبات والتنقل (UI Navigation)

        // التبديل إلى تبويب تسجيل الدخول
        private void buttonLogin_Click(object sender, EventArgs e)
        {
            panel1.BringToFront();

            buttonLogin.FillColor = colorInactive;
            buttonRegister.FillColor = colorActive;

            panel3.BackColor = colorActive;
            panel4.BackColor = colorActive;
        }

        // التبديل إلى تبويب إنشاء حساب جديد
        private void buttonRegister_Click(object sender, EventArgs e)
        {
            panel2.BringToFront();

            buttonRegister.FillColor = colorInactive;
            buttonLogin.FillColor = colorActive;

            panel3.BackColor = colorActive;
            panel4.BackColor = colorActive;
        }

        #endregion

        #region 4. منطق تسجيل الدخول (Login Logic)

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // 1. التحقق من صحة المدخلات
            if (string.IsNullOrWhiteSpace(txtemailLogin.Text.Trim()) ||
                string.IsNullOrWhiteSpace(txtpasswordLogin.Text.Trim()))
            {
                MessageBox.Show("يرجى ملء جميع الحقول المطلوبة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. التحقق من الحساب في قاعدة البيانات
            string query = "SELECT firstname FROM Login_tbl WHERE email = @email AND password = @password";

            try
            {
                using (SqlConnection connection = new SqlConnection(con))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@email", txtemailLogin.Text.Trim());
                        command.Parameters.AddWithValue("@password", txtpasswordLogin.Text.Trim());

                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                loggedInEmail = txtemailLogin.Text.Trim();
                                ClearLoginInputs();

                                // إظهار شاشة الانتقال وبدء شريط التقدم
                                panel5.BringToFront();
                                timer1.Start();
                            }
                            else
                            {
                                MessageBox.Show("البريد الإلكتروني أو كلمة المرور غير صحيحة.", "فشل الدخول", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "حدث خطأ في قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // شريط التحميل الدائري قبل الانتقال لنافذة الشات الرئيسية
        private void timer1_Tick(object sender, EventArgs e)
        {
            splashCounter++;
            guna2CircleProgressBar1.Value = splashCounter;

            if (guna2CircleProgressBar1.Value >= 20)
            {
                timer1.Stop();
                splashCounter = 0;
                guna2CircleProgressBar1.Value = 0;

                ChatForm chat = new ChatForm();
                chat.emailname = loggedInEmail;

                // إغلاق التطبيق بالكامل عند إغلاق واجهة الشات
                chat.FormClosed += (s, args) => this.Close();

                this.Hide();
                chat.Show();
            }
        }

        #endregion

        #region 5. منطق تسجيل حساب جديد (Registration Logic)

        private void btnRegisteration_Click(object sender, EventArgs e)
        {
            // 1. التحقق من اكتمال الخانات
            if (string.IsNullOrWhiteSpace(txtFirstName.Text.Trim()) ||
                string.IsNullOrWhiteSpace(txtLastName.Text.Trim()) ||
                string.IsNullOrWhiteSpace(txtEmail.Text.Trim()) ||
                string.IsNullOrWhiteSpace(txtPassword.Text.Trim()) ||
                string.IsNullOrWhiteSpace(txtConfirm.Text.Trim()))
            {
                MessageBox.Show("يرجى ملء جميع الحقول المطلوبة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. التحقق من الصورة الشخصية
            if (guna2CirclePictureBox1.Image == null)
            {
                MessageBox.Show("يرجى اختيار صورة شخصية للحساب.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. التحقق من تطابق كلمتي المرور
            if (txtPassword.Text.Trim() != txtConfirm.Text.Trim())
            {
                MessageBox.Show("كلمة المرور وتأكيدها غير متطابقين.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 4. تحويل الصورة الشخصية إلى بايتات
            byte[] imageBytes = ConvertImageToByteArray(guna2CirclePictureBox1.Image);

            // 5. حفظ البيانات في جدول المستخدمين
            string query = "INSERT INTO Login_tbl (firstname, lastname, email, password, confirmpass, image) " +
                           "VALUES (@firstname, @lastname, @email, @password, @confirmpass, @image)";

            try
            {
                using (SqlConnection connection = new SqlConnection(con))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@firstname", txtFirstName.Text.Trim());
                        command.Parameters.AddWithValue("@lastname", txtLastName.Text.Trim());
                        command.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                        command.Parameters.AddWithValue("@password", txtPassword.Text.Trim());
                        command.Parameters.AddWithValue("@confirmpass", txtConfirm.Text.Trim());
                        command.Parameters.AddWithValue("@image", imageBytes);

                        command.ExecuteNonQuery();

                        MessageBox.Show("تم التسجيل بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearRegisterInputs();

                        // العودة لتبويب تسجيل الدخول تلقائياً
                        buttonLogin.PerformClick();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "حدث خطأ في قاعدة البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // اختيار الصورة الشخصية
        private void guna2CirclePictureBox1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "اختر صورة شخصية";
                ofd.Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.jfif";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    guna2CirclePictureBox1.Image = new Bitmap(ofd.FileName);
                }
            }
        }

        #endregion

        #region 6. قيود الإدخال (Input Validation & KeyPress)

        // تقييد كلمة المرور للأرقام فقط في واجهة الدخول
        private void txtpasswordLogin_KeyPress(object sender, KeyPressEventArgs e)
        {
            RestrictToDigitsOnly(e);
        }

        // تقييد كلمة المرور للأرقام فقط في واجهة التسجيل
        private void txtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            RestrictToDigitsOnly(e);
        }

        // تقييد تأكيد كلمة المرور للأرقام فقط في واجهة التسجيل
        private void txtConfirm_KeyPress(object sender, KeyPressEventArgs e)
        {
            RestrictToDigitsOnly(e);
        }

        #endregion

        #region 7. دوال مساعدة عامة قابلة لإعادة الاستخدام (Helper Methods)

        // دالة حصر الإدخال بالأرقام وأزرار التحكم
        private void RestrictToDigitsOnly(KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        // دالة آمنة لتحويل أي صورة إلى مصفوفة بايتات لتخزينها في SQL
        private byte[] ConvertImageToByteArray(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (Bitmap bmp = new Bitmap(image))
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                }
                return ms.ToArray();
            }
        }

        // تفريغ حقول واجهة إنشاء الحساب
        public void ClearRegisterInputs()
        {
            txtFirstName.Clear();
            txtLastName.Clear();
            txtEmail.Clear();
            txtPassword.Clear();
            txtConfirm.Clear();
            guna2CirclePictureBox1.Image = null;
        }

        // تفريغ حقول واجهة تسجيل الدخول
        public void ClearLoginInputs()
        {
            txtemailLogin.Clear();
            txtpasswordLogin.Clear();
        }

        #endregion
    }
}
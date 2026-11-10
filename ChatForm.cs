using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ChatApplication
{
    public partial class ChatForm : Form
    {
        #region 1. الثوابت والمتغيرات العامة (Variables & Settings)

        // نص الاتصال بقاعدة البيانات المحلية
        private readonly string con = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\CahtApplication.mdf;Integrated Security=True";

        // البريد الإلكتروني للمستخدم المسجل حالياً في هذا الفورم
        public string emailname { get; set; }

        // البريد الإلكتروني للطرف الآخر المحدد للمحادثة حالياً
        private string activeChatUserEmail = "";

        // حالات القوائم المتحركة (مفتوحة / مغلقة)
        private bool isSidebarCollapsed = true;
        private bool isPanel6Collapsed = true;

        #endregion

        #region 2. مشيد الكلاس وبدء التشغيل (Constructor & Load)

        public ChatForm()
        {
            InitializeComponent();

            // تفعيل التخزين المؤقت المزدوج لمنع وميض الشاشة أثناء الحركة
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {
            // 1. إخفاء مساحة الإرسال حتى يتم اختيار شخص من القائمة
            ResetChatArea();

            // 2. جلب وتعبئة بيانات الحساب الحالي (الملف الشخصي)
            LoadCurrentUserProfile();

            // 3. تحميل جهات الاتصال المسجلة في القائمة الجانبية
            LoadContacts();
        }

        #endregion

        #region 3. الحساب الشخصي: الجلب والتعديل (Profile Management)

        // جلب بيانات الحساب الحالي من قاعدة البيانات
        private void LoadCurrentUserProfile()
        {
            string query = "SELECT firstname, lastname, email, password, image FROM Login_tbl WHERE email = @email";

            try
            {
                using (SqlConnection connection = new SqlConnection(con))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@email", emailname);
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string firstName = reader["firstname"].ToString();
                                string lastName = reader["lastname"].ToString();
                                string email = reader["email"].ToString();
                                string password = reader["password"].ToString();

                                // عرض البيانات في الواجهة
                                label2.Text = email;
                                txtFirstName.Text = firstName;
                                txtLastName.Text = lastName;
                                txtEmail.Text = email;
                                txtPassword.Text = password;

                                // تعبئة حقول التعديل
                                guna2TextBox3.Text = firstName;
                                guna2TextBox2.Text = lastName;
                                guna2TextBox1.Text = email;

                                // قراءة الصورة الشخصية
                                if (reader["image"] != DBNull.Value)
                                {
                                    byte[] imgData = (byte[])reader["image"];
                                    Image userImg = ConvertByteArrayToImage(imgData);

                                    if (userImg != null)
                                    {
                                        guna2CirclePictureBox1.Image = userImg;
                                        guna2CirclePictureBox2.Image = userImg;
                                        guna2CirclePictureBox3.Image = userImg;
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show("لم يتم العثور على بيانات الحساب.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في جلب البيانات", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // اختيار صورة شخصية جديدة من الجهاز
        private void guna2CirclePictureBox3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "اختر صورة شخصية";
                ofd.Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.jfif";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    guna2CirclePictureBox3.Image = new Bitmap(ofd.FileName);
                }
            }
        }

        // حفظ تعديلات البيانات الشخصية (الاسم، الإيميل، الصورة)
        private void guna2Button8_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(guna2TextBox3.Text.Trim()) ||
                string.IsNullOrWhiteSpace(guna2TextBox2.Text.Trim()) ||
                string.IsNullOrWhiteSpace(guna2TextBox1.Text.Trim()))
            {
                MessageBox.Show("يرجى ملء كافة الحقول قبل الحفظ.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            byte[] imageBytes = ConvertImageToByteArray(guna2CirclePictureBox3.Image);

            string query = "UPDATE Login_tbl SET firstname = @firstname, lastname = @lastname, email = @newEmail, image = @image WHERE email = @oldEmail";

            try
            {
                using (SqlConnection connection = new SqlConnection(con))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@firstname", guna2TextBox3.Text.Trim());
                        command.Parameters.AddWithValue("@lastname", guna2TextBox2.Text.Trim());
                        command.Parameters.AddWithValue("@newEmail", guna2TextBox1.Text.Trim());
                        command.Parameters.AddWithValue("@oldEmail", emailname);

                        if (imageBytes != null)
                            command.Parameters.AddWithValue("@image", imageBytes);
                        else
                            command.Parameters.AddWithValue("@image", DBNull.Value);

                        command.ExecuteNonQuery();

                        MessageBox.Show("تم تحديث البيانات بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // تحديث المتغيرات والنصوص في الفورم
                        emailname = guna2TextBox1.Text.Trim();
                        label2.Text = emailname;
                        txtFirstName.Text = guna2TextBox3.Text.Trim();
                        txtLastName.Text = guna2TextBox2.Text.Trim();
                        txtEmail.Text = emailname;

                        if (guna2CirclePictureBox3.Image != null)
                        {
                            guna2CirclePictureBox1.Image = guna2CirclePictureBox3.Image;
                            guna2CirclePictureBox2.Image = guna2CirclePictureBox3.Image;
                        }

                        panel7.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في التحديث", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // إعادة تحميل جهات الاتصال لتحديث الإيميل المستثنى
            LoadContacts();
        }

        // تغيير كلمة المرور
        private void btnSaveUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOldPassword.Text.Trim()) ||
                string.IsNullOrWhiteSpace(txtNewPassword.Text.Trim()) ||
                string.IsNullOrWhiteSpace(txtConfirmPassword.Text.Trim()))
            {
                MessageBox.Show("يرجى ملء كافة الحقول قبل الحفظ.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtOldPassword.Text.Trim() != txtPassword.Text.Trim())
            {
                MessageBox.Show("كلمة المرور الحالية غير صحيحة!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtNewPassword.Text.Trim() != txtConfirmPassword.Text.Trim())
            {
                MessageBox.Show("كلمة المرور الجديدة وتأكيدها غير متطابقين!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "UPDATE Login_tbl SET password = @password, confirmpass = @confirmpass WHERE email = @email";

            try
            {
                using (SqlConnection connection = new SqlConnection(con))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@password", txtNewPassword.Text.Trim());
                        command.Parameters.AddWithValue("@confirmpass", txtConfirmPassword.Text.Trim());
                        command.Parameters.AddWithValue("@email", emailname);

                        command.ExecuteNonQuery();

                        MessageBox.Show("تم تحديث كلمة المرور بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        txtPassword.Text = txtNewPassword.Text.Trim();

                        txtOldPassword.Clear();
                        txtNewPassword.Clear();
                        txtConfirmPassword.Clear();

                        panel8.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في التحديث", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // إظهار / إخفاء كلمة المرور
        private void guna2CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            char mask = guna2CheckBox1.Checked ? '\0' : '*';
            txtNewPassword.PasswordChar = mask;
            txtOldPassword.PasswordChar = mask;
            txtConfirmPassword.PasswordChar = mask;
        }

        #endregion

        #region 4. إدارة جهات الاتصال (Contacts Management)

        // فتح / إغلاق لوحة الشات الرئيسية
        private void guna2Button2_Click(object sender, EventArgs e)
        {
            panel9.Visible = !panel9.Visible;

            if (panel9.Visible)
            {
                panel9.BringToFront();

                // عرض جهات الاتصال وإخفاء الرسائل ومربع الكتابة مؤقتاً
                flowLayoutPanel1.Visible = true;
                flowLayoutPanelMessages.Visible = false;
                ResetChatArea();

                LoadContacts();
            }
        }

        // جلب قائمة المستخدمين المسجلين ما عدا المستخدم الحالي
        private void LoadContacts()
        {
            flowLayoutPanel1.Controls.Clear();

            string query = "SELECT firstname, lastname, email, image FROM Login_tbl WHERE email != @myEmail";

            try
            {
                using (SqlConnection connection = new SqlConnection(con))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@myEmail", emailname);
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UserControl1 contact = new UserControl1();
                                contact.tittle = $"{reader["firstname"]} {reader["lastname"]}";
                                contact.userEmail = reader["email"].ToString();

                                if (reader["image"] != DBNull.Value)
                                {
                                    byte[] imgData = (byte[])reader["image"];
                                    contact.image = ConvertByteArrayToImage(imgData);
                                }

                                contact.Click += Contact_Selected;
                                flowLayoutPanel1.Controls.Add(contact);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في تحميل جهات الاتصال", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // اختيار جهة اتصال وبدء المحادثة معها
        private void Contact_Selected(object sender, EventArgs e)
        {
            UserControl1 clickedContact = (UserControl1)sender;

            activeChatUserEmail = clickedContact.userEmail;

            // إخفاء القائمة وإظهار مساحة المحادثة وأدوات الإرسال
            flowLayoutPanel1.Visible = false;
            flowLayoutPanelMessages.Visible = true;
            txtMessage.Visible = true;
            btnSend.Visible = true;

            // جلب وعرض الرسائل المتبادلة
            LoadMessages(activeChatUserEmail);
        }

        // زر الرجوع لقائمة جهات الاتصال
        private void guna2CirclePictureBox4_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = true;
            flowLayoutPanelMessages.Visible = false;
            ResetChatArea();
        }

        #endregion

        #region 5. نظام إرسال وعرض الرسائل (Messaging System)

        // جلب الرسائل السابقة بين الطرفين وبناء الفقاعات ديناميكياً
        private void LoadMessages(string otherUserEmail)
        {
            flowLayoutPanelMessages.Controls.Clear();

            string query = "SELECT sender_email, message_text, sent_at FROM Messages_tbl " +
                           "WHERE (sender_email = @myEmail AND receiver_email = @otherEmail) " +
                           "   OR (sender_email = @otherEmail AND receiver_email = @myEmail) " +
                           "ORDER BY sent_at ASC";

            try
            {
                using (SqlConnection connection = new SqlConnection(con))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@myEmail", emailname);
                        command.Parameters.AddWithValue("@otherEmail", otherUserEmail);
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string sender = reader["sender_email"].ToString();
                                string msgText = reader["message_text"].ToString();
                                DateTime time = Convert.ToDateTime(reader["sent_at"]);

                                bool isMe = (sender == emailname);

                                // 1. إعداد نص الرسالة والوقت
                                Label lbl = new Label();
                                lbl.Text = $"{msgText}\n\n{time:hh:mm tt}";
                                lbl.ForeColor = Color.White;
                                lbl.Font = new Font("Segoe UI", 9.5f);
                                lbl.AutoSize = true;

                                // 2. إنشاء فقاعة الرسالة
                                Panel bubble = new Panel();
                                bubble.AutoSize = true;
                                bubble.Padding = new Padding(10);
                                bubble.BackColor = isMe ? Color.FromArgb(0, 168, 132) : Color.FromArgb(45, 62, 80);
                                bubble.Controls.Add(lbl);

                                // 3. حساب أقصى عرض للفقاعة
                                int maxBubbleWidth = flowLayoutPanelMessages.ClientSize.Width - 70;
                                bubble.MaximumSize = new Size(Math.Max(maxBubbleWidth, 120), 0);

                                // 4. حساب الهوامش لمنع السكرول الأفقي ودفع رسائلي لليمين
                                int bubbleWidth = bubble.PreferredSize.Width;
                                int availableWidth = flowLayoutPanelMessages.ClientSize.Width - 30;
                                int leftMargin = isMe ? (availableWidth - bubbleWidth) : 10;
                                bubble.Margin = new Padding(Math.Max(leftMargin, 10), 3, 5, 3);

                                flowLayoutPanelMessages.Controls.Add(bubble);
                            }
                        }
                    }
                }

                // تمرير تلقائي إلى آخر رسالة
                if (flowLayoutPanelMessages.Controls.Count > 0)
                {
                    flowLayoutPanelMessages.ScrollControlIntoView(flowLayoutPanelMessages.Controls[flowLayoutPanelMessages.Controls.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في تحميل الرسائل", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // إرسال رسالة جديدة
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(activeChatUserEmail))
            {
                MessageBox.Show("يرجى اختيار جهة اتصال من قائمة الشات أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string msg = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            string query = "INSERT INTO Messages_tbl (sender_email, receiver_email, message_text, sent_at) " +
                           "VALUES (@sender, @receiver, @text, @time)";

            try
            {
                using (SqlConnection connection = new SqlConnection(con))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@sender", emailname);
                        command.Parameters.AddWithValue("@receiver", activeChatUserEmail);
                        command.Parameters.AddWithValue("@text", msg);
                        command.Parameters.AddWithValue("@time", DateTime.Now);

                        connection.Open();
                        command.ExecuteNonQuery();

                        // تفريغ الصندوق وتحديث الشاشة فوراً
                        txtMessage.Clear();
                        txtMessage.Focus();
                        LoadMessages(activeChatUserEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ أثناء الإرسال", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // إرسال الرسالة بزر Enter
        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSend.PerformClick();
                e.SuppressKeyPress = true;
            }
        }

        // التحديث التلقائي الذكي للرسائل في الخلفية كل ثانية
        private void timer3_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(activeChatUserEmail) && flowLayoutPanelMessages.Visible)
            {
                string countQuery = "SELECT COUNT(*) FROM Messages_tbl " +
                                    "WHERE (sender_email = @myEmail AND receiver_email = @otherEmail) " +
                                    "   OR (sender_email = @otherEmail AND receiver_email = @myEmail)";

                try
                {
                    using (SqlConnection connection = new SqlConnection(con))
                    {
                        using (SqlCommand cmd = new SqlCommand(countQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@myEmail", emailname);
                            cmd.Parameters.AddWithValue("@otherEmail", activeChatUserEmail);
                            connection.Open();

                            int totalMessagesInDb = Convert.ToInt32(cmd.ExecuteScalar());

                            // لا نعيد بناء الواجهة إلا إذا وصلت رسالة جديدة لتفادي وميض الشاشة
                            if (totalMessagesInDb != flowLayoutPanelMessages.Controls.Count)
                            {
                                LoadMessages(activeChatUserEmail);
                            }
                        }
                    }
                }
                catch
                {
                    // تجاهل الخطأ اللحظي أثناء التزامن
                }
            }
        }

        #endregion

        #region 6. الحركات والقوائم الجانبية (UI Animations & Sidebar)

        // فتح / إغلاق السايد بار الجانبي الرئيسي
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int speed = 350;

            if (isSidebarCollapsed)
            {
                panel1.Width += speed;

                if (panel1.Width >= panel1.MaximumSize.Width)
                {
                    panel1.Width = panel1.MaximumSize.Width;
                    isSidebarCollapsed = false;
                    timer1.Stop();
                    pictureBox1.Image = Properties.Resources.photo_2026_11_09_12_42_03;
                }
            }
            else
            {
                panel1.Width -= speed;

                if (panel1.Width <= panel1.MinimumSize.Width)
                {
                    panel1.Width = panel1.MinimumSize.Width;
                    isSidebarCollapsed = true;
                    timer1.Stop();
                    pictureBox1.Image = Properties.Resources.photo_2026_11_09_15_19_27;
                }
            }
        }

        // قائمة الخيارات الإضافية (Top Right Menu)
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            panel4.Visible = !panel4.Visible;
            panel4.BringToFront();
        }

        // تسجيل الخروج والعودة لشاشة الدخول Form1
        private void guna2Button6_Click(object sender, EventArgs e)
        {
            Form1 loginForm = new Form1();
            loginForm.Show();
            this.Dispose();
        }

        // إظهار بانل الحساب
        private void btnAccount_Click(object sender, EventArgs e)
        {
            panel5.Visible = !panel5.Visible;
        }

        // فتح / إغلاق القائمة المنسدلة للإعدادات
        private void guna2Button7_Click(object sender, EventArgs e)
        {
            timer2.Start();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            int speed = 207;

            if (isPanel6Collapsed)
            {
                panel6.Height += speed;

                if (panel6.Height >= panel6.MaximumSize.Height)
                {
                    panel6.Height = panel6.MaximumSize.Height;
                    isPanel6Collapsed = false;
                    timer2.Stop();
                    guna2Button7.Image = Properties.Resources.photo_2026_11_09_18_04_38;
                }
            }
            else
            {
                panel6.Height -= speed;

                if (panel6.Height <= panel6.MinimumSize.Height)
                {
                    panel6.Height = panel6.MinimumSize.Height;
                    isPanel6Collapsed = true;
                    timer2.Stop();
                    guna2Button7.Image = Properties.Resources.photo_2026_11_09_17_45_281;
                }
            }
        }

        // إظهار بانل تعديل الملف الشخصي
        private void guna2Button5_Click(object sender, EventArgs e)
        {
            panel7.Visible = !panel7.Visible;
        }

        // إظهار بانل تغيير كلمة المرور
        private void guna2Button6_Click_1(object sender, EventArgs e)
        {
            panel8.Visible = !panel8.Visible;
        }

        #endregion

        #region 7. الدوال المساعدة العامة (Helper Methods)

        // تصفير وإخفاء منطقة الإرسال
        private void ResetChatArea()
        {
            txtMessage.Visible = false;
            btnSend.Visible = false;
            activeChatUserEmail = "";
        }

        // تحويل الصورة إلى بايتات للحفظ في SQL بأمان وتفادي قفل الملفات
        private byte[] ConvertImageToByteArray(Image image)
        {
            if (image == null) return null;

            using (MemoryStream ms = new MemoryStream())
            {
                using (Bitmap bmp = new Bitmap(image))
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                }
                return ms.ToArray();
            }
        }

        // تحويل البايتات القادمة من قاعدة البيانات إلى كائن Image
        private Image ConvertByteArrayToImage(byte[] imgData)
        {
            if (imgData == null || imgData.Length == 0) return null;

            using (MemoryStream ms = new MemoryStream(imgData))
            {
                using (Image tempImg = Image.FromStream(ms))
                {
                    return new Bitmap(tempImg);
                }
            }
        }

        #endregion
    }
}
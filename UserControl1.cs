using System.Drawing;
using System.Windows.Forms;

namespace ChatApplication
{
    public partial class UserControl1 : UserControl
    {
        #region 1. المتغيرات والخصائص العامة (Properties)

        // اسم المستخدم الظاهر في البطاقة
        private string _userName;
        public string tittle
        {
            get => _userName;
            set
            {
                _userName = value;
                label1.Text = value;
            }
        }

        // الصورة الشخصية للمستخدم
        private Image _userImage;
        public Image image
        {
            get => _userImage;
            set
            {
                _userImage = value;
                guna2CirclePictureBox1.Image = value;
            }
        }

        // البريد الإلكتروني للمستخدم (معرّف فريد لا يظهر في الواجهة لكنه يُستخدم لربط المحادثة)
        public string userEmail { get; set; }

        #endregion

        #region 2. مشيد الكائن وربط الأحداث (Constructor & Event Routing)

        public UserControl1()
        {
            InitializeComponent();

            // تمرير حدث النقر من العناصر الداخلية إلى حاوية البطاقة بالكامل
            // لضمان استجابة حدث الاختيار حتى لو ضغط المستخدم على النص أو الصورة مباشرة
            label1.Click += (sender, e) => this.OnClick(e);
            guna2CirclePictureBox1.Click += (sender, e) => this.OnClick(e);
        }

        #endregion
    }
}
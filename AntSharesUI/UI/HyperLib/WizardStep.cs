using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace AntShares.UI.HyperLib
{
    [Designer(typeof(ParentControlDesigner))]
    [Docking(DockingBehavior.AutoDock)]
    [ToolboxItem(false)]
    public partial class WizardStep : UserControl
    {
        public WizardStep()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
        }

        [Category("向导")]
        [Description("获取或设置向导步骤的标题。")]
        public string Title
        {
            get
            {
                return lbl_title.Text;
            }
            set
            {
                lbl_title.Text = value;
            }
        }

        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        [Category("向导")]
        [Description("获取或设置向导步骤的说明文字。")]
        public string Caption
        {
            get
            {
                return lbl_caption.Text;
            }
            set
            {
                lbl_caption.Text = value;
            }
        }

        [Category("向导")]
        [Description("获取向导步骤的索引编号。")]
        public int Index
        {
            get;
            internal set;
        }
    }
}

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace AntShares.UI.HyperLib
{
    public partial class Wizard : Form
    {
        private WizardStepCollection m_steps = null;
        private int m_currentstep;
        private bool m_loaded = false;

        public Wizard()
        {
            InitializeComponent();
            m_steps = new WizardStepCollection(this);
            m_currentstep = 0;
        }

        private void Wizard_Load(object sender, System.EventArgs e)
        {
            m_loaded = true;
            OnCurrentStepChanged(-1);
        }

        private void Wizard_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult != DialogResult.OK)
            {
                if (MessageBox.Show("向导还没有完成，您是否确定要取消向导？", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }

        private void btn_step_prev_Click(object sender, System.EventArgs e)
        {
            int pStep = m_currentstep;
            if (m_currentstep <= 1)
            {
                m_currentstep = 0;
            }
            else
            {
                m_currentstep--;
            }
            if (pStep != m_currentstep)
            {
                OnCurrentStepChanged(pStep);
            }
        }

        private void btn_step_next_Click(object sender, System.EventArgs e)
        {
            if (m_currentstep + 1 >= m_steps.Count)
            {
                m_currentstep = m_steps.Count - 1;
                if (WizardFinished != null)
                {
                    WizardFinished(this, EventArgs.Empty);
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                m_currentstep++;
                OnCurrentStepChanged(m_currentstep - 1);
            }
        }

        private void btn_exit_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Category("向导")]
        [Description("获取向导步骤的集合。")]
        public WizardStepCollection Steps
        {
            get
            {
                return m_steps;
            }
        }

        [Category("向导")]
        [Description("获取或设置当前步骤的索引号。")]
        public int CurrentStep
        {
            get
            {
                return m_currentstep;
            }
            set
            {
                int pStep = m_currentstep;
                m_currentstep = value;
                if (m_currentstep != pStep)
                {
                    OnCurrentStepChanged(pStep);
                }
            }
        }

        [Category("向导")]
        [Description("获取或设置当前向导是否可以进入下一步。")]
        public bool CanStepNext
        {
            get
            {
                return btn_step_next.Enabled;
            }
            set
            {
                btn_step_next.Enabled = value;
            }
        }

        [Category("向导")]
        [Description("向导步骤改变时引发。")]
        public event EventHandler<WizardStepChangingEventArgs> WizardStepChanging;
        [Category("向导")]
        [Description("向导步骤改变后引发。")]
        public event EventHandler<WizardStepChangedEventArgs> WizardStepChanged;
        [Category("向导")]
        [Description("向导完成后引发。")]
        public event EventHandler WizardFinished;

        internal void OnCurrentStepChanged(int pStep)
        {
            if (m_loaded)
            {
                if (m_currentstep >= 0 && m_currentstep < m_steps.Count)
                {
                    WizardStepChangingEventArgs e = new WizardStepChangingEventArgs(pStep, m_currentstep, false);
                    if (WizardStepChanging != null)
                    {
                        WizardStepChanging(this, e);
                    }
                    if (e.Cancel)
                    {
                        m_currentstep = pStep;
                    }
                    else
                    {
                        pnl_steps.Controls.Clear();
                        if (m_steps.Count > 0)
                        {
                            pnl_steps.Controls.Add(m_steps[m_currentstep]);
                        }
                        OnStepCountChanged();
                        if (WizardStepChanged != null)
                        {
                            WizardStepChanged(this, e);
                        }
                    }
                }
            }
        }

        internal void OnStepCountChanged()
        {
            if (m_loaded)
            {
                btn_step_prev.Enabled = m_currentstep != 0;
                if (m_currentstep + 1 >= m_steps.Count)
                {
                    btn_step_next.Text = "完成(&N)";
                }
                else
                {
                    ComponentResourceManager resources = new ComponentResourceManager(typeof(Wizard));
                    btn_step_next.Text = resources.GetString("btn_step_next.Text");
                }
            }
        }
    }
}

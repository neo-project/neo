using System.Collections;

namespace AntShares.UI.HyperLib
{
    public class WizardStepCollection
        : CollectionBase
    {
        private Wizard m_wizard = null;

        public WizardStepCollection(Wizard wizard)
            : base()
        {
            m_wizard = wizard;
        }

        public WizardStep this[int index]
        {
            get
            {
                return (WizardStep)base.List[index];
            }
            set
            {
                base.List[index] = value;
            }
        }

        public void Add(WizardStep item)
        {
            base.List.Add(item);
        }

        public void Remove(WizardStep item)
        {
            if (base.List[item.Index] == item)
            {
                base.List.RemoveAt(item.Index);
            }
        }

        protected override void OnClear()
        {
            foreach (WizardStep i in base.List)
            {
                i.Index = -1;
            }
            base.OnClear();
        }

        protected override void OnClearComplete()
        {
            base.OnClearComplete();
            m_wizard.OnCurrentStepChanged(m_wizard.CurrentStep);
            m_wizard.OnStepCountChanged();
        }

        protected override void OnInsertComplete(int index, object value)
        {
            base.OnInsertComplete(index, value);
            for (int i = index; i < base.Count; i++)
            {
                this[i].Index = i;
            }
            if (index <= m_wizard.CurrentStep)
            {
                m_wizard.OnCurrentStepChanged(m_wizard.CurrentStep);
            }
            m_wizard.OnStepCountChanged();
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            base.OnRemoveComplete(index, value);
            ((WizardStep)value).Index = -1;
            for (int i = index; i < base.Count; i++)
            {
                this[i].Index = i;
            }
            if (index <= m_wizard.CurrentStep)
            {
                m_wizard.OnCurrentStepChanged(m_wizard.CurrentStep);
            }
            m_wizard.OnStepCountChanged();
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            base.OnSetComplete(index, oldValue, newValue);
            ((WizardStep)oldValue).Index = -1;
            ((WizardStep)newValue).Index = index;
            if (index == m_wizard.CurrentStep)
            {
                m_wizard.OnCurrentStepChanged(m_wizard.CurrentStep);
            }
        }
    }
}

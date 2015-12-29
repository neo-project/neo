namespace AntShares.UI.HyperLib
{
    public class WizardStepChangingEventArgs
        : WizardStepChangedEventArgs
    {
        public bool Cancel { get; set; }

        internal WizardStepChangingEventArgs(int previousStep, int currentStep, bool cancel)
            : base(previousStep, currentStep)
        {
            this.Cancel = cancel;
        }
    }
}

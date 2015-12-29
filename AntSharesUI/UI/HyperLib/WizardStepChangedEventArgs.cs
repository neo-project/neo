using System;

namespace AntShares.UI.HyperLib
{
    public class WizardStepChangedEventArgs
        : EventArgs
    {
        public int PreviousStep { get; private set; }
        public int CurrentStep { get; private set; }

        internal WizardStepChangedEventArgs(int previousStep, int currentStep)
        {
            this.PreviousStep = previousStep;
            this.CurrentStep = currentStep;
        }
    }
}

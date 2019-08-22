
        protected override void LoadContext(ExecutionContext context)
        {
            // Set default execution context state

            context.SetState(new ExecutionContextState()
            {
                ScriptHash = ((byte[])context.Script).ToScriptHash()
            });

            base.LoadContext(context);
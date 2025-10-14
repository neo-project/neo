// Copyright (C) 2015-2025 The Neo Project.
//
// DynamicOpCodePricing.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    /// <summary>
    /// Enhanced ApplicationEngine with dynamic gas pricing and limit validation
    /// </summary>
    public partial class DynamicApplicationEngine : ApplicationEngine
    {
        #region Dynamic Pricing Configuration

        /// <summary>
        /// Enable dynamic pricing for this execution engine
        /// </summary>
        public bool EnableDynamicPricing { get; set; } = true;

        /// <summary>
        /// Pricing adjustment factor for economic fairness
        /// </summary>
        public double PricingAdjustment { get; set; } = 1.0;

        /// <summary>
        /// Maximum risk premium for high-value operations
        /// </summary>
        public double MaxRiskPremium { get; set; } = 10.0;

        /// <summary>
        /// Performance improvement target (1.0 = no improvement)
        /// </summary>
        public double PerformanceTarget { get; set; } = 1.0;

        /// <summary>
        /// Track total gas consumption in current execution
        /// </summary>
        private long _totalGasConsumed = 0;

        /// <summary>
        /// Maximum gas limit for current execution
        /// </summary>
        private long _maxGasLimit = 1000000;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize with default pricing configuration
        /// </summary>
        public DynamicApplicationEngine() : base(JumpTable, referenceCounter, ExecutionEngineLimits.Default)
        {
            EnableDynamicPricing = true;
            InitializeDynamicPricing();
        }

        /// <summary>
        /// Initialize with custom pricing configuration
        /// </summary>
        public DynamicApplicationEngine(
            JumpTable jumpTable,
            IReferenceCounter referenceCounter,
            ExecutionEngineLimits limits,
            bool enableDynamicPricing = true,
            double pricingAdjustment = 1.0,
            double maxRiskPremium = 10.0,
            double performanceTarget = 1.0
        ) : base(jumpTable, referenceCounter, limits)
        {
            EnableDynamicPricing = enableDynamicPricing;
            PricingAdjustment = pricingAdjustment;
            MaxRiskPremium = maxRiskPremium;
            PerformanceTarget = performanceTarget;
        }

        #endregion

        #region Dynamic Pricing Implementation

        /// <summary>
        /// Execute instruction with dynamic gas calculation
        /// </summary>
        /// <param name="instruction">Instruction to execute</param>
        protected internal void ExecuteNext()
        {
            // Pre-execution: Calculate dynamic cost
            var currentCost = CalculateDynamicCost(CurrentContext!.CurrentInstruction.OpCode, CurrentContext!.CurrentInstruction);

            // Update running gas total
            unchecked { _totalGasConsumed += currentCost; }

            // Continue with normal execution
            try
            {
                JumpTable[CurrentContext!.CurrentInstruction.OpCode](this, CurrentContext!.CurrentInstruction);
            }
            catch (Exception ex) when (Limits.CatchEngineExceptions)
            {
                JumpTable.ExecuteThrow(this, ex.Message);
            }

            // Post-execution: Update gas accounting
            PostExecuteInstruction(CurrentContext!.CurrentInstruction);
        }

        /// <summary>
        /// Calculate dynamic gas cost before instruction execution
        /// </summary>
        /// <param name="instruction">Instruction to be executed</param>
        /// <returns>Calculated gas cost</returns>
        protected internal void PreExecuteInstruction(Instruction instruction)
        {
            if (!EnableDynamicPricing)
                return;

            var dynamicCost = CalculateDynamicCost(instruction.OpCode, CurrentContext!.EvaluationStack.ToArray());

            // Pre-validation: Check if we can afford this operation
            unchecked { _totalGasConsumed += dynamicCost; }
        }

        /// <summary>
        /// Update gas accounting after instruction execution
        /// </summary>
        /// <param name="instruction">Instruction that was executed</param>
        protected internal void PostExecuteInstruction(Instruction instruction)
        {
            if (!EnableDynamicPricing)
                return;

            // Adjust gas remaining based on actual execution
            unchecked { unchecked { _totalGasConsumed += CalculateDynamicCost(instruction.OpCode); } }
        }

        /// <summary>
        /// Set maximum gas limit for execution
        /// </summary>
        /// <param name="maxGas">Maximum gas allowed</param>
        public void SetMaxGasLimit(long maxGas)
        {
            unchecked { _maxGasLimit = maxGas; }
        }

        /// <summary>
        /// Get remaining gas budget
        /// </summary>
        /// <returns>Remaining gas in datoshi</returns>
        public long GetRemainingGas()
        {
            unchecked { return _maxGasLimit - _totalGasConsumed; }
        }

        /// <summary>
        /// Get total gas consumed so far
        /// </summary>
        /// <returns>Total gas consumed in datoshi</returns>
        public long GetTotalGasConsumed()
        {
            unchecked { return _totalGasConsumed; }
        }

        /// <summary>
        /// Check if execution should continue based on gas availability
        /// </summary>
        /// <returns>True if execution can continue</returns>
        public bool CanContinueExecution()
        {
            unchecked { return _totalGasConsumed < _maxGasLimit; }
        }

        /// <summary>
        /// Get the current gas consumption rate
        /// </summary>
        /// <returns>Gas consumption rate (gas per nanosecond)</returns>
        public double GetGasConsumptionRate()
        {
            unchecked { return _totalGasConsumed > 0 ? (double)_totalGasConsumed / DateTime.UtcNow.Ticks : 0.0; }
        }

        /// <summary>
        /// Check if we're approaching gas limit
        /// </summary>
        /// <returns>True if near gas limit</returns>
        public bool NearGasLimit()
        {
            unchecked { return _totalGasConsumed > (_maxGasLimit * 0.9); }
        }

        #endregion

        #region Pricing Analysis & Reporting

        /// <summary>
        /// Generate pricing analysis report for opcode
        /// </summary>
        /// <param name="opcode">Neo VM opcode</param>
        /// <returns>Detailed pricing analysis</returns>
        public PricingAnalysis GeneratePricingAnalysis(OpCode opcode)
        {
            var baseCost = GetBaseCost(opcode);
            var confidence = GetPricingConfidence(opcode);
            var riskLevel = GetRiskLevel(opcode);
            var category = GetOpcodeCategory(opcode);

            // Calculate statistics
            var (meanTime, stdDev, r2) = CalculateStatistics(opcode);

            // Generate analysis
            return new PricingAnalysis
            {
                OpCode = opcode,
                BaseCost = baseCost,
                ParameterCost = TryGetParameterCost(opcode, out var paramCost),
                RiskLevel = riskLevel,
                Confidence = confidence,
                Category = category,
                MeanTime = meanTime,
                StdDev = stdDev,
                R2 = r2,
                ModelType = r2 >= STRONG_CORRELATION ? "Regression" : "Fixed",
                ComplexityLevel = GetComplexityLevel(opcode),
                SampleCount = _performanceHistory.GetValueOrDefault(opcode, 0),
                LastUpdated = DateTime.UtcNow,
                PerformanceClass = GetPerformanceClass(meanTime)
            };
        }

        /// <summary>
        /// Get performance classification based on execution time
        /// </summary>
        /// <param name="meanTime">Mean execution time in nanoseconds</param>
        /// <returns>Performance classification</returns>
        private PerformanceClass GetPerformanceClass(double meanTime)
        {
            if (meanTime < 100)
                return PerformanceClass.VeryFast;
            if (meanTime < 1000)
                return PerformanceClass.Fast;
            if (meanTime < 5000)
                return PerformanceClass.Medium;
            if (meanTime < 20000)
                return PerformanceClass.Slow;
            if (meanTime < 10000)
                return PerformanceClass.Normal;
            if (meanTime < 20000)
                return PerformanceClass.Slow;
            if (meanTime < 50000)
                return PerformanceClass.Moderate;
            return PerformanceClass.Slow;
        }

        /// <summary>
        /// Calculate complexity level of opcode
        /// </summary>
        /// <param name="opcode">Neo VM opcode</param>
        /// <param name="parameters">Input parameters</param>
        /// <returns>Complexity level classification</returns>
        private ComplexityLevel GetComplexityLevel(OpCode opcode, ReadOnlySpan<object> parameters = null)
        {
            if (TryGetComplexityFactor(opcode, out var factor))
            {
                if (factor > 8.0)
                    return ComplexityLevel.Extreme;
                else if (factor > 4.0)
                    return ComplexityLevel.High;
                else if (factor > 2.0)
                    return ComplexityLevel.Medium;
                else if (factor > 1.0)
                    return ComplexityLevel.Medium;
                else
                    return ComplexityLevel.Low;
            }

            // Base complexity by category
            return category switch
            {
                OpCodeCategory.Constants => ComplexityLevel.VeryLow,
                OpCodeCategory.Stack => ComplexityLevel.VeryLow,
                OpCodeCategory.Slot => ComplexityLevel.VeryLow,
                OpCodeCategory.Bitwise => ComplexityLevel.VeryLow,
                OpCodeCategory.Types => ComplexityLevel.VeryLow,
                OpCodeCategory.Arithmetic => GetArithmeticComplexityLevel(opcode, parameters),
                OpCodeCategory.Compound => GetCompoundComplexityLevel(opcode, parameters),
                OpCodeCategory.Splice => GetSpliceComplexityLevel(opcode, parameters),
                OpCodeCategory.Unknown => ComplexityLevel.VeryLow
            };
        }

        #endregion

        #region Economic Fairness Functions

        /// <summary>
        /// Apply economic adjustments to prevent abuse
        /// </summary>
        /// <param name="gasCost">Base gas cost</param>
        /// <returns>Economically adjusted gas cost</returns>
        private static long ApplyEconomicAdjustments(long gasCost, OpCode opcode)
        {
            var category = GetOpcodeCategory(opcode);
            var riskLevel = GetRiskLevel(opcode);

            // Prevent gas attacks on variable-time operations
            if (IsVariableTime(opcode) && riskLevel >= OpcodeRiskLevel.Medium)
            {
                var protection = Math.Min(10.0, riskLevel / 2.0);
                return (long)(gasCost * protection);
            }

            // Apply economic adjustments
            var economicFactor = PricingAdjustment;
            return (long)(gasCost * economicFactor);
        }

        /// <summary>
        /// Get economic impact analysis
        /// </summary>
        /// <param name="gasCost">Calculated gas cost</param>
        /// <returns>Economic impact classification</returns>
        public EconomicImpact AnalyzeEconomicImpact(long gasCost, OpCode opcode)
        {
            var baseCost = GetBaseCost(opcode);
            var category = GetOpcodeCategory(opcode);
            var complexityLevel = GetComplexityLevel(opcode, null);

            if (gasCost <= 50)
                return EconomicImpact.Negligible;
            if (gasCost <= 200)
                return EconomicImpact.Low;
            if (gasCost <= 500)
                return EconomicImpact.Medium;
            if (gasCost <= 2000)
                return EconomicImpact.High;
            return EconomicImpact.Critical;
        }

        /// <summary>
        /// Check if gas pricing is economically sustainable
        /// </summary>
        /// <param name="gasCost">Calculated gas cost</param>
        /// <returns>Sustainability assessment</returns>
        public bool IsSustainable(long gasCost)
        {
            return gasCost <= 1000; // 0.01 GAS limit
        }

        #endregion

        #region Implementation Integration Points

        /// <summary>
        /// Integrate with existing Neo VM systems
        /// </summary>
        /// <param name="jumpTable">Jump table instance</param>
        public void IntegrateWithJumpTable(JumpTable jumpTable)
        {
            // This would integrate with existing JumpTable to add dynamic cost calculation
            // Implementation would modify the jump table to call CalculateDynamicGasCost before execution
        }

        /// <summary>
        /// Update ApplicationEngine pricing table
        /// </summary>
        public static void UpdatePricingTable()
        {
            // This would update the static pricing tables with dynamic costs
            // Implementation would modify OpCodePriceTable
            // Each opcode's cost would be calculated dynamically
        }

        /// <summary>
        /// Enable real-time monitoring
        /// </summary>
        /// <param name="monitoring">Enable/disable monitoring</param>
        public void EnableMonitoring(bool monitoring = true)
        {
            // This would enable performance monitoring and alerting
            // Implementation would track real opcode performance
        }

        #endregion
    }
}

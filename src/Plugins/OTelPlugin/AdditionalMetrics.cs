// Copyright (C) 2015-2025 The Neo Project.
//
// AdditionalMetrics.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.OpenTelemetry
{
    /// <summary>
    /// Additional critical metrics for comprehensive observability
    /// </summary>
    public static class AdditionalMetricNames
    {
        // Consensus metrics
        public const string ConsensusRound = "neo.consensus.round";
        public const string ConsensusView = "neo.consensus.view";
        public const string ConsensusState = "neo.consensus.state";
        public const string ConsensusViewChangesTotal = "neo.consensus.view_changes_total";
        public const string ConsensusMessagesSent = "neo.consensus.messages_sent_total";
        public const string ConsensusMessagesReceived = "neo.consensus.messages_received_total";
        public const string ConsensusTimeToFinality = "neo.consensus.time_to_finality";

        // Storage metrics
        public const string StorageReadLatency = "neo.storage.read_latency";
        public const string StorageWriteLatency = "neo.storage.write_latency";
        public const string StorageSize = "neo.storage.size_bytes";
        public const string StorageReadTotal = "neo.storage.reads_total";
        public const string StorageWriteTotal = "neo.storage.writes_total";

        // Contract execution metrics
        public const string ContractExecutionTime = "neo.contract.execution_time";
        public const string ContractGasConsumed = "neo.contract.gas_consumed";
        public const string ContractExecutionErrors = "neo.contract.execution_errors_total";
        public const string ContractDeployments = "neo.contract.deployments_total";

        // Transaction pool metrics
        public const string TransactionPoolAddLatency = "neo.transaction_pool.add_latency";
        public const string TransactionPoolRemoveLatency = "neo.transaction_pool.remove_latency";
        public const string TransactionPoolRejections = "neo.transaction_pool.rejections_total";
        public const string TransactionPoolEvictions = "neo.transaction_pool.evictions_total";

        // State metrics
        public const string StateRootHeight = "neo.state.root_height";
        public const string StateValidatedRootHeight = "neo.state.validated_root_height";
        public const string StateRootLag = "neo.state.root_lag";
        public const string StateSnapshotApplyDuration = "neo.state.snapshot_apply_duration_ms";
        public const string StateSnapshotCommitDuration = "neo.state.snapshot_commit_duration_ms";
        public const string StateSnapshotHealth = "neo.state.snapshot_health";
        public const string StateValidations = "neo.state.validations_total";
        public const string StateValidationErrors = "neo.state.validation_errors_total";

        // P2P detailed metrics
        public const string P2PMessageLatency = "neo.p2p.message_latency";
        public const string P2PPeerLatency = "neo.p2p.peer_latency";
        public const string P2PPeerQuality = "neo.p2p.peer_quality";
        public const string P2PBannedPeers = "neo.p2p.banned_peers";

        // Health and readiness
        public const string NodeHealthScore = "neo.node.health_score";
        public const string NodeReadiness = "neo.node.readiness";
        public const string NodeLastActivity = "neo.node.last_activity";

        // Virtual machine metrics
        public const string VmInstructionRate = "neo.vm.instruction_rate";
        public const string VmInstructionLatency = "neo.vm.instruction_latency_ms";
        public const string VmEvaluationStackDepth = "neo.vm.evaluation_stack_depth";
        public const string VmInvocationStackDepth = "neo.vm.invocation_stack_depth";
        public const string VmResultStackDepth = "neo.vm.result_stack_depth";
        public const string VmReferenceSweepRate = "neo.vm.reference_sweeps_rate";
        public const string VmHotTraceRatio = "neo.vm.trace.hot_ratio";
        public const string VmHotTraceHits = "neo.vm.trace.hot_hits";
        public const string VmHotTraceMaxRatio = "neo.vm.trace.max_hot_ratio";
        public const string VmHotTraceMaxHits = "neo.vm.trace.max_hot_hits";
        public const string VmTraceProfileCount = "neo.vm.trace.profile_count";
        public const string VmSuperInstructionPlanCount = "neo.vm.superinstruction.plan_count";

        // Resource utilization
        public const string FileDescriptors = "process.file_descriptors";
        public const string OpenConnections = "process.open_connections";
        public const string DiskFreeBytes = "neo.node.disk_free_bytes";
        public const string ChainDbSize = "neo.node.chain_db_size_bytes";

        // RPC metrics
        public const string RpcRequestsTotal = "neo.rpc.requests_total";
        public const string RpcRequestErrorsTotal = "neo.rpc.request_errors_total";
        public const string RpcRequestDuration = "neo.rpc.request_duration_ms";
        public const string RpcActiveRequests = "neo.rpc.active_requests";
    }
}

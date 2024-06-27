# SmartThrottler

## 1. Introduction

The SmartThrottler is designed to protect the blockchain from potential attacks and network congestion by intelligently controlling transaction flow. This document outlines its core features and implementation details. Issue ref. https://github.com/neo-project/neo/issues/2862.

## 2. Key Features

- Dynamic adjustment of transaction acceptance rate
- Per-sender transaction limits
- Priority handling for high-fee transactions
- Multi-factor network load estimation
- Adaptive response to new block additions

## 3. Core Components

### 3.1 Transaction Acceptance Control

The `ShouldAcceptTransaction` method is the gatekeeper for new transactions. It resets the per-second transaction counter every second and checks against sender limits. High-fee transactions get preferential treatment.

### 3.2 Network Load Estimation

Network load is calculated based on three factors:

1. Memory Pool Usage (30% weight)
    - Ratio of current transactions to pool capacity
    - Indicates short-term transaction backlog

2. Recent Block Times (30% weight)
    - Average time for the last 20 blocks vs. expected time
    - Reflects medium-term network performance

3. Transaction Growth or Block Fullness (40% weight)
    - Either current block transaction count or unconfirmed transaction growth
    - Shows immediate transaction processing pressure

The final load score is capped at 100 to maintain consistency.

### 3.3 Optimal TPS Calculation

The `CalculateOptimalTps` method determines the best transactions-per-second rate. It factors in memory pool usage, network load, and current block details to adapt to changing conditions.

### 3.4 High-Priority Transaction Identification

Transactions with fees exceeding 3 times the average are flagged as high-priority. This allows important transactions to bypass normal throttling limits.

### 3.5 Sender Limit Enforcement

Each sender is capped at 10 transactions in the memory pool. This prevents any single entity from flooding the network.

## 4. Workflow

1. Initialization: Set up initial parameters.
2. Transaction Acceptance:
    - Check and reset per-second counter if needed
    - Evaluate network conditions
    - Apply throttling rules
    - Update counters for accepted transactions
3. Network State Updates:
    - Recalculate average fees
    - Adjust throttling parameters
4. Transaction Removal:
    - Update sender transaction counts

## 5. Key Algorithms

### 5.1 Network Load Calculation

```
load = (pool_usage * 30) + (block_time_factor * 30) + (tx_growth_or_block_fullness * 40)
```

### 5.2 Optimal TPS Calculation

```
optimal_tps = base_tps * (1 - pool_usage) * (1 - network_load/100) * block_factor
```

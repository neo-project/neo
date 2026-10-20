# Neo Vulnerability Bounty Program

Neo is committed to maintaining the security and stability of its blockchain network. We encourage security researchers to responsibly report vulnerabilities affecting Neo N3 and reward eligible findings based on their severity and impact.

## Program Rules

- **Local testing only.** All vulnerability testing and exploitation must be performed in an isolated local network. Testing, exploiting, or attempting to exploit vulnerabilities on Neo MainNet or public TestNet is strictly prohibited.
- **No harm.** Researchers must not access third-party data or assets, disrupt network operations, or cause damage to users or infrastructure.
- **Responsible disclosure.** Vulnerabilities must remain confidential and must not be publicly disclosed without prior authorization from Neo.
- **First valid report.** Only the first eligible report of a previously unknown vulnerability may receive a monetary reward.
- **Related vulnerabilities.** Multiple findings arising from the same underlying root cause may be treated as a single vulnerability.
- **Final assessment.** Neo reserves the right to determine eligibility, severity, and reward amounts at its discretion.

## Scope

The program covers security vulnerabilities affecting supported and actively maintained versions of Neo N3 in the following official repositories:

- [neo](https://github.com/neo-project/neo)
- [neo-vm](https://github.com/neo-project/neo-vm)
- [neo-node](https://github.com/neo-project/neo-node)
- [neo-devpack-dotnet](https://github.com/neo-project/neo-devpack-dotnet)

Only vulnerabilities with a demonstrable security impact on Neo N3 are eligible for monetary rewards.

### Out of Scope

- Neo.GUI, websites, third-party applications, smart contracts, and repositories not explicitly listed above.
- Unsupported, deprecated, or unmaintained versions.
- Theoretical vulnerabilities, speculative attacks, and findings without a reproducible security impact are not eligible for rewards.
- Code quality issues, best-practice recommendations, and feature requests.
- Issues requiring non-default configurations or optional plugins (such as RPC plugin) are automatically classified as Informational or Low severity, unless they also affect a default configuration.
- Economic, governance, consensus-majority, and Sybil attacks that do not exploit an underlying vulnerability in Neo N3.
- Differences between the official C# implementation and alternatives, such as neo-go, are classified as informational and are not eligible for rewards.
- Exposed credentials, API keys, or secrets without evidence of an active security impact.
- Issues requiring compromised credentials, privileged access, social engineering, or attacks against third-party infrastructure.
- Previously reported, publicly known, or duplicate vulnerabilities.

## Reporting Vulnerabilities

All vulnerabilities must be reported privately through [**Neo GitHub Private Vulnerability Reporting**](https://github.com/neo-project/neo/security), using the **Report a vulnerability** option in the Security tab.

Each report must include:

- A clear description of the vulnerability and affected version.
- Detailed steps to reproduce the issue.
- A working Proof of Concept (PoC) executed in an isolated local Neo N3 network.
- Evidence demonstrating the actual security impact.
- An assessment of the vulnerability's severity.

**A reproducible PoC in an isolated local network is mandatory for all submissions.** Reports without sufficient evidence or demonstrable security impact are not eligible for rewards.

## Rewards

Eligible vulnerabilities are evaluated by the Neo security team according to their impact, likelihood of exploitation, and overall risk.

| Severity | Maximum Reward | Example |
|---|---|---|
| Critical | $5,000 | Severe loss of assets |
| High | $2,500 | Network-wide failure |
| Medium | $1,000 | Single-node failure |
| Low | $100 | Other valid security issues |
| Info | Public Acknowledgment | Findings with no significant security impact |

Reward amounts are denominated in USD and paid in the equivalent amount of NEO or GAS.

Informational findings are not eligible for monetary rewards but may receive public acknowledgment at Neo's discretion, only after the issue has been resolved or Neo has explicitly authorized its disclosure.

Final severity and reward amounts are determined by Neo following technical validation and risk assessment. A valid report does not automatically guarantee a monetary reward.


We appreciate the contributions of security researchers in helping protect the Neo ecosystem.

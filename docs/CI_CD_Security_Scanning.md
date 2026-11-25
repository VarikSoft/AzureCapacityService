# CI/CD Security Scanning: Secrets and Vulnerable Dependencies

## Goal

Prevent accidental credential leaks and known vulnerable dependencies from reaching Azure. These checks run in **Azure Capacity Service CI** alongside unit and integration tests. The CD workflow starts only after a successful CI run on \`main\`.

## 1. Secret scanning — Gitleaks

**What it does:** Gitleaks examines Git content and history for strings that resemble credentials, such as API keys, access tokens, connection strings, and private keys. The CI workflow checks out full Git history (\`fetch-depth: 0\`) and runs \`gitleaks/gitleaks-action@v2\` in the \`secret-scan\` job.

**Pipeline configuration:**

\`\`\`yaml
- uses: actions/checkout@v4
  with:
    fetch-depth: 0

- name: Scan repository with Gitleaks
  uses: gitleaks/gitleaks-action@v2
  env:
    GITHUB_TOKEN: \${{ secrets.GITHUB_TOKEN }}
\`\`\`

The GitHub token belongs to the workflow; it is not an Azure client secret. Gitleaks does not require Azure authentication. If Gitleaks reports a leak, the CI workflow fails, preventing automatic CD.

**When a leak is detected:**

1. Treat the credential as compromised; revoke or rotate it in its issuing system.
2. Remove the secret from the code and configuration. Store required secrets in Azure Key Vault or a suitable secret store.
3. If necessary, coordinate Git history cleanup; deleting the latest occurrence does **not** revoke a credential or erase earlier commits.
4. Re-run CI. Review false positives carefully rather than disabling the scanner globally.

To run a local scan after installing Gitleaks:

\`\`\`bash
gitleaks git . --redact
\`\`\`

> GitHub's native secret scanning and push protection can provide additional coverage if enabled in repository settings. Gitleaks does not guarantee detection of every secret.

## 2. Dependency vulnerability scanning — NuGetAudit

**What it does:** NuGetAudit checks direct and transitive NuGet dependencies against known security advisories during \`dotnet restore\`. It is configured in the repository-root \`Directory.Build.props\` so the application and test projects use the same policy.

\`\`\`xml
<NuGetAudit>true</NuGetAudit>
<NuGetAuditLevel>high</NuGetAuditLevel>
<WarningsAsErrors>$(WarningsAsErrors);NU1903;NU1904</WarningsAsErrors>
\`\`\`

- \`NU1903\`: a dependency has a known **high-severity** vulnerability.
- \`NU1904\`: a dependency has a known **critical-severity** vulnerability.
- Both warnings are treated as errors, so CI fails and blocks automatic deployment.
- Low and moderate advisories are not blocking under the current policy; review them separately.

**Pipeline configuration:** Both application and test dependencies are restored before build. The vulnerability audit runs during each restore, including for transitive packages.

\`\`\`bash
dotnet restore AzureCapacityService/AzureCapacityService.csproj
dotnet restore AzureCapacityService.Tests/AzureCapacityService.Tests.csproj
\`\`\`

To inspect vulnerable packages locally with the .NET 10 CLI:

\`\`\`bash
dotnet package list --project AzureCapacityService/AzureCapacityService.csproj --vulnerable --include-transitive
dotnet package list --project AzureCapacityService.Tests/AzureCapacityService.Tests.csproj --vulnerable --include-transitive
\`\`\`

**When a vulnerability is detected:** Identify the affected direct or transitive package, update to a fixed compatible version, run tests, and repeat the audit. If no fix exists, document the risk and mitigation rather than suppressing the warning without review.

> NuGetAudit needs access to advisory information from configured NuGet sources. It only detects known reported vulnerabilities; it does not replace application security testing.

## 3. Unit and integration tests as CI quality gates

- **Unit tests:** \`dotnet test ... --filter "Category!=Integration"\` runs existing \`QuotaServiceTests\` without Azure access.
- **Integration tests:** \`dotnet test ... --filter "Category=Integration"\` runs the ASP.NET Core HTTP pipeline tests and the Key Vault service wiring test.
- **Key Vault integration test:** Creates the actual application DI container and \`KeyVaultService\`, but replaces the external Azure SDK \`SecretClient\` call with a mock. This verifies service registration and SDK interaction **without** requiring live Azure credentials, a provisioned secret, or network access. It is **not** a live Key Vault end-to-end test.
- **HTTP integration tests:** Use \`WebApplicationFactory\` to exercise routing, dependency injection, controllers, and JSON serialization with an isolated quota-service dependency.
- Test reports are uploaded as GitHub Actions artifacts.

A failing test, secret scan, or high/critical dependency audit makes CI unsuccessful. The CD workflow is configured to deploy on \`main\` only after successful CI. **Note:** the separate manual \`workflow_dispatch\` trigger in CD is an explicit bypass of that automatic CI gate; production deployments should use environment approvals and restrict manual dispatch permissions.

## 4. Further hardening

- Use OIDC federation for Azure login rather than client secrets.
- Keep Azure RBAC permissions scoped to the minimum required resources.
- Require PR reviews and successful CI checks before merging into \`main\`.
- Enable Dependabot updates for NuGet and GitHub Actions, if desired.
- Pin third-party GitHub Actions to reviewed commit SHAs for stronger supply-chain protection.
- For a **live** Key Vault integration test, use a dedicated test vault and federated test identity with read-only secret permissions, and seed a non-sensitive test secret out of band. Do not put test credentials in source code.

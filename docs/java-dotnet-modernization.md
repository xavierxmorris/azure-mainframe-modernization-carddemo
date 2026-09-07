# AI-assisted modernization to Java and .NET

Reviewed: **7 September 2026**. Scope: this repository's preserved mainframe
corpus, existing read-only .NET application, and a small Java numeric example.

## Best fit for CardDemo

**Keep ASP.NET Core 10 as the primary reference implementation.** It already
has the parser, safe domain views, routes, tests, container, and Bicep. An
additional Java application is worthwhile when the target team owns a JVM
estate or needs a genuine comparison, not merely because AI can generate it.

For a Java service, use a maintained **JDK 25 LTS** distribution and a supported
Spring Boot 4.1.x patch after dependency assessment. The Spring requirements
page reported **4.1.1** on this review date. For .NET, use **.NET 10 LTS**;
.NET 8 support ends on 10 November 2026.

Do not adopt microservices, AKS, a vector database, or an AI runtime as a
prerequisite for moving deterministic COBOL behavior. Keep the existing
Container Apps hosting for this small stateless slice. AI assists development;
it does not calculate account balances at runtime.

## Start with the existing .NET example

```powershell
dotnet test .\CardDemo.Azure.slnx --configuration Release
dotnet run --project .\src\CardDemo.Modern
```

Open the locally printed URL. The existing `/api/accounts/{id}` endpoint and
Razor pages are read-only data exploration, not a replacement for a CICS
transaction's authorization, locking, or commit/recovery behavior.

Read `docs/azure-migration-assessment.md` and `AGENTS.md` before extending it.
The full local gate remains `.\scripts\validate-modern.ps1`; it does not
deploy resources.

### Reuse the same API contract against a running instance

```powershell
.\scripts\test-http-contract.ps1 -BaseUrl http://127.0.0.1:8080
```

For an authorized existing Azure demo, supply its HTTPS origin instead. The
script warms the app, performs locked restore, and runs `AppRoutesTests`
against that instance. The tests compare all 50 sample account views and their
linked cards, customers, and transactions with the local read-only reference,
and exercise masking, safe errors, search validation, and response headers.

These are bounded, sequential **GET-only** checks, not a load test. Use only a
trusted instance containing the checked-in synthetic dataset, never customer
or production records. Redirects are disabled; responses and request time are
bounded. HTTPS is required except for loopback HTTP.

The script sets `CARDDEMO_TEST_BASE_URL` only for its process and restores the
previous value afterward. With that variable unset, the ordinary .NET suite
continues to use its in-process test host. The full local gate and container
CI also run this contract against the real production image.

## Runnable Java example: the same signed-overpunch contract

`app/cpy/CVACT01Y.cpy` declares a 300-byte account record. After the 11-digit
account ID and one-character status, `ACCT-CURR-BAL` is `PIC S9(10)V99`:
zero-based offset **12**, width **12**, implied scale **2**.

The checked-in ASCII export uses trailing signed overpunch. For example,
`00000001940{` decodes to `194.00`, and `00000000000J` decodes to `-0.01`.
This is not a decoder for arbitrary EBCDIC bytes or packed `COMP-3` data.

| Artifact | Role |
| --- | --- |
| `examples/contracts/signed-overpunch.txt` | Shared synthetic positive, negative, zero, scale, overflow, and invalid-input cases |
| `examples/java/OverpunchContract.java` | Independent Java decoder and executable contract check |
| `src/CardDemo.Modern/Services/FixedWidth.cs` | Existing .NET decoder, unchanged |
| `tests/CardDemo.Modern.Tests/SharedOverpunchContractTests.cs` | Runs the same fixtures against the existing .NET decoder |
| `scripts/check-java-contract.ps1` | Compiles with JDK 25 and fails on any contract mismatch |

With JDK 25 and .NET 10 installed:

```powershell
.\scripts\check-java-contract.ps1
dotnet test .\CardDemo.Azure.slnx --configuration Release `
  --filter "FullyQualifiedName~SharedOverpunchContractTests"
```

Without a local JDK, run the Java example in a Linux container:

```powershell
$repo = (Get-Location).Path
docker run --rm --mount "type=bind,source=$repo,target=/work" --workdir /work `
  eclipse-temurin:25-jdk-noble sh -c `
  "mkdir -p build/java-contract && javac --release 25 -encoding UTF-8 -Xlint:all -Werror -d build/java-contract examples/java/OverpunchContract.java && java -cp build/java-contract OverpunchContract examples/contracts/signed-overpunch.txt"
```

Both checks consume one fixture file, so a second implementation cannot
quietly carry a different expected answer. Java uses `BigDecimal` with an
explicit scale; .NET uses `decimal`. Both first enforce the existing parser's
signed-64-bit magnitude boundary and reject unsupported formats.

**Evidence limit:** these are authored numeric contract fixtures, not recorded
CICS transactions. Agreement proves this boundary contract for these inputs,
not full CardDemo parity, a working Java web service, or production readiness.
There are no new HTTP routes or changes to the deployed application.

## Extend one vertical slice, not the whole estate

| Stage | Java target | .NET target | Exit condition |
| --- | --- | --- | --- |
| Record interpretation | Typed records, explicit `BigDecimal` scale/bounds | Existing records and `FixedWidth` methods | Shared fixtures cover offsets, numeric representation, malformed fields, and transport lengths |
| Read-only inquiry | Small Spring Boot host around a reviewed domain service | Existing ASP.NET Core API and Razor pages | Field-level comparison, masking/omission checks, missing-account behavior, and bounded pagination |
| Relational persistence | JDBC/JPA only after schema and transaction design | EF Core or SQL client after the same design | Reconciliation, constraints, duplicate handling, and source-of-truth ownership established |
| Transaction writes | Explicit application transaction boundary | Explicit application transaction boundary | Identity, authorization, idempotency, audit, concurrency, and recovery approved and exercised |
| Batch and messaging | Job/consumer adapters around pure domain operations | Worker/job adapters around pure domain operations | Restart/checkpoint, retries, ordering, dead-letter, and duplicate semantics demonstrated |
| Cutover | Chosen host behind controlled routing | Chosen host behind controlled routing | Shadow comparison, agreed reconciliation thresholds, canary, operational readiness, and rollback rehearsed |

Do not write to both legacy and modern systems independently and call that
parity. A write migration needs one authoritative writer and a designed
replication/reconciliation strategy.

## Mapping rules that AI must not guess

| Mainframe concern | Required decision |
| --- | --- |
| IDs such as `PIC 9(11)` | Keep identifiers as strings at the API boundary so leading zeros survive |
| Display numeric, overpunch, `COMP`, `COMP-3` | Identify encoding, sign, byte representation, precision, and scale separately |
| Decimal arithmetic | Use integer minor units when the scale is fixed; otherwise constrained `BigDecimal` / `decimal`, never binary floating point |
| `REDEFINES`, `OCCURS`, `OCCURS DEPENDING ON` | Model layout/variant/count explicitly and test invalid lengths |
| Space, low-values, empty, and null | Define distinct source and API meanings; do not normalize them without evidence |
| CICS units of work / VSAM locks | Design transaction and concurrency behavior, not just REST endpoints |
| Db2 / IMS / MQ | Preserve commit, isolation, retry, ordering, and failure semantics explicitly |
| JCL / scheduler state | Preserve restartability and job completion dependencies, not sleeps or optimistic sequencing |
| RACF and sensitive-shaped fields | Design modern authorization; retain masking and omit CVV, SSN, government IDs, and raw records |

Language selection is usually less risky than getting these decisions wrong.
Use the original corpus for traceability but only approved synthetic data
when generating examples or calling external documentation tools.

## Copilot setup and a bounded example task

Use VS Code with Copilot, COBOL navigation, Java tooling, and C# Dev Kit.
Run Linux-sensitive checks in containers. Existing analyst, implementer,
reviewer, skill, and prompt files remain the primary task context.

The [portfolio setup and executable accounting lab](https://github.com/xavierxmorris/ghcp-demo-13-modernize-legacy-cobol-app/blob/main/docs/JAVA-DOTNET-MODERNIZATION.md)
provides a multi-language container and a complete small behavioral port.
It is a good rehearsal before working on a CICS/VSAM-dependent transaction.

Use [compare-java-dotnet.prompt.md](../.github/prompts/compare-java-dotnet.prompt.md)
or paste:

```text
Read AGENTS.md, CVACT01Y.cpy, FixedWidth.cs, and the shared overpunch fixtures.
Trace ACCT-CURR-BAL's offset, width, sign representation, scale, and limits.
Compare the Java decoder with the existing .NET implementation. Add only
synthetic contract cases for a specific uncovered boundary and explain their
source. Do not change legacy assets, normalize malformed values to zero,
expose raw records, add write routes, or claim CICS parity from parser tests.
Run both contract checks and report remaining coverage gaps.
```

Use the documented Java/.NET upgrade tools **after** a supported application
exists. The Java plugin expects a build project such as Maven/Gradle; this
standalone Java numeric example is intentionally smaller. The current
supported-language list does not document COBOL language conversion.

## Sources

| Source | Currency |
| --- | --- |
| [OpenJDK 25](https://openjdk.org/projects/jdk/25/) | GA 16 September 2025; LTS support is vendor-specific |
| [Spring Boot requirements](https://docs.spring.io/spring-boot/system-requirements.html) | Reported 4.1.1, Java 17-26, on 7 September 2026 |
| [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | Updated 11 August 2026; .NET 10 LTS through 14 November 2028 |
| [Copilot modernization language scope](https://learn.microsoft.com/azure/developer/github-copilot-app-modernization/languages) | Reviewed 7 September 2026 |
| [Java modernization in Copilot CLI](https://learn.microsoft.com/azure/developer/java/migration/github-copilot-app-modernization-for-java-copilot-cli) | Plugin prerequisites and workflow reviewed 7 September 2026 |
| [.NET upgrade workflow](https://learn.microsoft.com/dotnet/core/porting/how-to-upgrade-with-github-copilot) | Host-specific entry points reviewed 7 September 2026 |

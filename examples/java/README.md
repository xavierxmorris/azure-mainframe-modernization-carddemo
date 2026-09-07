# Java signed-overpunch example

This is a JDK 25 standard-library example, not a Java rewrite of CardDemo.
It decodes synthetic ASCII trailing-overpunch fields and checks the same
fixtures as the existing .NET parser tests.

From the repository root:

```powershell
.\scripts\check-java-contract.ps1
dotnet test .\CardDemo.Azure.slnx --configuration Release `
  --filter "FullyQualifiedName~SharedOverpunchContractTests"
```

See [the Java/.NET modernization path](../../docs/java-dotnet-modernization.md)
for a Docker alternative, copybook mapping, implementation limits, and the
next read-only slice. Do not feed this example raw EBCDIC or packed-decimal
records; their decoding is a separate boundary.

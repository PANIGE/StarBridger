# StarBridger Telemetry — first gRPC source

This starter connects to the experimental **Star Citizen** local gRPC service at `http://127.0.0.1:2942`, streams `GameTelemetryService/TelemetryStream`, maps the received Vehicle / Actor / Weapons frames into the **existing StarBridger.Contracts.Telemetry** records, and publishes them into three `StateStore<T>` instances.

## Provenance and verification

Schema layout was checked against: https://github.com/Sokren/sc-telemetry-deck/blob/main/schema/sc_extrait.proto . The `.proto` in this starter was validated with `protoc` 3.13 using the `--experimental_allow_proto3_optional` flag, and generated C# property names were checked against the mapper. **A complete .NET build and a live connection were not possible in the artifact environment**; run `dotnet build` in your own StarBridger solution.

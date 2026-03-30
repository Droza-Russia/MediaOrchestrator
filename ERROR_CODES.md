# MediaOrchestrator Error Codes

This document describes the public error-code contract exposed by `MediaOrchestrator`.

Every `MediaOrchestratorException` provides:
- `ErrorCode`
- `ErrorCodeId`

Example:

```csharp
catch (MediaOrchestratorException ex)
{
    Console.WriteLine(ex.ErrorCode);   // AudioStreamNotFound
    Console.WriteLine(ex.ErrorCodeId); // MOR-IN-3007
}
```

## Prefix Semantics

- `MOR` - MediaOrchestrator
- `GN` - generic fallback
- `CV` - conversion and media-processing execution
- `IN` - input validation and input media structure
- `IO` - output path, output directory and disk write conditions
- `HW` - hardware acceleration
- `TL` - toolchain discovery and executable validation
- `HD` - hosted-video download
- `OP` - operation lifecycle such as cancellation

Format:

```text
MOR-<DOMAIN>-<NUMBER>
```

## Catalog

### Generic

| Code | Enum | Meaning |
|---|---|---|
| `MOR-GN-0000` | `Unknown` | Fallback for unclassified library errors. |

### Conversion

| Code | Enum | Meaning |
|---|---|---|
| `MOR-CV-1000` | `ConversionFailed` | Generic conversion failure. |
| `MOR-CV-1001` | `UnknownDecoder` | No suitable decoder could be resolved for the input. |
| `MOR-CV-1003` | `NoSuitableOutputFormat` | No suitable output container could be determined. |
| `MOR-CV-1004` | `InvalidBitstreamFilter` | The requested bitstream filter is unsupported. |
| `MOR-CV-1005` | `StreamMappingFailed` | Requested stream mapping could not be satisfied. |
| `MOR-CV-1006` | `StreamCodecNotSupported` | The selected codec is unsupported for the operation or container. |

### Toolchain

| Code | Enum | Meaning |
|---|---|---|
| `MOR-TL-2000` | `ExecutableNotFound` | Required media executables were not found. |
| `MOR-TL-2001` | `ExecutablesPathAccessDenied` | The configured executables directory is not accessible. |
| `MOR-TL-2002` | `ExecutableSignatureMismatch` | Located executable does not match the expected platform signature. |

### Input

| Code | Enum | Meaning |
|---|---|---|
| `MOR-IN-3000` | `InvalidInput` | Generic invalid input. |
| `MOR-IN-3001` | `InputFileUnreadable` | Input file cannot be read. |
| `MOR-IN-3002` | `InputPathAccessDenied` | Input path access was denied. |
| `MOR-IN-3003` | `InputFileEmpty` | Input file is empty. |
| `MOR-IN-3004` | `InputFileLocked` | Input file is locked by another process. |
| `MOR-IN-3005` | `InputFileStillBeingWritten` | Input file is still being written and is not stable yet. |
| `MOR-IN-3006` | `InputFileSignatureMismatch` | File signature does not match the expected media type. |
| `MOR-IN-3007` | `AudioStreamNotFound` | Expected audio stream is missing. |
| `MOR-IN-3008` | `VideoStreamNotFound` | Expected video stream is missing. |
| `MOR-IN-3009` | `SubtitleStreamNotFound` | Expected subtitle stream is missing. |
| `MOR-IN-3010` | `StreamIndexOutOfRange` | Requested stream index is outside the available range. |

### Output And Disk I/O

| Code | Enum | Meaning |
|---|---|---|
| `MOR-IO-4000` | `OutputPathAccessDenied` | Output path access was denied. |
| `MOR-IO-4001` | `OutputDirectoryNotWritable` | Output directory is not writable. |
| `MOR-IO-4002` | `InsufficientDiskSpace` | Disk space is insufficient to complete the write. |

### Hosted Download

| Code | Enum | Meaning |
|---|---|---|
| `MOR-HD-5000` | `HostedVideoDownloadFailed` | External hosted-video download failed. |

### Hardware

| Code | Enum | Meaning |
|---|---|---|
| `MOR-HW-6000` | `HardwareAcceleratorNotFound` | Requested hardware accelerator is not available. |

### Operation Lifecycle

| Code | Enum | Meaning |
|---|---|---|
| `MOR-OP-9000` | `OperationCanceled` | The operation was canceled. |

## How Reports Use Error Codes

Analytics reports expose failures through:
- `ByErrorCode`
- `ByFailureType`
- `ByFailureCategory`

This makes it possible to build:
- Grafana panels grouped by stable code;
- support dashboards grouped by exception type;
- aggregated operational reporting grouped by failure category.

Recommended usage:
- use `ErrorCodeId` for UI, reports and documentation links;
- use `ErrorCode` enum inside code and switch statements;
- use `ByErrorCode` in dashboards because it is stable across refactors more often than exception type names.

## Recommended Documentation Mapping

For external documentation or support portals, use a route pattern like:

```text
/docs/errors/MOR-IN-3007
```

or

```text
/errors/MOR-CV-1001
```

This keeps support links stable even if internal class names evolve later.

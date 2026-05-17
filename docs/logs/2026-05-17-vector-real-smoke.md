# 2026-05-17 Vector Real Smoke

## Context

M5's parser matrix still listed Vector / Convex2D as implemented but needing
real-client validation beyond the early UI/Map notes. Convex2D remains covered
by synthetic fixtures only until a concrete client sample is found, but the
local GMS install has stable Vector anchors in both WZ and MS/MN-style image
payload paths.

## Changes

- Added optional external-client Core smoke for
  `Data/Character/Character_000.wz`, selector `00002000.img`, validating
  `walk1/0/body/origin` and `walk1/0/arm/origin` Vector values and identity
  value paths.
- Added optional external-client Core smoke for `Data/Packs/Mob_00000.ms`,
  selector `Mob/1150000.img`, validating `move/0/origin`, `lt`, `rb`, and
  `head` Vector values through the MS image payload extraction path.
- Updated parser coverage notes to distinguish real Vector smoke from the still
  fixture-only Convex2D real-sample gap.

## Validation

Run the targeted smoke with a local client `Data` directory:

```bash
WCX_CLIENT_DATA_DIR=/path/to/Data dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter "FullyQualifiedName~InspectOptionalExternalClientCharacterImage_ReadsVectorAnchors|FullyQualifiedName~InspectOptionalExternalClientMsPackImage_ReadsVectorAnchors"
```

The targeted smoke passed against the local GMS client. Full solution build/test
and the standard App Headless subset also passed before commit.

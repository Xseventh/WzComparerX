# Parser Coverage And Resource Model Matrix

This document is the working checklist for Milestone 5. The high-level roadmap
matrix answers "which feature family owns this"; this matrix answers "what
parser or resource-model slice should we validate next".

WC remains the behavior reference. WCX should keep the implementation layered:
WzLib owns binary format knowledge, Core owns resource identity / inspection /
diagnostics contracts, and App consumes Core services.

## Priority Scale

- P0: blocks Compare/Search/export correctness or common real-client browsing.
- P1: important for broad real-client coverage, but can follow the P0 slices.
- P2: useful compatibility or polish, not required before M6.

## Package Formats

| Area | WC reference behavior | WCX current state | Gap | Priority | Validation path |
| --- | --- | --- | --- | --- | --- |
| PKG1 header | Detect PKG1, encrypted version, header size, version/hash profile | Implemented and tested | Keep regression coverage for boundary offsets and missing encrypted version | P0 | Synthetic WzLib tests plus local `Base/Base.wz` smoke |
| PKG1 directory entries | Recursive directories, string-reference names, checksums, offsets | Implemented for current GMS smoke path | Add more edge-case fixtures for malformed offsets, duplicate names, large tables | P0 | Synthetic fixtures, `inspect --debug`, local Map/UI/String packages |
| PKG1 package groups | `Name.ini` / `LastWzIndex` plus numbered shard merge | Implemented in Core package group layer | Add stable diagnostics for missing or inconsistent shard links | P0 | Core temp-fixture tests plus local `Map1.wz` smoke |
| Base package links | Empty stubs link into sibling package folders | Implemented conservatively for Base-style workspace and same-package relative links | Clarify diagnostics for unresolved stubs and preserve true source identity | P0 | Core tests plus local `Base.wz`, `UI.wz`, `Map.wz` smoke |
| PKG2 header | Detect PKG2 and read header hash fields | Header-only | Parse at least one representative directory shape or document blocker | P0 | WC reference review, synthetic fixture if possible, real-client smoke if sample exists |
| List.wz / optional containers | WC supports older/newer container helpers | Not implemented | Decide whether current target clients require List.wz, `.ms`, or `.mn` | P1 | Local-client scan notes, no code until sample-driven |

## IMG Values

| Area | WC reference behavior | WCX current state | Gap | Priority | Validation path |
| --- | --- | --- | --- | --- | --- |
| IMG root object | Lazy extract selected IMG, then full object tree | Implemented for supported object families | Keep App lazy/full flow aligned with Core inspection defaults | P0 | App tests and local GMS UI smoke |
| Property traversal | Nested property tree with scalar values | Implemented with caller depth; UI uses full inspection | Add more deterministic fixtures for unusual scalar encodings and empty properties | P0 | WzLib/Core fixtures, CLI golden output |
| Vector / Convex2D | Decode geometry values and nested components | Metadata/value inspection implemented | Validate real-client cases beyond UI/Map samples | P1 | `inspect --debug` smoke notes |
| UOL / link strings | Resolve links where possible; display unresolved links | UOL inspected mostly as value metadata; Canvas string links partially resolve in App/Core preview path | Move link target semantics into Core inspection metadata and diagnostics | P0 | Core link fixtures, App preview tests, local Map/UI smoke |
| Canvas metadata | Width, height, format, scale, payload metadata | Implemented | Add unsupported-format diagnostics where payload metadata is insufficient | P0 | Synthetic Canvas fixtures and local UI/Map smoke |
| Sound / Video / RawData | Metadata plus payload extraction/playback/export in WC | Metadata only | Preserve payload offsets/lengths and stable unsupported diagnostics before media export | P1 | Local Sound/UI smoke and export diagnostics tests |
| Lua IMG | Lua-specific stream decode and script export | Synthetic fixture support; no direct local real-sample entry yet | Find real sample or keep documented as fixture-only | P1 | Fixture tests, future local smoke |
| Text-format IMG | WC text IMG v1/v2 parse/export | Synthetic fixture support; no direct local real-sample entry yet | Find real sample; add multiline v2 behavior if needed | P1 | Fixture tests, future local smoke |

## Resource Identity Contract

| Area | WC reference behavior | WCX current state | Gap | Priority | Validation path |
| --- | --- | --- | --- | --- | --- |
| Node path | Tree nodes are navigable by stable WZ-style paths | `ResourceInspectionNode.Path` is present | Define exact semantics for package path, image selector, and value path separately | P0 | Core model tests and CLI JSON golden output |
| Source package | Split/merged image nodes know original package | Core preserves target paths for merged shard images | Make source package explicit enough for Compare/Search/export consumers | P0 | Core package-group tests |
| Image selector | Selected IMG can be reloaded without UI path guessing | Implemented through selector/path normalization | Document and test selector behavior across direct package, merged shard, and linked package | P0 | Core/App tests |
| Value path | Canvas export and preview use explicit property paths | Implemented for Canvas export/preview paths | Promote value path to stable inspection metadata where useful | P0 | Export/App tests |
| Linked target | `source`, `_inlink`, `_outlink`, UOL targets should be represented | Canvas preview can resolve selected link strings | Add Core-level linked-target metadata and unresolved-link diagnostics | P0 | Core fixtures plus App preview tests |
| Diagnostics | Stable severity/source/code/path | Implemented for many parser/export cases | Add diagnostics for unresolved package links, unsupported link targets, and PKG2 blockers | P0 | Unit tests and CLI golden output |

## Real-Client Smoke Matrix

| Package family | Current coverage | Next M5 validation |
| --- | --- | --- |
| Base | Header, top-level directory, Base-style package links | Record unresolved-stub diagnostics once implemented |
| Map | M4 UI smoke covers `Map1.wz` group merge and linked Canvas preview | Add CLI/Core smoke note for package group resource identity |
| UI | Directory and Canvas metadata smoke exists | Add link diagnostics smoke for `_Canvas` packages |
| Item / Character / Skill | Basic browse coverage only through Base links if opened manually | Add representative `inspect --debug` notes for IMG value families |
| String | Basic directory coverage through Base links | Use as early StringLinker/Search input after M5 identity stabilizes |
| Mob / Npc | Basic browse coverage only | Add representative IMG/property smoke notes |
| Sound | Sound_DX8 metadata smoke exists | Preserve payload diagnostics before M8 export |
| Effect | Base link path coverage | Add Canvas/link smoke if suitable sample is found |
| Lua / text IMG | Synthetic only | Find a direct local sample or keep fixture-only status explicit |

## Immediate M5 Slices

1. Tighten resource identity in Core inspection output: source package, image
   selector, value path, and linked target should be unambiguous for future
   Compare/Search consumers.
2. Add stable diagnostics for unresolved split-package and Canvas/link targets.
3. Choose the first PKG2 directory parsing slice or document the exact blocker.
4. Add representative real-client smoke notes for Map, UI, Item/Character,
   String, Mob/Npc, Skill, Sound, and Effect without committing client files.

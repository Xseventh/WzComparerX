# Resource Identity Contract

This document defines the current M5 identity contract for
`ResourceInspectionNode`. The goal is to keep CLI, UI, export, search, and
future compare workflows from guessing resource targets from display text.

## Node Path

`ResourceInspectionNode.Path` is the stable tree/display path for the node in
the current inspection document. It is useful for UI selection, diagnostics,
and text output, but callers should not parse it to recover filesystem targets.

For merged package groups, an image node path may include its source package,
for example:

```text
/Data/Map/Map1/Map1_000.wz/100000000.img
```

Use `Identity` fields for automation and cross-service calls.

## Identity Fields

`ResourceInspectionIdentity` is optional because synthetic fixtures and
non-resource helper nodes may not have a backing WZ target.

- `PackagePath`: package file that owns this node. For merged numbered shards,
  this is the true shard package, not necessarily the entry package displayed at
  the root of the current inspection tree.
- `ImageSelector`: IMG selector that can be passed back to Core together with
  `PackagePath` to inspect or export the selected IMG. It is set for image
  nodes and nodes inside an IMG.
- `ValuePath`: inside-IMG property/value path. It is set for IMG content nodes
  such as `info/name`, `icon`, or `miniMap/canvas`. It is null for package and
  image-root nodes.
- `LinkedTarget`: normalized logical target string for link-like IMG values
  such as `source`, `_inlink`, `_outlink`, `link`, and UOL. Backslashes are
  normalized to `/`. This field records the target text; it does not guarantee
  that the target has been resolved to an existing package/value.

## Resolution Boundary

Core inspection records raw identity and normalized link targets. It does not
currently rewrite link-like nodes to a resolved target identity during ordinary
inspection.

Canvas preview performs a narrower resolution pass when a selected Canvas link
needs to be displayed. Failed viewer resolution is reported as
`wcx.viewer.canvas.linkUnresolved`.

Future search/compare work should extend the model with resolved target
identity only when that behavior is shared by multiple workflows and covered by
tests.

## CLI JSON

`inspect --debug --json` is the stable CLI surface for identity records. Text
format is primarily human-readable; debug text may include selected identity
facts as metadata, but automation should prefer JSON.

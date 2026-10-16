# Working in this repository

Read README.md and the top three CHANGELOG.md entries. The maintained MSTest reference is ../mstest-samples-with-docs-4.5.1/SKILL.md; read it in place, without installing or duplicating it.

Use native MSTest 4.5.1 and Microsoft.Testing.Platform 2.5.1. Preserve original test attributes, discovery locations and result objects. Consumer global lifecycle hooks must live in the consuming test assembly.

Keep generated OTLP DTOs internal and regenerate from the pinned official proto source. Preserve typed values, all resource/scope metadata, events, links and optional-field presence. Do not filter captured records. Trace queries may select their trace; metric attribution needs an explicit resource partition or matching exemplar.

Edit existing code and tests directly. No test seams, blanket suppressions, coverage exclusions, fake APIs or empty-selection passes. Review every changed test's oracle and fixture lifetime. Run the smallest relevant native check; release gates additionally require ./build.sh Pack with both 100% coverage thresholds and zero INFO-or-higher inspection findings.

Commit and push completed verified work directly to main. Do not publish an unpublished dependency, configure a private feed in tracked files, or report a local candidate as publicly available. Release through ./release.sh only; never print a credential value. Keep unrelated owner changes intact.

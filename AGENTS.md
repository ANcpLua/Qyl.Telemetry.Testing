# Working in this repository

Read README.md and the top three CHANGELOG.md entries. For changes to MSTest extensions, use the maintained
../mstest-samples-with-docs-4.5.1/SKILL.md in place. Use the versions resolved by this repository; currently
MSTest 4.5.1 and Microsoft.Testing.Platform 2.5.1. Preserve original attributes, results and discovery locations.
Consumer global lifecycle hooks belong in the consuming test assembly.

Keep generated OTLP DTOs internal and regenerate from the pinned official proto source. Preserve original payloads,
typed values, resource/scope metadata, events, links and optional-field presence. Never filter raw captures.
Trace selection is explicit; metric attribution requires a resource partition or matching exemplar. Persistence policy
belongs to the production Collector: updating SemanticConventions must not apply its key denials to this receiver.

Edit existing code and tests directly. No artificial test seams, blanket suppressions, coverage exclusions, fake APIs
or empty-evidence passes. Review changed tests' oracles and resource ownership. Run the smallest relevant native check;
release gates additionally require ./build.sh Pack, all native tests passing, unfiltered coverage and zero INFO-or-higher
inspection findings. Coverage is informational: prioritize business rules and invariants, leaving dependency and
generated-code gaps visible instead of adding tests that merely touch members.

Commit and push completed work directly to main. Preserve unrelated owner changes. Consume indexed dependencies for
publication; never track private feeds or call a local candidate publicly available. Release through ./release.sh;
never print credentials. Keep release snapshots and test-review evidence in artifacts/ and eng/ rather than adding
handoff or review narratives to the package README.

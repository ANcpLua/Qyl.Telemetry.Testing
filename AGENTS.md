# Qyl.Telemetry.Testing

Canonical in-process OTLP receiver and assertions. README.md documents public APIs; build.sh lists targets and release.sh owns publication. Generated DTOs come from the pinned official proto. Preserve raw payloads, typed values and field presence. Metric attribution needs a resource partition or exemplar. Persistence policy belongs to qyl's Collector, not this receiver. Consumer lifecycle hooks stay in the consuming test assembly.

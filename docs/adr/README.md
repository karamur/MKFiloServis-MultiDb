# Architecture Decision Records (ADR)

MKFiloServis projesi için mimari karar kayıtları.

| ADR | Başlık | Status | Sprint | Tarih |
|-----|--------|:------:|:------:|-------|
| [0001](0001-operasyon-puantaj-iki-katmanli-mimari.md) | OperasyonKaydi + PuantajKayit İki Katmanlı Mimari | Accepted | S1 | 2026-05-25 |
| [0002](0002-puantaj-engine-revizyon-detay-mimarisi.md) | PuantajEngine V1 — Revizyon + Detay + Audit | Accepted | S3 | 2026-05-25 |
| [0003](0003-workflow-approval-state-machine.md) | Workflow + Approval State Machine | Accepted | S5 | 2026-05-25 |
| [0004](0004-finansal-cikti-fatura-snapshot-koprusu.md) | Finansal Çıktı — Fatura + Snapshot Köprüsü | Accepted | S6 | 2026-05-25 |
| [0005](0005-multi-tenant-database-per-firma.md) | Multi-Tenant Database-Per-Firma Mimarisi | Accepted | — | 2026-05-25 |
| [0006](0006-puantaj-quartz-automation.md) | Puantaj Engine Quartz Automation | Accepted | S8 | 2026-05-25 |
| [0007](0007-background-puantaj-processing.md) | Background Puantaj Processing Architecture | Accepted | S8 | 2026-05-26 |
| [0008](0008-production-hardening-review.md) | Production Hardening Review | Active Review | S8 | 2026-05-26 |
| [0009](0009-production-remediation-plan.md) | Production Remediation Plan | Plan | S8 | 2026-05-26 |
| [0010](0010-observability-architecture.md) | Observability Architecture | Plan | S8 | 2026-05-26 |
| [0011](0011-production-readiness-final.md) | Production Readiness Final Assessment | Conditional Go | S8 | 2026-05-26 |

## Format

Her ADR şu başlıkları içerir:
- **Status**: Proposed → Accepted → Deprecated → Superseded
- **Context**: Problemin iş ihtiyacı ve teknik bağlamı
- **Decision**: Alınan mimari karar
- **Consequences**: Avantajlar, dezavantajlar, riskler
- **Alternatives Considered**: Değerlendirilen ama seçilmeyen alternatifler
- **Related Components**: Entity, service, UI, migration, workflow
- **Migration Impact**: DB, existing code, backward compatibility
- **Verification Checklist**: Doğrulama kriterleri

## Superseded ADR'ler

Henüz yok.


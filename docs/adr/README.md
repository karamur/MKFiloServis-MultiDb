# Architecture Decision Records (ADR)

KOAFiloServis projesi için mimari karar kayıtları.

| ADR | Başlık | Status | Sprint | Tarih |
|-----|--------|:------:|:------:|-------|
| [0001](0001-operasyon-puantaj-iki-katmanli-mimari.md) | OperasyonKaydi + PuantajKayit İki Katmanlı Mimari | Accepted | S1 | 2026-05-25 |
| [0002](0002-puantaj-engine-revizyon-detay-mimarisi.md) | PuantajEngine V1 — Revizyon + Detay + Audit | Accepted | S3 | 2026-05-25 |
| [0003](0003-workflow-approval-state-machine.md) | Workflow + Approval State Machine | Accepted | S5 | 2026-05-25 |
| [0004](0004-finansal-cikti-fatura-snapshot-koprusu.md) | Finansal Çıktı — Fatura + Snapshot Köprüsü | Accepted | S6 | 2026-05-25 |
| [0005](0005-multi-tenant-database-per-firma.md) | Multi-Tenant Database-Per-Firma Mimarisi | Accepted | — | 2026-05-25 |

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

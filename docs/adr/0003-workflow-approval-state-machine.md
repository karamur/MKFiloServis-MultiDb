# ADR-0003: Workflow + Approval State Machine

## Status
Accepted (2026-05-25)

## Context
Sprint 3'te PuantajEngine hesaplama yapıp `Aktif` duruma geçiyordu. Ancak onay süreci yoktu:
- `Aktif` olan dönem doğrudan revize edilebiliyordu
- Finans/Muhasebe onayı mekanizması yoktu
- Kilit mekanizması yoktu
- Her aksiyon audit log üretmiyordu

İş ihtiyacı:
- Onaylı dönem değiştirilememeli
- Finans onayı olmadan muhasebe onayı yapılamamalı
- Kilitli dönem revize edilmeden açılamamalı
- Her durum geçişi audit log üretmeli
- Rol bazlı yetkilendirme olmalı

## Decision
**2 eksenli state machine, minimal invasive yaklaşım:**

1. **Durum (hesap yaşam döngüsü)** — mevcut, korunuyor:
   `Taslak → Aktif → Superseded / Iptal`

2. **OnayDurum (onay zinciri)** — yeni:
   `Bekliyor → FinansOnaylandi → MuhasebeOnaylandi → Kilitli`

**Entity değişikliği**: Yeni entity yok. `PuantajHesapDonemi`'ye 7 onay alanı eklendi.

**Audit**: Hafif `PuantajAuditLog` entity'si. Her durum geçişinde otomatik oluşturulur.

**Authorization**: Service seviyesinde rol kontrolü. Admin her şeyi yapabilir. Finans rolü sadece FinansOnayı. Muhasebeci rolü sadece MuhasebeOnayı.

**Kilit kontrolü (çift katmanlı):**
- Engine: Kilitli dönem → revizyon hatası
- BusinessRules: Onaylı dönem → OperasyonKaydi değişikliği hatası

## Consequences

**Avantajlar:**
- Mevcut Durum enum'u korundu, breaking change yok
- Onay zinciri Durum'dan bağımsız, 2 eksenli tasarım
- Tek entity'ye 7 alan eklenerek yapıldı, yeni entity maliyeti yok
- Audit log otomatik, manuel müdahale gerektirmez

**Riskler:**
- OnayDurum sadece `Durum == Aktif` iken anlamlı → service seviyesinde validasyon
- Mevcut Aktif kayıtlar OnayDurum'suz → default Bekliyor, migration'da backfill

## Alternatives Considered
1. **Ayrı Onay entity'si**: Reddedildi — minimal invasive tercih edildi
2. **Durum enum genişletme**: Reddedildi — mevcut Durum değerlerini değiştirmek breaking change
3. **Workflow engine (Windows Workflow / Camunda)**: Reddedildi — altyapı maliyeti yüksek

## Related Components
- `PuantajHesapDonemi.cs` (+7 alan)
- `PuantajAuditLog.cs` (yeni)
- `PuantajWorkflowService.cs` (Web/Services)
- `PuantajEngineService.cs` (kilit kontrolü)
- `OperasyonKaydiBusinessRules.cs` (onay kontrolü)
- `OperasyonKaydiService.cs` (kilit kontrolü)
- `PuantajHesaplama.razor` (onay paneli)

## Migration Impact
- PuantajHesapDonemleri: 7 kolon ekle (nullable)
- Yeni tablo: PuantajAuditLogs
- Mevcut tablolarda değişiklik yok

## Verification Checklist
- [x] FinansOnayla: Bekliyor → FinansOnaylandi, audit log üretildi
- [x] MuhasebeOnayla: FinansOnaylandi → MuhasebeOnaylandi (sıralı zorunlu)
- [x] Kilitle: MuhasebeOnaylandi → Kilitli
- [x] KilitAc: Kilitli → MuhasebeOnaylandi
- [x] Kilitli dönem → revizyon engellendi (Engine hatası)
- [x] Onaylı dönem → operasyon değişikliği engellendi (BusinessRules hatası)
- [x] Audit log: her durum geçişinde kayıt
- [x] UI paneli: butonlar + audit log tablosu
- [x] Build: 0 hata, 0 uyarı
- [x] Test: 305/305 başarılı

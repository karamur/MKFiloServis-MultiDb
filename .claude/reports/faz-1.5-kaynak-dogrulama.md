# Faz 1.5 — PuantajFaturaRaporService Kaynak Doğrulama Raporu

**Commit**: `817fa11` + `e1045fc`
**Tarih**: 2026-06-18
**Durum**: SADECE ANALİZ — kod değişikliği yok

---

## 1. Servis Hangi Tabloyu Ana Kaynak Olarak Kullanıyor?

**Cevap: `PuantajKayit` (DbSet: `db.PuantajKayitlar`)**

Kanıt (PuantajFaturaRaporService.cs satır 174):
```csharp
var query = db.PuantajKayitlar
    .Where(x => x.Yil == request.Yil && x.Ay == request.Ay && !x.IsDeleted)
    .Where(x => x.OnayDurum == PuantajOnayDurum.Onaylandi);
```

`HakedisPuantaj` (DbSet: `db.HakedisPuantajlar`) **hiç kullanılmıyor.** Bu bilinçli bir karar.

---

## 2. Neden PuantajKayit Seçildi?

| Özellik | PuantajKayit | HakedisPuantaj | Sonuç |
|---------|-------------|----------------|--------|
| **KurumId** | ✅ Direkt kolon | ❌ YOK (Guzergah.KurumId üzerinden dolaylı) | PuantajKayit kazanır |
| **FaturaKesiciCariId** | ✅ Kim fatura kesecek | ❌ YOK | PuantajKayit kazanır |
| **OdemeYapilacakCariId** | ✅ Kime ödenecek | CariId (sadece tedarikçi) | PuantajKayit kazanır |
| **Gun01-Gun31** | ✅ 31 direkt kolon | HakedisPuantajDetay (JOIN gerek) | PuantajKayit kazanır |
| **KaynakTipi** | ✅ Kendi/Tedarikci | ❌ YOK (Arac uzerinden) | PuantajKayit kazanır |
| **OnayDurum** | ✅ Taslak/Onaylandı/Red | Durum (farklı enum) | Eşit |
| **Fatura durumu** | ✅ GelirFaturaKesildi, GiderFaturaAlindi | Faturalasti | PuantajKayit kazanır |
| **Versiyon** | ✅ HesapDonemiId + Versiyon | ❌ YOK | PuantajKayit kazanır |

---

## 3. HakedisPuantaj'daki Alanlar PuantajKayit'ta Karşılanıyor mu?

### Karşılaştırma Tablosu

| HakedisPuantaj Alanı | PuantajKayit Karşılığı | Birebir mi? | Not |
|---------------------|----------------------|------------|-----|
| `GelirBirimFiyat` | `BirimGelir` | ⚠️ FARKLI | PuantajKayit'ta birim başına; HakedisPuantaj'da günlük toplam |
| `GiderBirimFiyat` | `BirimGider` | ⚠️ FARKLI | Aynı şekilde |
| `GelirToplam` | `ToplamGelir` | ✅ | |
| `GiderToplam` | `ToplamGider` | ✅ | |
| `KdvTutari` (Gider KDV) | `GiderKdv20Tutari + GiderKdv10Tutari` | ✅ | PuantajKayit daha detaylı |
| `GelirKdvTutari` | `GelirKdvTutari` | ✅ | |
| `ToplamKesinti` | `GelirKesinti + GiderKesinti` | ⚠️ | HakedisPuantaj'da tek alan; PuantajKayit'ta gelir/gider ayrı |
| `TahsilEdilecekTutar` | `Alinacak` | ✅ | |
| `OdenecekTutar` | `Odenecek` | ✅ | |
| `ToplamSefer` | `Gun` (decimal) | ⚠️ | PuantajKayit'ta decimal, HakedisPuantaj'da int |
| `KdvOrani` (tek) | `GelirKdvOrani` + `GiderKdvOrani20/10` | ✅ DAHA İYİ | PuantajKayit çift KDV oranı destekler |
| `GunlukSeferSayisi` | `SeferSayisi` | ✅ | |
| `YonTipi` | `Yon` | ⚠️ FARKLI ENUM | PuantajYon vs YonTipi — değerler uyumlu |
| Günlük detay (1-31) | `Gun01-Gun31` (31 kolon) | ⚠️ FARKLI FORMAT | PuantajKayit wide-column, HakedisPuantaj normalized (Detay tablosu) |

### PuantajKayit'ta FAZLA olan alanlar (HakedisPuantaj'da YOK):

| Alan | Önem |
|------|------|
| `KurumId` / `Kurum` | 🔴 KRİTİK — Ağaç yapısı için zorunlu |
| `FaturaKesiciCariId` | 🔴 KRİTİK — Fatura kime kesilecek |
| `FaturaKesiciAdi` / `FaturaKesiciTelefon` | 🟡 — Excel kolonları |
| `OdemeYapilacakCariId` | 🟡 — Gider tarafı için |
| `KaynakTipi` (Kendi/Tedarikci) | 🟡 — Tedarikçi ağacı için |
| `FinansYonu` (Gelen/Giden) | 🟢 |
| `SoforOdemeTipi` | 🟢 |
| `GelirFaturaKesildi` / `GiderFaturaAlindi` | 🟡 — Durum takibi |
| `GelirFaturaNo` / `GiderFaturaNo` | 🟡 — Fatura takibi |
| `HesapDonemiId` / `Versiyon` | 🟢 — Audit |
| `GelirKdvOrani10/20`, `GiderKdvOrani10/20` | 🟡 — Çift KDV |

---

## 4. Veri Kaybı Riski Var mı?

### Risk 1: Sadece HakedisPuantaj'da olan kayıtlar 🔴

**Senaryo**: Kullanıcı `HakedisPuantaj` sayfasından direkt hakedis girer, Engine V1 kullanmaz. Bu kayıtlar `PuantajKayit` tablosunda YOKTUR.

**Etki**: Servis bu kayıtları **görmez**. Fatura hazırlık listesinde eksik kalem olur.

**Olasılık**: Kullanıcıların hangi giriş yöntemini kullandığına bağlı. Her iki tablo da aktif kullanılıyorsa risk YÜKSEK.

**Çözüm**: Servise HakedisPuantaj desteği eklenmeli (auto-merge veya UNION).

### Risk 2: PuantajKayit.Gun vs HakedisPuantaj.ToplamSefer ⚠️

PuantajKayit'ta `Gun` decimal (örn: 0.5, 22), HakedisPuantaj'da `ToplamSefer` int. DTO'da `ToplamSefer = (int)k.Gun` cast'i var. Decimal değerlerde veri kaybı olabilir.

### Risk 3: Cari alanlarının eksik olması 🟡

Serviste `CariId = k.FaturaKesiciCariId ?? k.OdemeYapilacakCariId` yapılıyor. Eğer ikisi de null ise Cari bilgisi tamamen eksik kalır. Excel'den gelen ham verilerde (`FaturaKesiciAdi`) string olarak durabilir.

### Risk 4: KurumId null olan kayıtlar 🟡

PuantajKayit'ta `KurumId` nullable. Eğer Excel'den KurumId eşleştirilemezse null kalır. Bu durumda "Kurum > Araç > Güzergah" ağacında bu kayıtlar "Kurum #null" altında gruplanır — anlamsız.

---

## 5. Ağaç Yapıları Doğrulaması

| Ağaç | Seviye 1 Kaynağı | Durum |
|------|-----------------|-------|
| CariAracGuzergah | `FaturaKesiciCariId` → Cari.Unvan | ⚠️ Cari null olabilir |
| KurumAracGuzergah | `KurumId` → Kurum.KurumAdi | ⚠️ KurumId null olabilir |
| TedarikciAracGuzergah | `OdemeYapilacakCariId` (sadece KaynakTipi=Tedarikci ise) | ⚠️ KaynakTipi filtresi gerek |
| KurumGuzergahArac | `KurumId` → Guzergah → Arac | ⚠️ Aynı risk |

---

## 6. Excel Kolon Karşılaştırması

| Excel Kolonu | DTO'da Karşılığı | Kaynak | Durum |
|-------------|-----------------|--------|-------|
| S.NO | — (UI'da) | Hesaplama | ✅ |
| GÜZERGAH | GuzergahAdi | ✅ | |
| GELİR | ToplamGelir | ✅ | |
| GİDER | ToplamGider | ✅ | |
| YÖN | YonTipi | ✅ | |
| PLAKA | Plaka | ✅ | |
| ŞOFÖR | SoforAdi | ✅ | |
| FATURA KESİLECEK CARİ | CariUnvan | ⚠️ Null olabilir |
| TELEFON | Telefon | ⚠️ Null olabilir |
| 1-31 GÜN | Gun01-Gun31 | ✅ | |
| TOPLAM | TahsilEdilecek/Odenecek | ✅ | |
| KDV/20 | Kdv20Tutar | ✅ | |
| KDV/10 | Kdv10Tutar | ⚠️ Çoğunlukla 0 |
| KESİNTİ | KesintiTutar | ✅ | |
| ÖDENECEK | Odenecek/TahsilEdilecek | ✅ | |

---

## 7. Debug/SQL Karşılaştırma Önerisi

Aynı dönem için iki kaynağı karşılaştırmak için:

```sql
-- PuantajKayit toplamı
SELECT COUNT(*) as KayitSayisi,
       SUM("ToplamGelir") as ToplamGelir,
       SUM("ToplamGider") as ToplamGider,
       SUM("GelirKdvTutari" + "GiderKdv20Tutari" + "GiderKdv10Tutari") as ToplamKdv,
       SUM("GelirKesinti" + "GiderKesinti") as ToplamKesinti
FROM "PuantajKayitlar"
WHERE "Yil" = 2026 AND "Ay" = 6 AND NOT "IsDeleted"
  AND "OnayDurum" = 2; -- Onaylandi

-- HakedisPuantaj toplamı
SELECT COUNT(*) as KayitSayisi,
       SUM("GelirToplam") as ToplamGelir,
       SUM("GiderToplam") as ToplamGider,
       SUM("GelirKdvTutari" + "KdvTutari") as ToplamKdv,
       SUM("ToplamKesinti") as ToplamKesinti
FROM "HakedisPuantajlar"
WHERE "Yil" = 2026 AND "Ay" = 6 AND NOT "IsDeleted"
  AND "Durum" >= 2; -- Onaylandi veya sonrası
```

Bu iki sorgunun sonuçları karşılaştırıldığında:
- Kayıt sayıları farklıysa → bazı kayıtlar sadece bir tabloda var
- Tutarlar farklıysa → hesaplama farkı var

---

## 8. Karar: Faz 2'ye Geçilebilir mi?

### EVET, aşağıdaki koşullarla:

1. **PuantajKayit primary kaynak olarak KALMALI** — KurumId, FaturaKesiciCariId, Gun01-Gun31 avantajları kritik.

2. **HakedisPuantaj secondary olarak EKLENMELİ** (Faz 2 öncesi):
   ```csharp
   // Servis her iki kaynaktan da okuyabilmeli
   var puantajKayitSource = await db.PuantajKayitlar.Where(...).ToListAsync();
   var hakedisSource = await db.HakedisPuantajlar.Where(...).ToListAsync();
   // Merge

   ```

3. **Minimum düzeltme (kod değişikliği gerekir)**:
   - `GetSatirlarAsync` ve `GetAgacAsync` içine HakedisPuantaj sorgusu ekle
   - `MapToSatirDto(HakedisPuantaj)` overload'u ekle
   - İki kaynağı merge et (Concat/Union)
   - DTO'ya `Kaynak` alanı zaten var — auto-set

4. **Düzeltme sonrası test**: Yukarıdaki SQL ile iki kaynak karşılaştırılıp merge sonrası toplam doğrulanmalı.

### Risk Seviyesi: 🟡 ORTA

PuantajKayit tek başına çalışır durumda. Ama sadece HakedisPuantaj'da olan kayıtlar eksik kalır. Bu risk, kullanıcıların hangi giriş ekranını kullandığına bağlı olarak değişir.

---

## 9. Değişiklik Gerekiyorsa Önerilen Minimum Düzeltme

```
PuantajFaturaRaporService.cs:
  1. BuildBaseQuery → iki ayrı query: BuildPuantajKayitQuery + BuildHakedisPuantajQuery
  2. MapToSatirDto overload → HakedisPuantaj → PuantajFaturaSatirDto
  3. GetSatirlarAsync → iki kaynaktan oku, merge et, sayfala
  4. GetAgacAsync → iki kaynaktan oku, merge et, grupla
  5. GetOzetAsync → SUM'ları birleştir
```

**Tahmini iş yükü**: ~100 satır ek kod. 0 migration, 0 entity değişikliği.

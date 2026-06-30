using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Araç GPS takip sistemi servisi implementasyonu
/// </summary>
public class AracTakipService : IAracTakipService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<AracTakipService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public AracTakipService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<AracTakipService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    #region Cihaz Yönetimi

    public async Task<List<AracTakipCihaz>> GetAllCihazlarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracTakipCihazlar
            .AsNoTracking()
            .Include(c => c.Arac)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Arac.AktifPlaka)
            .ToListAsync();
    }

    public async Task<List<AracTakipCihaz>> GetAktifCihazlarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracTakipCihazlar
            .AsNoTracking()
            .Include(c => c.Arac)
            .Where(c => !c.IsDeleted && c.Aktif)
            .OrderBy(c => c.Arac.AktifPlaka)
            .ToListAsync();
    }

    public async Task<AracTakipCihaz?> GetCihazByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracTakipCihazlar
            .Include(c => c.Arac)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<AracTakipCihaz?> GetCihazByAracIdAsync(int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracTakipCihazlar
            .Include(c => c.Arac)
            .FirstOrDefaultAsync(c => c.AracId == aracId && !c.IsDeleted && c.Aktif);
    }

    public async Task<AracTakipCihaz?> GetCihazByCihazIdAsync(string cihazId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracTakipCihazlar
            .Include(c => c.Arac)
            .FirstOrDefaultAsync(c => c.CihazId == cihazId && !c.IsDeleted);
    }

    public async Task<AracTakipCihaz> CreateCihazAsync(AracTakipCihaz cihaz)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Aynı cihaz ID var mı kontrol et
        var mevcutCihaz = await context.AracTakipCihazlar
            .FirstOrDefaultAsync(c => c.CihazId == cihaz.CihazId && !c.IsDeleted);
        
        if (mevcutCihaz != null)
            throw new InvalidOperationException($"Bu cihaz ID ({cihaz.CihazId}) zaten kayıtlı.");
        
        // Araçta aktif cihaz var mı kontrol et
        var mevcutAracCihaz = await context.AracTakipCihazlar
            .FirstOrDefaultAsync(c => c.AracId == cihaz.AracId && c.Aktif && !c.IsDeleted);
        
        if (mevcutAracCihaz != null && cihaz.Aktif)
        {
            // Önceki cihazı pasif yap
            mevcutAracCihaz.Aktif = false;
            context.AracTakipCihazlar.Update(mevcutAracCihaz);
        }
        
        context.AracTakipCihazlar.Add(cihaz);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Yeni takip cihazı eklendi. CihazId: {CihazId}, AracId: {AracId}", cihaz.CihazId, cihaz.AracId);
        
        return cihaz;
    }

    public async Task<AracTakipCihaz> UpdateCihazAsync(AracTakipCihaz cihaz)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.AracTakipCihazlar.FindAsync(cihaz.Id);
        if (mevcut == null)
            throw new InvalidOperationException("Cihaz bulunamadı.");
        
        mevcut.CihazId = cihaz.CihazId;
        mevcut.CihazMarka = cihaz.CihazMarka;
        mevcut.CihazModel = cihaz.CihazModel;
        mevcut.SimKartNo = cihaz.SimKartNo;
        mevcut.Aktif = cihaz.Aktif;
        mevcut.KurulumTarihi = cihaz.KurulumTarihi;
        mevcut.Notlar = cihaz.Notlar;
        
        context.AracTakipCihazlar.Update(mevcut);
        await context.SaveChangesAsync();
        
        return mevcut;
    }

    public async Task DeleteCihazAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cihaz = await context.AracTakipCihazlar.FindAsync(id);
        if (cihaz != null)
        {
            cihaz.IsDeleted = true;
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Takip cihazı silindi. CihazId: {CihazId}", cihaz.CihazId);
        }
    }

    #endregion

    #region Konum Yönetimi

    public async Task<AracKonum?> GetSonKonumByAracIdAsync(int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cihaz = await GetCihazByAracIdAsync(aracId);
        if (cihaz == null) return null;
        
        return await context.AracKonumlar
            .AsNoTracking()
            .Where(k => k.AracTakipCihazId == cihaz.Id && !k.IsDeleted)
            .OrderByDescending(k => k.KayitZamani)
            .FirstOrDefaultAsync();
    }

    public async Task<AracKonum?> GetSonKonumByCihazIdAsync(string cihazId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cihaz = await GetCihazByCihazIdAsync(cihazId);
        if (cihaz == null) return null;
        
        return await context.AracKonumlar
            .AsNoTracking()
            .Where(k => k.AracTakipCihazId == cihaz.Id && !k.IsDeleted)
            .OrderByDescending(k => k.KayitZamani)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AracKonumDto>> GetTumAraclarinSonKonumlariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonKonumlar = new List<AracKonumDto>();
        
        // Aktif cihazları al
        var cihazlar = await GetAktifCihazlarAsync();
        
        foreach (var cihaz in cihazlar)
        {
            var sonKonum = await context.AracKonumlar
                .AsNoTracking()
                .Where(k => k.AracTakipCihazId == cihaz.Id && !k.IsDeleted)
                .OrderByDescending(k => k.KayitZamani)
                .FirstOrDefaultAsync();
            
            var dto = new AracKonumDto
            {
                AracId = cihaz.AracId,
                Plaka = cihaz.Arac?.AktifPlaka ?? "Plaka Yok",
                AracMarka = cihaz.Arac?.Marka,
                AracModel = cihaz.Arac?.Model,
                CihazId = cihaz.CihazId,
                SonIletisimZamani = cihaz.SonIletisimZamani,
                BataryaSeviyesi = cihaz.BataryaSeviyesi,
                SinyalGucu = cihaz.SinyalGucu
            };
            
            if (sonKonum != null)
            {
                dto.Latitude = sonKonum.Latitude;
                dto.Longitude = sonKonum.Longitude;
                dto.Hiz = sonKonum.Hiz;
                dto.Yon = sonKonum.Yon;
                dto.KayitZamani = sonKonum.KayitZamani;
                dto.Adres = sonKonum.Adres;
                dto.KontakDurumu = sonKonum.KontakDurumu;
                dto.MotorDurumu = sonKonum.MotorDurumu;
                dto.YakitSeviyesi = sonKonum.YakitSeviyesi;
                
                // Durum belirleme
                dto.Durum = BelirleAracDurum(sonKonum, cihaz.SonIletisimZamani);
                dto.DurumAciklama = GetDurumAciklama(dto.Durum);
            }
            else
            {
                dto.Durum = AracTakipDurum.Cevrimdisi;
                dto.DurumAciklama = "Konum verisi yok";
            }
            
            sonKonumlar.Add(dto);
        }
        
        return sonKonumlar;
    }

    private AracTakipDurum BelirleAracDurum(AracKonum konum, DateTime? sonIletisim)
    {
        // Son 10 dakika içinde iletişim yoksa çevrimdışı
        if (sonIletisim == null || sonIletisim < DateTime.Now.AddMinutes(-10))
            return AracTakipDurum.Cevrimdisi;
        
        // Kontak kapalıysa park
        if (konum.KontakDurumu == false)
            return AracTakipDurum.Park;
        
        // Hız > 3 km/s ise hareket
        if (konum.Hiz > 3)
            return AracTakipDurum.Hareket;
        
        // Kontak açık, hız düşük = bekliyor
        if (konum.KontakDurumu == true)
            return AracTakipDurum.Bekliyor;
        
        return AracTakipDurum.Bilinmiyor;
    }

    private string GetDurumAciklama(AracTakipDurum durum)
    {
        return durum switch
        {
            AracTakipDurum.Cevrimdisi => "Çevrimdışı",
            AracTakipDurum.Hareket => "Hareket Halinde",
            AracTakipDurum.Bekliyor => "Beklemede",
            AracTakipDurum.Park => "Park Halinde",
            _ => "Bilinmiyor"
        };
    }

    public async Task<List<AracKonum>> GetKonumGecmisiAsync(int aracId, DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cihaz = await GetCihazByAracIdAsync(aracId);
        if (cihaz == null) return new List<AracKonum>();
        
        return await GetKonumGecmisiByCihazAsync(cihaz.Id, baslangic, bitis);
    }

    public async Task<List<AracKonum>> GetKonumGecmisiByCihazAsync(int cihazId, DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracKonumlar
            .AsNoTracking()
            .Where(k => k.AracTakipCihazId == cihazId 
                && !k.IsDeleted
                && k.KayitZamani >= baslangic 
                && k.KayitZamani <= bitis)
            .OrderBy(k => k.KayitZamani)
            .ToListAsync();
    }

    public async Task<AracKonum> KaydetKonumAsync(AracKonum konum)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.AracKonumlar.Add(konum);
        await context.SaveChangesAsync();
        
        // Cihazın son iletişim zamanını güncelle
        var cihaz = await context.AracTakipCihazlar.FindAsync(konum.AracTakipCihazId);
        if (cihaz != null)
        {
            cihaz.SonIletisimZamani = DateTime.Now;
            await context.SaveChangesAsync();
        }
        
        // Alarm kontrolü
        await KontrolEtAlarmlar(context, konum);
        
        return konum;
    }

    public async Task<int> KaydetKonumlarAsync(List<AracKonum> konumlar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (!konumlar.Any()) return 0;
        
        context.AracKonumlar.AddRange(konumlar);
        var kayitSayisi = await context.SaveChangesAsync();
        
        // Cihazların son iletişim zamanlarını güncelle
        var cihazIdler = konumlar.Select(k => k.AracTakipCihazId).Distinct().ToList();
        foreach (var cihazId in cihazIdler)
        {
            var cihaz = await context.AracTakipCihazlar.FindAsync(cihazId);
            if (cihaz != null)
            {
                cihaz.SonIletisimZamani = DateTime.Now;
            }
        }
        await context.SaveChangesAsync();
        
        _logger.LogInformation("{Adet} adet konum kaydedildi.", kayitSayisi);
        
        return kayitSayisi;
    }

    private async Task KontrolEtAlarmlar(ApplicationDbContext context, AracKonum konum)
    {
        try
        {
            var cihaz = await GetCihazByIdAsync(konum.AracTakipCihazId);
            if (cihaz == null) return;
            
            // Hız aşımı kontrolü (varsayılan 120 km/s)
            if (konum.Hiz > 120)
            {
                await CreateAlarmAsync(new AracTakipAlarm
                {
                    AracTakipCihazId = konum.AracTakipCihazId,
                    AlarmTipi = AlarmTipi.HizAsimi,
                    AlarmZamani = konum.KayitZamani,
                    Latitude = konum.Latitude,
                    Longitude = konum.Longitude,
                    Deger = konum.Hiz,
                    Mesaj = $"Hız aşımı: {konum.Hiz:F0} km/s"
                });
            }
            
            // Bölge kontrolü
            var atamalar = await context.AracBolgeAtamalar
                .Include(a => a.AracBolge)
                .Where(a => a.AracId == cihaz.AracId && !a.IsDeleted && a.AracBolge.Aktif)
                .ToListAsync();
            
            foreach (var atama in atamalar)
            {
                var bolge = atama.AracBolge;
                var icinde = KonumBolgeIcindeMi(konum.Latitude, konum.Longitude, bolge);
                
                // Önceki durumu kontrol et ve değişiklik varsa alarm oluştur
                // (basitleştirilmiş - daha kompleks mantık eklenebilir)
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alarm kontrolü sırasında hata oluştu.");
        }
    }

    private bool KonumBolgeIcindeMi(double lat, double lng, AracBolge bolge)
    {
        if (bolge.Tip == BolgeTipi.Daire && bolge.MerkezLatitude.HasValue && bolge.MerkezLongitude.HasValue && bolge.YaricapMetre.HasValue)
        {
            var mesafe = HesaplaMesafe(lat, lng, bolge.MerkezLatitude.Value, bolge.MerkezLongitude.Value);
            return mesafe <= bolge.YaricapMetre.Value;
        }
        
        // Çokgen kontrolü için daha kompleks algoritma gerekli
        return false;
    }

    #endregion

    #region Bölge (Geofence) Yönetimi

    public async Task<List<AracBolge>> GetAllBolgelerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracBolgeler
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Include(b => b.Atamalar)
                .ThenInclude(a => a.Arac)
            .OrderBy(b => b.BolgeAdi)
            .ToListAsync();
    }

    public async Task<AracBolge?> GetBolgeByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracBolgeler
            .Include(b => b.Atamalar)
                .ThenInclude(a => a.Arac)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
    }

    public async Task<List<AracBolge>> GetBolgelerByAracIdAsync(int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracBolgeAtamalar
            .AsNoTracking()
            .Where(a => a.AracId == aracId && !a.IsDeleted)
            .Select(a => a.AracBolge)
            .Where(b => !b.IsDeleted && b.Aktif)
            .ToListAsync();
    }

    public async Task<AracBolge> CreateBolgeAsync(AracBolge bolge)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.AracBolgeler.Add(bolge);
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Yeni bölge oluşturuldu: {BolgeAdi}", bolge.BolgeAdi);
        
        return bolge;
    }

    public async Task<AracBolge> UpdateBolgeAsync(AracBolge bolge)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.AracBolgeler.FindAsync(bolge.Id);
        if (mevcut == null)
            throw new InvalidOperationException("Bölge bulunamadı.");
        
        mevcut.BolgeAdi = bolge.BolgeAdi;
        mevcut.Tip = bolge.Tip;
        mevcut.MerkezLatitude = bolge.MerkezLatitude;
        mevcut.MerkezLongitude = bolge.MerkezLongitude;
        mevcut.YaricapMetre = bolge.YaricapMetre;
        mevcut.PoligonKoordinatlari = bolge.PoligonKoordinatlari;
        mevcut.Renk = bolge.Renk;
        mevcut.GirisBildirimi = bolge.GirisBildirimi;
        mevcut.CikisBildirimi = bolge.CikisBildirimi;
        mevcut.Aktif = bolge.Aktif;
        mevcut.Notlar = bolge.Notlar;
        
        await context.SaveChangesAsync();
        
        return mevcut;
    }

    public async Task DeleteBolgeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bolge = await context.AracBolgeler.FindAsync(id);
        if (bolge != null)
        {
            bolge.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task AtaBolgeToAracAsync(int bolgeId, int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Zaten atanmış mı kontrol et
        var mevcut = await context.AracBolgeAtamalar
            .FirstOrDefaultAsync(a => a.AracBolgeId == bolgeId && a.AracId == aracId && !a.IsDeleted);
        
        if (mevcut != null) return;
        
        var atama = new AracBolgeAtama
        {
            AracBolgeId = bolgeId,
            AracId = aracId
        };
        
        context.AracBolgeAtamalar.Add(atama);
        await context.SaveChangesAsync();
    }

    public async Task KaldirBolgeFromAracAsync(int bolgeId, int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var atama = await context.AracBolgeAtamalar
            .FirstOrDefaultAsync(a => a.AracBolgeId == bolgeId && a.AracId == aracId && !a.IsDeleted);
        
        if (atama != null)
        {
            atama.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Alarm Yönetimi

    public async Task<List<AracTakipAlarm>> GetAktifAlarmlarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracTakipAlarmlar
            .AsNoTracking()
            .Include(a => a.AracTakipCihaz)
                .ThenInclude(c => c.Arac)
            .Where(a => !a.IsDeleted && !a.Islendi)
            .OrderByDescending(a => a.AlarmZamani)
            .ToListAsync();
    }

    public async Task<List<AracTakipAlarm>> GetAlarmlarByAracIdAsync(int aracId, DateTime? baslangic = null, DateTime? bitis = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cihaz = await GetCihazByAracIdAsync(aracId);
        if (cihaz == null) return new List<AracTakipAlarm>();
        
        var query = context.AracTakipAlarmlar
            .AsNoTracking()
            .Where(a => a.AracTakipCihazId == cihaz.Id && !a.IsDeleted);
        
        if (baslangic.HasValue)
            query = query.Where(a => a.AlarmZamani >= baslangic.Value);
        
        if (bitis.HasValue)
            query = query.Where(a => a.AlarmZamani <= bitis.Value);
        
        return await query
            .OrderByDescending(a => a.AlarmZamani)
            .ToListAsync();
    }

    public async Task<AracTakipAlarm> CreateAlarmAsync(AracTakipAlarm alarm)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.AracTakipAlarmlar.Add(alarm);
        await context.SaveChangesAsync();
        
        _logger.LogWarning("Araç takip alarmı: {AlarmTipi} - {Mesaj}", alarm.AlarmTipi, alarm.Mesaj);
        
        return alarm;
    }

    public async Task OkunduIsaretle(int alarmId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var alarm = await context.AracTakipAlarmlar.FindAsync(alarmId);
        if (alarm != null)
        {
            alarm.Okundu = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task IslendiIsaretle(int alarmId, string? notlar = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var alarm = await context.AracTakipAlarmlar.FindAsync(alarmId);
        if (alarm != null)
        {
            alarm.Islendi = true;
            alarm.Okundu = true;
            if (!string.IsNullOrEmpty(notlar))
                alarm.Notlar = notlar;
            await context.SaveChangesAsync();
        }
    }

    public async Task<int> GetOkunmamisAlarmSayisiAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracTakipAlarmlar
            .CountAsync(a => !a.IsDeleted && !a.Okundu);
    }

    #endregion

    #region İstatistik ve Raporlama

    public async Task<AracTakipIstatistik> GetIstatistikAsync(int aracId, DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var konumlar = await GetKonumGecmisiAsync(aracId, baslangic, bitis);
        var alarmlar = await GetAlarmlarByAracIdAsync(aracId, baslangic, bitis);
        
        var istatistik = new AracTakipIstatistik
        {
            AracId = aracId,
            Baslangic = baslangic,
            Bitis = bitis
        };
        
        if (!konumlar.Any()) return istatistik;
        
        // Toplam mesafe hesaplama
        istatistik.ToplamMesafeKm = HesaplaToplamMesafe(konumlar);
        
        // Max ve ortalama hız
        var hizlar = konumlar.Where(k => k.Hiz.HasValue).Select(k => k.Hiz!.Value).ToList();
        if (hizlar.Any())
        {
            istatistik.MaxHizKmSaat = hizlar.Max();
            istatistik.OrtalamaHizKmSaat = hizlar.Average();
        }
        
        // Hareket ve bekleme süreleri
        TimeSpan hareketSuresi = TimeSpan.Zero;
        TimeSpan beklemeSuresi = TimeSpan.Zero;
        int durakSayisi = 0;
        
        for (int i = 1; i < konumlar.Count; i++)
        {
            var sure = konumlar[i].KayitZamani - konumlar[i - 1].KayitZamani;
            if (konumlar[i].Hiz > 3)
                hareketSuresi += sure;
            else
            {
                beklemeSuresi += sure;
                if (konumlar[i - 1].Hiz > 3) durakSayisi++;
            }
        }
        
        istatistik.ToplamHareketSuresi = hareketSuresi;
        istatistik.ToplamBeklemeSuresi = beklemeSuresi;
        istatistik.DurakSayisi = durakSayisi;
        
        // Alarm sayısı
        istatistik.AlarmSayisi = alarmlar.Count;
        istatistik.HizAsimiSayisi = alarmlar.Count(a => a.AlarmTipi == AlarmTipi.HizAsimi);
        
        // Yakıt
        var ilkYakit = konumlar.FirstOrDefault(k => k.YakitSeviyesi.HasValue)?.YakitSeviyesi;
        var sonYakit = konumlar.LastOrDefault(k => k.YakitSeviyesi.HasValue)?.YakitSeviyesi;
        istatistik.BaslangicYakit = ilkYakit;
        istatistik.BitisYakit = sonYakit;
        
        return istatistik;
    }

    public async Task<List<DurakNoktasi>> GetDurakNoktalariAsync(int aracId, DateTime baslangic, DateTime bitis, int minDurakDakika = 5)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var konumlar = await GetKonumGecmisiAsync(aracId, baslangic, bitis);
        var duraklar = new List<DurakNoktasi>();
        
        if (!konumlar.Any()) return duraklar;
        
        DurakNoktasi? aktifDurak = null;
        
        foreach (var konum in konumlar)
        {
            bool duruyorMu = konum.Hiz <= 3;
            
            if (duruyorMu)
            {
                if (aktifDurak == null)
                {
                    aktifDurak = new DurakNoktasi
                    {
                        Latitude = konum.Latitude,
                        Longitude = konum.Longitude,
                        Adres = konum.Adres,
                        BaslangicZamani = konum.KayitZamani,
                        KontakAcik = konum.KontakDurumu ?? false
                    };
                }
            }
            else
            {
                if (aktifDurak != null)
                {
                    aktifDurak.BitisZamani = konum.KayitZamani;
                    aktifDurak.Sure = aktifDurak.BitisZamani.Value - aktifDurak.BaslangicZamani;
                    
                    if (aktifDurak.Sure.TotalMinutes >= minDurakDakika)
                    {
                        duraklar.Add(aktifDurak);
                    }
                    
                    aktifDurak = null;
                }
            }
        }
        
        // Son durak hala devam ediyorsa
        if (aktifDurak != null)
        {
            aktifDurak.BitisZamani = konumlar.Last().KayitZamani;
            aktifDurak.Sure = aktifDurak.BitisZamani.Value - aktifDurak.BaslangicZamani;
            
            if (aktifDurak.Sure.TotalMinutes >= minDurakDakika)
            {
                duraklar.Add(aktifDurak);
            }
        }
        
        return duraklar;
    }

    public async Task<double> HesaplaToplamMesafeAsync(int aracId, DateTime baslangic, DateTime bitis)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var konumlar = await GetKonumGecmisiAsync(aracId, baslangic, bitis);
        return HesaplaToplamMesafe(konumlar);
    }

    private double HesaplaToplamMesafe(List<AracKonum> konumlar)
    {
        if (konumlar.Count < 2) return 0;
        
        double toplamMesafe = 0;
        for (int i = 1; i < konumlar.Count; i++)
        {
            toplamMesafe += HesaplaMesafe(
                konumlar[i - 1].Latitude, konumlar[i - 1].Longitude,
                konumlar[i].Latitude, konumlar[i].Longitude);
        }
        
        return toplamMesafe / 1000; // Metre'den km'ye çevir
    }

    /// <summary>
    /// İki koordinat arasındaki mesafeyi metre cinsinden hesaplar (Haversine formula)
    /// </summary>
    private double HesaplaMesafe(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Dünya yarıçapı metre
        
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return R * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180;

    #endregion

    #region API Entegrasyonu

    public async Task<bool> TestBaglantisiAsync(string apiUrl, string apiKey)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            
            var response = await client.GetAsync($"{apiUrl}/ping");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPS API bağlantı testi başarısız: {ApiUrl}", apiUrl);
            return false;
        }
    }

    public async Task SenkronizeEtAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Bu metod dış GPS platformundan verileri çekmek için kullanılır
        // Örnek: Teltonika, Queclink API'ları
        _logger.LogInformation("GPS verisi senkronizasyonu başlatıldı...");
        
        // TODO: Platform API entegrasyonu
        await Task.CompletedTask;
    }

    #endregion
}



using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Models;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// E-Fatura XML oluşturma servisi (GİB UBL-TR 1.2)
/// </summary>
public class EFaturaXmlService : IEFaturaXmlService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IFirmaService _firmaService;
    private readonly ILogger<EFaturaXmlService> _logger;
    private readonly IWebHostEnvironment _environment;

    private const string XmlDizin = "wwwroot/efatura";

    public EFaturaXmlService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IFirmaService firmaService,
        ILogger<EFaturaXmlService> logger,
        IWebHostEnvironment environment)
    {
        _dbContextFactory = dbContextFactory;
        _firmaService = firmaService;
        _logger = logger;
        _environment = environment;
    }

    /// <inheritdoc/>
    public async Task<EFaturaXmlSonuc> XmlOlusturAsync(EFaturaXmlRequest request)
    {
        var sonuc = new EFaturaXmlSonuc();

        try
        {
            // UBL-TR'ye dönüştür
            var ublInvoice = await UblDonusturAsync(request.FaturaId);
            if (ublInvoice == null)
            {
                sonuc.Hata = "Fatura bulunamadı veya dönüştürülemedi.";
                return sonuc;
            }

            // ETTN ayarla
            var ettn = string.IsNullOrEmpty(request.EttnNo) 
                ? YeniEttnOlustur() 
                : request.EttnNo;
            ublInvoice.Uuid = ettn;

            // Profil ID ayarla
            ublInvoice.ProfileId = request.Senaryo switch
            {
                EFaturaSenaryo.Temel => EFaturaProfilIdleri.TEMEL,
                EFaturaSenaryo.Ticari => EFaturaProfilIdleri.TICARI,
                EFaturaSenaryo.Ihracat => EFaturaProfilIdleri.IHRACAT,
                EFaturaSenaryo.Kamu => EFaturaProfilIdleri.TEMEL,
                _ => EFaturaProfilIdleri.TEMEL
            };

            // XML'e serialize et
            var xmlIcerik = SerializeToXml(ublInvoice);

            // Doğrula
            var dogrulamaRapor = await DogrulaAsync(xmlIcerik);
            sonuc.DogrulamaRapor = dogrulamaRapor;

            if (!dogrulamaRapor.Gecerli)
            {
                sonuc.Hata = "E-Fatura doğrulama hatası.";
                sonuc.Uyarilar = dogrulamaRapor.Uyarilar;
                return sonuc;
            }

            // Dosyaya kaydet
            var dosyaYolu = await DosyayaKaydetAsync(request.FaturaId, xmlIcerik);

            // Fatura kaydını güncelle
            await FaturayiGuncelleAsync(request.FaturaId, ettn, dosyaYolu);

            sonuc.Basarili = true;
            sonuc.XmlIcerik = xmlIcerik;
            sonuc.EttnNo = ettn;
            sonuc.DosyaYolu = dosyaYolu;
            sonuc.Uyarilar = dogrulamaRapor.Uyarilar;

            _logger.LogInformation("E-Fatura XML oluşturuldu. FaturaId: {FaturaId}, ETTN: {Ettn}", 
                request.FaturaId, ettn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-Fatura XML oluşturma hatası. FaturaId: {FaturaId}", request.FaturaId);
            sonuc.Hata = $"XML oluşturma hatası: {ex.Message}";
        }

        return sonuc;
    }

    /// <inheritdoc/>
    public async Task<UblInvoice?> UblDonusturAsync(int faturaId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var fatura = await dbContext.Faturalar
            .Include(f => f.Cari)
            .Include(f => f.Firma)
            .Include(f => f.FaturaKalemleri)
            .FirstOrDefaultAsync(f => f.Id == faturaId);

        if (fatura == null)
            return null;

        // Firma bilgisi
        var firma = fatura.Firma;
        if (firma == null)
        {
            var aktifFirma = _firmaService.GetAktifFirma();
            if (aktifFirma != null && aktifFirma.FirmaId > 0)
            {
                firma = await dbContext.Firmalar.FindAsync(aktifFirma.FirmaId);
            }
        }

        if (firma == null)
        {
            _logger.LogWarning("Firma bilgisi bulunamadı. FaturaId: {FaturaId}", faturaId);
            return null;
        }

        var ublInvoice = new UblInvoice
        {
            Id = fatura.FaturaNo,
            Uuid = fatura.EttnNo ?? YeniEttnOlustur(),
            IssueDate = fatura.FaturaTarihi.ToString("yyyy-MM-dd"),
            IssueTime = fatura.FaturaTarihi.ToString("HH:mm:ss"),
            LineCountNumeric = fatura.FaturaKalemleri.Count,
            DocumentCurrencyCode = "TRY"
        };

        // Fatura tipi belirle
        ublInvoice.InvoiceTypeCode = FaturaTipiKoduBelirle(fatura);

        // Notlar
        if (!string.IsNullOrEmpty(fatura.Aciklama))
            ublInvoice.Notes.Add(fatura.Aciklama);
        if (!string.IsNullOrEmpty(fatura.Notlar))
            ublInvoice.Notes.Add(fatura.Notlar);

        // Satıcı (Firma) bilgisi
        ublInvoice.AccountingSupplierParty = OlusturSatici(firma);

        // Alıcı (Cari) bilgisi
        ublInvoice.AccountingCustomerParty = OlusturAlici(fatura.Cari);

        // Ödeme koşulları
        if (fatura.VadeTarihi.HasValue)
        {
            ublInvoice.PaymentMeans = new UblPaymentMeans
            {
                PaymentMeansCode = "1", // Nakit
                PaymentDueDate = fatura.VadeTarihi.Value.ToString("yyyy-MM-dd")
            };

            ublInvoice.PaymentTerms = new UblPaymentTerms
            {
                Note = $"Vade Tarihi: {fatura.VadeTarihi.Value:dd.MM.yyyy}"
            };
        }

        // Fatura kalemleri
        var siraNo = 1;
        foreach (var kalem in fatura.FaturaKalemleri.OrderBy(k => k.SiraNo))
        {
            var ublLine = OlusturFaturaKalemi(kalem, siraNo++);
            ublInvoice.InvoiceLines.Add(ublLine);
        }

        // Vergi toplamları
        ublInvoice.TaxTotals = OlusturVergiToplamlari(fatura);

        // Tevkifat varsa
        if (fatura.TevkifatliMi && fatura.TevkifatTutar > 0)
        {
            ublInvoice.WithholdingTaxTotals = OlusturTevkifatToplamlari(fatura);
        }

        // Parasal toplam
        ublInvoice.LegalMonetaryTotal = OlusturMonetaryTotal(fatura);

        return ublInvoice;
    }

    /// <inheritdoc/>
    public Task<EFaturaDogrulamaRapor> DogrulaAsync(string xmlIcerik)
    {
        var rapor = new EFaturaDogrulamaRapor { Gecerli = true };

        try
        {
            // XML parse kontrolü
            var doc = new XmlDocument();
            doc.LoadXml(xmlIcerik);

            // Zorunlu alan kontrolleri
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("inv", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
            ns.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            ns.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");

            // UUID kontrolü
            var uuidNode = doc.SelectSingleNode("//cbc:UUID", ns);
            if (uuidNode == null || string.IsNullOrEmpty(uuidNode.InnerText))
            {
                rapor.Hatalar.Add(new EFaturaDogrulamaHatasi
                {
                    Kod = "UUID001",
                    Mesaj = "ETTN (UUID) zorunludur.",
                    Alan = "UUID"
                });
            }

            // Fatura No kontrolü
            var idNode = doc.SelectSingleNode("//cbc:ID", ns);
            if (idNode == null || string.IsNullOrEmpty(idNode.InnerText))
            {
                rapor.Hatalar.Add(new EFaturaDogrulamaHatasi
                {
                    Kod = "ID001",
                    Mesaj = "Fatura numarası zorunludur.",
                    Alan = "ID"
                });
            }

            // Satıcı VKN kontrolü
            var supplierVkn = doc.SelectSingleNode("//cac:AccountingSupplierParty//cac:PartyIdentification/cbc:ID[@schemeID='VKN' or @schemeID='TCKN']", ns);
            if (supplierVkn == null || string.IsNullOrEmpty(supplierVkn.InnerText))
            {
                rapor.Hatalar.Add(new EFaturaDogrulamaHatasi
                {
                    Kod = "SUPPLIER001",
                    Mesaj = "Satıcı VKN/TCKN zorunludur.",
                    Alan = "AccountingSupplierParty"
                });
            }

            // Alıcı VKN kontrolü
            var customerVkn = doc.SelectSingleNode("//cac:AccountingCustomerParty//cac:PartyIdentification/cbc:ID[@schemeID='VKN' or @schemeID='TCKN']", ns);
            if (customerVkn == null || string.IsNullOrEmpty(customerVkn.InnerText))
            {
                rapor.Uyarilar.Add("Alıcı VKN/TCKN belirtilmemiş. E-Arşiv için sorun oluşturabilir.");
            }

            // Fatura satırı kontrolü
            var invoiceLines = doc.SelectNodes("//cac:InvoiceLine", ns);
            if (invoiceLines == null || invoiceLines.Count == 0)
            {
                rapor.Hatalar.Add(new EFaturaDogrulamaHatasi
                {
                    Kod = "LINE001",
                    Mesaj = "En az bir fatura kalemi zorunludur.",
                    Alan = "InvoiceLine"
                });
            }

            // Tutar kontrolü
            var payableAmount = doc.SelectSingleNode("//cac:LegalMonetaryTotal/cbc:PayableAmount", ns);
            if (payableAmount == null)
            {
                rapor.Hatalar.Add(new EFaturaDogrulamaHatasi
                {
                    Kod = "AMOUNT001",
                    Mesaj = "Ödenecek tutar zorunludur.",
                    Alan = "PayableAmount"
                });
            }

            rapor.Gecerli = rapor.Hatalar.Count == 0;
        }
        catch (XmlException ex)
        {
            rapor.Gecerli = false;
            rapor.Hatalar.Add(new EFaturaDogrulamaHatasi
            {
                Kod = "XML001",
                Mesaj = $"XML format hatası: {ex.Message}",
                Alan = "XML"
            });
        }

        return Task.FromResult(rapor);
    }

    /// <inheritdoc/>
    public async Task<string?> DosyayaKaydetAsync(int faturaId, string xmlIcerik)
    {
        try
        {
            var dizin = Path.Combine(_environment.ContentRootPath, XmlDizin);
            if (!Directory.Exists(dizin))
                Directory.CreateDirectory(dizin);

            // Alt klasör: Yıl/Ay
            var yil = DateTime.Now.Year.ToString();
            var ay = DateTime.Now.Month.ToString("00");
            var altDizin = Path.Combine(dizin, yil, ay);
            if (!Directory.Exists(altDizin))
                Directory.CreateDirectory(altDizin);

            var dosyaAdi = $"fatura_{faturaId}_{DateTime.Now:yyyyMMddHHmmss}.xml";
            var dosyaYolu = Path.Combine(altDizin, dosyaAdi);

            await File.WriteAllTextAsync(dosyaYolu, xmlIcerik, Encoding.UTF8);

            // Göreli yol döndür
            return Path.Combine("efatura", yil, ay, dosyaAdi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-Fatura XML dosyası kaydedilemedi. FaturaId: {FaturaId}", faturaId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> XmlOkuAsync(int faturaId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var fatura = await dbContext.Faturalar
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == faturaId);

        if (fatura?.XmlDosyaYolu == null)
            return null;

        var dosyaYolu = Path.Combine(_environment.ContentRootPath, "wwwroot", fatura.XmlDosyaYolu);
        if (!File.Exists(dosyaYolu))
            return null;

        return await File.ReadAllTextAsync(dosyaYolu, Encoding.UTF8);
    }

    /// <inheritdoc/>
    public async Task<List<EFaturaXmlSonuc>> TopluXmlOlusturAsync(List<int> faturaIdler, EFaturaSenaryo senaryo)
    {
        var sonuclar = new List<EFaturaXmlSonuc>();

        foreach (var faturaId in faturaIdler)
        {
            var request = new EFaturaXmlRequest
            {
                FaturaId = faturaId,
                Senaryo = senaryo
            };

            var sonuc = await XmlOlusturAsync(request);
            sonuclar.Add(sonuc);
        }

        return sonuclar;
    }

    /// <inheritdoc/>
    public async Task<bool> GibDurumGuncelleAsync(int faturaId, GibGonderimDurumu durum, string? gibKodu = null, string? mesaj = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var fatura = await dbContext.Faturalar.FindAsync(faturaId);
        if (fatura == null)
            return false;

        if (durum != GibGonderimDurumu.Bekliyor && string.IsNullOrWhiteSpace(fatura.XmlDosyaYolu))
            throw new InvalidOperationException("GİB durumuna alınmadan önce XML oluşturulmuş olmalıdır.");

        fatura.GibDurumu = durum;
        fatura.GibDurumMesaji = mesaj;
        fatura.GibDurumGuncellemeTarihi = DateTime.UtcNow;

        if (durum == GibGonderimDurumu.Gonderildi)
        {
            fatura.GibGonderimTarihi = DateTime.UtcNow;
        }

        if (durum == GibGonderimDurumu.KabulEdildi)
        {
            fatura.GibKodu = gibKodu;
            fatura.GibOnayTarihi = DateTime.UtcNow;
        }

        if (durum == GibGonderimDurumu.Reddedildi)
        {
            fatura.GibKodu = gibKodu;
            fatura.GibOnayTarihi = null;
        }

        if (durum == GibGonderimDurumu.Bekliyor)
        {
            fatura.GibKodu = null;
            fatura.GibOnayTarihi = null;
            fatura.GibGonderimTarihi = null;
        }

        await dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc/>
    public string YeniEttnOlustur()
    {
        return Guid.NewGuid().ToString().ToUpperInvariant();
    }

    /// <inheritdoc/>
    public string BirimKoduDonustur(string birim)
    {
        return UblBirimKodlari.GetUblKod(birim);
    }

    #region Private Helper Methods

    private string FaturaTipiKoduBelirle(Fatura fatura)
    {
        if (fatura.TevkifatliMi)
            return EFaturaTipKodlari.TEVKIFAT;

        return fatura.FaturaTipi switch
        {
            FaturaTipi.SatisIadeFaturasi or FaturaTipi.AlisIadeFaturasi => EFaturaTipKodlari.IADE,
            _ => EFaturaTipKodlari.SATIS
        };
    }

    private UblAccountingParty OlusturSatici(Firma firma)
    {
        var party = new UblParty
        {
            PartyName = new UblPartyName { Name = firma.UnvanTam ?? firma.FirmaAdi },
            PostalAddress = new UblAddress
            {
                StreetName = firma.Adres,
                CitySubdivisionName = firma.Ilce,
                CityName = firma.Il,
                Country = new UblCountry()
            },
            PartyTaxScheme = new UblPartyTaxScheme
            {
                TaxScheme = new UblTaxScheme { Name = firma.VergiDairesi }
            }
        };

        // VKN veya TCKN
        if (!string.IsNullOrEmpty(firma.VergiNo))
        {
            var schemeId = firma.VergiNo.Length == 11 ? "TCKN" : "VKN";
            party.PartyIdentifications.Add(new UblPartyIdentification
            {
                Id = new UblIdentifier { SchemeId = schemeId, Value = firma.VergiNo }
            });
        }

        // İletişim
        if (!string.IsNullOrEmpty(firma.Telefon) || !string.IsNullOrEmpty(firma.Email))
        {
            party.Contact = new UblContact
            {
                Telephone = firma.Telefon,
                ElectronicMail = firma.Email
            };
        }

        // Web sitesi
        if (!string.IsNullOrEmpty(firma.WebSite))
            party.WebsiteUri = firma.WebSite;

        return new UblAccountingParty { Party = party };
    }

    private UblAccountingParty OlusturAlici(Cari cari)
    {
        var party = new UblParty
        {
            PartyName = new UblPartyName { Name = cari.Unvan },
            PostalAddress = new UblAddress
            {
                StreetName = cari.Adres,
                CitySubdivisionName = cari.Ilce,
                CityName = cari.Il,
                PostalZone = cari.PostaKodu,
                Country = new UblCountry()
            },
            PartyTaxScheme = new UblPartyTaxScheme
            {
                TaxScheme = new UblTaxScheme { Name = cari.VergiDairesi }
            }
        };

        // VKN veya TCKN
        var vergiNo = cari.VergiNo ?? cari.TcKimlikNo;
        if (!string.IsNullOrEmpty(vergiNo))
        {
            var schemeId = vergiNo.Length == 11 ? "TCKN" : "VKN";
            party.PartyIdentifications.Add(new UblPartyIdentification
            {
                Id = new UblIdentifier { SchemeId = schemeId, Value = vergiNo }
            });
        }

        // Şahıs firması ise kişi bilgisi
        if (!string.IsNullOrEmpty(cari.TcKimlikNo) && !string.IsNullOrEmpty(cari.YetkiliKisi))
        {
            var adParcalari = cari.YetkiliKisi.Split(' ', 2);
            party.Person = new UblPerson
            {
                FirstName = adParcalari.Length > 0 ? adParcalari[0] : cari.YetkiliKisi,
                FamilyName = adParcalari.Length > 1 ? adParcalari[1] : string.Empty
            };
        }

        // İletişim
        if (!string.IsNullOrEmpty(cari.Telefon) || !string.IsNullOrEmpty(cari.Email))
        {
            party.Contact = new UblContact
            {
                Telephone = cari.Telefon,
                Telefax = cari.Fax,
                ElectronicMail = cari.Email
            };
        }

        // Web sitesi
        if (!string.IsNullOrEmpty(cari.WebSitesi))
            party.WebsiteUri = cari.WebSitesi;

        return new UblAccountingParty { Party = party };
    }

    private UblInvoiceLine OlusturFaturaKalemi(FaturaKalem kalem, int siraNo)
    {
        var netTutar = (kalem.Miktar * kalem.BirimFiyat) - kalem.IskontoTutar;

        var line = new UblInvoiceLine
        {
            Id = siraNo.ToString(),
            Note = kalem.Aciklama,
            InvoicedQuantity = new UblQuantity
            {
                UnitCode = BirimKoduDonustur(kalem.Birim),
                Value = kalem.Miktar
            },
            LineExtensionAmount = new UblAmount { Value = netTutar },
            Item = new UblItem
            {
                Name = kalem.Aciklama,
                Description = kalem.Aciklama
            },
            Price = new UblPrice
            {
                PriceAmount = new UblAmount { Value = kalem.BirimFiyat }
            }
        };

        // Ürün kodu varsa
        if (!string.IsNullOrEmpty(kalem.UrunKodu))
        {
            line.Item.SellersItemIdentification = new UblItemIdentification
            {
                Id = kalem.UrunKodu
            };
        }

        // İskonto
        if (kalem.IskontoTutar > 0)
        {
            line.AllowanceCharge = new UblAllowanceCharge
            {
                ChargeIndicator = false,
                MultiplierFactorNumeric = kalem.IskontoOrani / 100m,
                Amount = new UblAmount { Value = kalem.IskontoTutar },
                BaseAmount = new UblAmount { Value = kalem.Miktar * kalem.BirimFiyat }
            };
        }

        // Satır KDV
        line.TaxTotal = new UblTaxTotal
        {
            TaxAmount = new UblAmount { Value = kalem.KdvTutar },
            TaxSubtotals = new List<UblTaxSubtotal>
            {
                new UblTaxSubtotal
                {
                    TaxableAmount = new UblAmount { Value = netTutar },
                    TaxAmount = new UblAmount { Value = kalem.KdvTutar },
                    Percent = kalem.KdvOrani,
                    TaxCategory = new UblTaxCategory
                    {
                        TaxScheme = new UblTaxSchemeDetail
                        {
                            Name = "KDV",
                            TaxTypeCode = GibVergiKodlari.KDV
                        }
                    }
                }
            }
        };

        // Satır tevkifat
        if (kalem.TevkifatTutar > 0)
        {
            line.WithholdingTaxTotal = new UblTaxTotal
            {
                TaxAmount = new UblAmount { Value = kalem.TevkifatTutar },
                TaxSubtotals = new List<UblTaxSubtotal>
                {
                    new UblTaxSubtotal
                    {
                        TaxableAmount = new UblAmount { Value = kalem.KdvTutar },
                        TaxAmount = new UblAmount { Value = kalem.TevkifatTutar },
                        Percent = kalem.TevkifatOrani,
                        TaxCategory = new UblTaxCategory
                        {
                            TaxScheme = new UblTaxSchemeDetail
                            {
                                Name = "KDV TEVKIFAT",
                                TaxTypeCode = GibVergiKodlari.KDV
                            }
                        }
                    }
                }
            };
        }

        return line;
    }

    private List<UblTaxTotal> OlusturVergiToplamlari(Fatura fatura)
    {
        // KDV oranlarına göre grupla
        var kdvGruplari = fatura.FaturaKalemleri
            .GroupBy(k => k.KdvOrani)
            .Select(g => new
            {
                Oran = g.Key,
                Matrah = g.Sum(k => (k.Miktar * k.BirimFiyat) - k.IskontoTutar),
                KdvTutar = g.Sum(k => k.KdvTutar)
            })
            .ToList();

        var taxTotal = new UblTaxTotal
        {
            TaxAmount = new UblAmount { Value = fatura.KdvTutar }
        };

        foreach (var grup in kdvGruplari)
        {
            taxTotal.TaxSubtotals.Add(new UblTaxSubtotal
            {
                TaxableAmount = new UblAmount { Value = grup.Matrah },
                TaxAmount = new UblAmount { Value = grup.KdvTutar },
                Percent = grup.Oran,
                TaxCategory = new UblTaxCategory
                {
                    TaxScheme = new UblTaxSchemeDetail
                    {
                        Name = "KDV",
                        TaxTypeCode = GibVergiKodlari.KDV
                    }
                }
            });
        }

        return new List<UblTaxTotal> { taxTotal };
    }

    private List<UblTaxTotal> OlusturTevkifatToplamlari(Fatura fatura)
    {
        var taxTotal = new UblTaxTotal
        {
            TaxAmount = new UblAmount { Value = fatura.TevkifatTutar },
            TaxSubtotals = new List<UblTaxSubtotal>
            {
                new UblTaxSubtotal
                {
                    TaxableAmount = new UblAmount { Value = fatura.KdvTutar },
                    TaxAmount = new UblAmount { Value = fatura.TevkifatTutar },
                    Percent = fatura.TevkifatOrani,
                    TaxCategory = new UblTaxCategory
                    {
                        TaxScheme = new UblTaxSchemeDetail
                        {
                            Name = "KDV TEVKIFAT",
                            TaxTypeCode = GibVergiKodlari.KDV
                        }
                    }
                }
            }
        };

        return new List<UblTaxTotal> { taxTotal };
    }

    private UblMonetaryTotal OlusturMonetaryTotal(Fatura fatura)
    {
        var kdvHaricToplam = fatura.AraToplam - fatura.IskontoTutar;

        return new UblMonetaryTotal
        {
            LineExtensionAmount = new UblAmount { Value = kdvHaricToplam },
            TaxExclusiveAmount = new UblAmount { Value = kdvHaricToplam },
            TaxInclusiveAmount = new UblAmount { Value = fatura.GenelToplam },
            AllowanceTotalAmount = fatura.IskontoTutar > 0 
                ? new UblAmount { Value = fatura.IskontoTutar } 
                : null,
            PayableAmount = new UblAmount 
            { 
                Value = fatura.TevkifatliMi 
                    ? fatura.GenelToplam - fatura.TevkifatTutar 
                    : fatura.GenelToplam 
            }
        };
    }

    private string SerializeToXml(UblInvoice invoice)
    {
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
        namespaces.Add("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
        namespaces.Add("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
        namespaces.Add("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

        var serializer = new XmlSerializer(typeof(UblInvoice));
        
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        
        serializer.Serialize(xmlWriter, invoice, namespaces);
        
        return stringWriter.ToString();
    }

    private async Task FaturayiGuncelleAsync(int faturaId, string ettn, string? dosyaYolu)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var fatura = await dbContext.Faturalar.FindAsync(faturaId);
        if (fatura != null)
        {
            fatura.EttnNo = ettn;
            if (!string.IsNullOrEmpty(dosyaYolu))
                fatura.XmlDosyaYolu = dosyaYolu;
            fatura.GibDurumu = GibGonderimDurumu.XmlHazirlandi;
            fatura.GibDurumMesaji = "UBL-TR XML oluşturuldu.";
            fatura.GibDurumGuncellemeTarihi = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }
    }

    #endregion
}



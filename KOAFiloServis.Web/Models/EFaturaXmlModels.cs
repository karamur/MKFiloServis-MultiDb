using System.Xml.Serialization;

namespace KOAFiloServis.Web.Models;

#region Ana E-Fatura Modeli

/// <summary>
/// E-Fatura XML oluşturma isteği
/// </summary>
public class EFaturaXmlRequest
{
    public int FaturaId { get; set; }
    public EFaturaSenaryo Senaryo { get; set; } = EFaturaSenaryo.Temel;
    public string? EttnNo { get; set; } // Boş ise otomatik oluşturulur (GUID)
    public bool ImzaliMi { get; set; } = false; // Mali mühür ile imzalanacak mı?
}

/// <summary>
/// E-Fatura XML oluşturma sonucu
/// </summary>
public class EFaturaXmlSonuc
{
    public bool Basarili { get; set; }
    public string? XmlIcerik { get; set; }
    public string? EttnNo { get; set; }
    public string? DosyaYolu { get; set; }
    public string? Hata { get; set; }
    public List<string> Uyarilar { get; set; } = new();
    public EFaturaDogrulamaRapor DogrulamaRapor { get; set; } = new();
}

/// <summary>
/// E-Fatura doğrulama raporu
/// </summary>
public class EFaturaDogrulamaRapor
{
    public bool Gecerli { get; set; }
    public List<EFaturaDogrulamaHatasi> Hatalar { get; set; } = new();
    public List<string> Uyarilar { get; set; } = new();
}

/// <summary>
/// E-Fatura doğrulama hatası
/// </summary>
public class EFaturaDogrulamaHatasi
{
    public string Kod { get; set; } = string.Empty;
    public string Mesaj { get; set; } = string.Empty;
    public string? Alan { get; set; }
}

/// <summary>
/// E-Fatura senaryoları
/// </summary>
public enum EFaturaSenaryo
{
    Temel = 1,      // Standart fatura
    Ticari = 2,     // Ticari fatura (kabul/red mekanizması)
    Ihracat = 3,    // İhracat faturası
    Kamu = 4        // Kamu kurumu faturası
}

#endregion

#region UBL-TR XML Yapıları

/// <summary>
/// UBL-TR Fatura kök elementi
/// </summary>
[XmlRoot("Invoice", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2")]
public class UblInvoice
{
    [XmlElement("UBLVersionID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string UblVersionId { get; set; } = "2.1";

    [XmlElement("CustomizationID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string CustomizationId { get; set; } = "TR1.2";

    [XmlElement("ProfileID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string ProfileId { get; set; } = "TICARIFATURA";

    [XmlElement("ID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string Id { get; set; } = string.Empty;

    [XmlElement("CopyIndicator", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public bool CopyIndicator { get; set; } = false;

    [XmlElement("UUID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string Uuid { get; set; } = string.Empty;

    [XmlElement("IssueDate", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string IssueDate { get; set; } = string.Empty;

    [XmlElement("IssueTime", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string IssueTime { get; set; } = string.Empty;

    [XmlElement("InvoiceTypeCode", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string InvoiceTypeCode { get; set; } = "SATIS";

    [XmlElement("Note", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public List<string> Notes { get; set; } = new();

    [XmlElement("DocumentCurrencyCode", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string DocumentCurrencyCode { get; set; } = "TRY";

    [XmlElement("LineCountNumeric", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public int LineCountNumeric { get; set; }

    [XmlElement("Signature", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblSignature? Signature { get; set; }

    [XmlElement("AccountingSupplierParty", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblAccountingParty AccountingSupplierParty { get; set; } = new();

    [XmlElement("AccountingCustomerParty", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblAccountingParty AccountingCustomerParty { get; set; } = new();

    [XmlElement("PaymentMeans", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblPaymentMeans? PaymentMeans { get; set; }

    [XmlElement("PaymentTerms", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblPaymentTerms? PaymentTerms { get; set; }

    [XmlElement("TaxTotal", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public List<UblTaxTotal> TaxTotals { get; set; } = new();

    [XmlElement("WithholdingTaxTotal", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public List<UblTaxTotal>? WithholdingTaxTotals { get; set; }

    [XmlElement("LegalMonetaryTotal", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblMonetaryTotal LegalMonetaryTotal { get; set; } = new();

    [XmlElement("InvoiceLine", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public List<UblInvoiceLine> InvoiceLines { get; set; } = new();
}

/// <summary>
/// UBL İmza bilgisi
/// </summary>
public class UblSignature
{
    [XmlElement("ID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string Id { get; set; } = string.Empty;

    [XmlElement("SignatoryParty", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblParty? SignatoryParty { get; set; }
}

/// <summary>
/// UBL Muhasebe Tarafı (Satıcı/Alıcı)
/// </summary>
public class UblAccountingParty
{
    [XmlElement("Party", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblParty Party { get; set; } = new();
}

/// <summary>
/// UBL Taraf bilgisi
/// </summary>
public class UblParty
{
    [XmlElement("WebsiteURI", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? WebsiteUri { get; set; }

    [XmlElement("PartyIdentification", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public List<UblPartyIdentification> PartyIdentifications { get; set; } = new();

    [XmlElement("PartyName", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblPartyName? PartyName { get; set; }

    [XmlElement("PostalAddress", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblAddress PostalAddress { get; set; } = new();

    [XmlElement("PartyTaxScheme", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblPartyTaxScheme PartyTaxScheme { get; set; } = new();

    [XmlElement("Contact", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblContact? Contact { get; set; }

    [XmlElement("Person", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblPerson? Person { get; set; }
}

/// <summary>
/// UBL Taraf kimlik bilgisi
/// </summary>
public class UblPartyIdentification
{
    [XmlElement("ID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblIdentifier Id { get; set; } = new();
}

/// <summary>
/// UBL Kimlik tanımlayıcı (schemeID ile)
/// </summary>
public class UblIdentifier
{
    [XmlAttribute("schemeID")]
    public string SchemeId { get; set; } = string.Empty;

    [XmlText]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// UBL Taraf adı
/// </summary>
public class UblPartyName
{
    [XmlElement("Name", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// UBL Adres bilgisi
/// </summary>
public class UblAddress
{
    [XmlElement("ID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? Id { get; set; }

    [XmlElement("StreetName", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? StreetName { get; set; }

    [XmlElement("BuildingName", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? BuildingName { get; set; }

    [XmlElement("BuildingNumber", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? BuildingNumber { get; set; }

    [XmlElement("CitySubdivisionName", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? CitySubdivisionName { get; set; } // İlçe

    [XmlElement("CityName", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? CityName { get; set; } // İl

    [XmlElement("PostalZone", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? PostalZone { get; set; }

    [XmlElement("Country", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblCountry Country { get; set; } = new();
}

/// <summary>
/// UBL Ülke bilgisi
/// </summary>
public class UblCountry
{
    [XmlElement("IdentificationCode", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string IdentificationCode { get; set; } = "TR";

    [XmlElement("Name", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string Name { get; set; } = "Türkiye";
}

/// <summary>
/// UBL Vergi dairesi bilgisi
/// </summary>
public class UblPartyTaxScheme
{
    [XmlElement("TaxScheme", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblTaxScheme TaxScheme { get; set; } = new();
}

/// <summary>
/// UBL Vergi dairesi
/// </summary>
public class UblTaxScheme
{
    [XmlElement("Name", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? Name { get; set; }
}

/// <summary>
/// UBL İletişim bilgisi
/// </summary>
public class UblContact
{
    [XmlElement("Telephone", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? Telephone { get; set; }

    [XmlElement("Telefax", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? Telefax { get; set; }

    [XmlElement("ElectronicMail", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? ElectronicMail { get; set; }
}

/// <summary>
/// UBL Kişi bilgisi (şahıs firmaları için)
/// </summary>
public class UblPerson
{
    [XmlElement("FirstName", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? FirstName { get; set; }

    [XmlElement("FamilyName", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? FamilyName { get; set; }

    [XmlElement("MiddleName", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? MiddleName { get; set; }
}

/// <summary>
/// UBL Ödeme şekli
/// </summary>
public class UblPaymentMeans
{
    [XmlElement("PaymentMeansCode", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string PaymentMeansCode { get; set; } = "1"; // Nakit

    [XmlElement("PaymentDueDate", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? PaymentDueDate { get; set; }

    [XmlElement("PayeeFinancialAccount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblFinancialAccount? PayeeFinancialAccount { get; set; }
}

/// <summary>
/// UBL Finansal hesap (IBAN)
/// </summary>
public class UblFinancialAccount
{
    [XmlElement("ID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? Id { get; set; }

    [XmlElement("CurrencyCode", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string CurrencyCode { get; set; } = "TRY";

    [XmlElement("PaymentNote", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? PaymentNote { get; set; }
}

/// <summary>
/// UBL Ödeme koşulları
/// </summary>
public class UblPaymentTerms
{
    [XmlElement("Note", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? Note { get; set; }

    [XmlElement("PenaltySurchargePercent", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public decimal? PenaltySurchargePercent { get; set; }

    [XmlElement("Amount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount? Amount { get; set; }
}

/// <summary>
/// UBL Vergi toplamı
/// </summary>
public class UblTaxTotal
{
    [XmlElement("TaxAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount TaxAmount { get; set; } = new();

    [XmlElement("TaxSubtotal", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public List<UblTaxSubtotal> TaxSubtotals { get; set; } = new();
}

/// <summary>
/// UBL Vergi alt toplamı
/// </summary>
public class UblTaxSubtotal
{
    [XmlElement("TaxableAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount TaxableAmount { get; set; } = new();

    [XmlElement("TaxAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount TaxAmount { get; set; } = new();

    [XmlElement("CalculationSequenceNumeric", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public int CalculationSequenceNumeric { get; set; } = 1;

    [XmlElement("Percent", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public decimal Percent { get; set; }

    [XmlElement("TaxCategory", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblTaxCategory TaxCategory { get; set; } = new();
}

/// <summary>
/// UBL Vergi kategorisi
/// </summary>
public class UblTaxCategory
{
    [XmlElement("TaxScheme", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblTaxSchemeDetail TaxScheme { get; set; } = new();

    [XmlElement("TaxExemptionReasonCode", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? TaxExemptionReasonCode { get; set; }

    [XmlElement("TaxExemptionReason", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? TaxExemptionReason { get; set; }
}

/// <summary>
/// UBL Vergi şeması detayı
/// </summary>
public class UblTaxSchemeDetail
{
    [XmlElement("Name", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string Name { get; set; } = "KDV";

    [XmlElement("TaxTypeCode", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string TaxTypeCode { get; set; } = "0015"; // KDV kodu
}

/// <summary>
/// UBL Tutar (para birimi ile)
/// </summary>
public class UblAmount
{
    [XmlAttribute("currencyID")]
    public string CurrencyId { get; set; } = "TRY";

    [XmlText]
    public decimal Value { get; set; }
}

/// <summary>
/// UBL Miktar (birim ile)
/// </summary>
public class UblQuantity
{
    [XmlAttribute("unitCode")]
    public string UnitCode { get; set; } = "C62"; // Adet

    [XmlText]
    public decimal Value { get; set; }
}

/// <summary>
/// UBL Parasal toplam
/// </summary>
public class UblMonetaryTotal
{
    [XmlElement("LineExtensionAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount LineExtensionAmount { get; set; } = new(); // Satır toplamı (KDV hariç)

    [XmlElement("TaxExclusiveAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount TaxExclusiveAmount { get; set; } = new(); // Vergi hariç toplam

    [XmlElement("TaxInclusiveAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount TaxInclusiveAmount { get; set; } = new(); // Vergi dahil toplam

    [XmlElement("AllowanceTotalAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount? AllowanceTotalAmount { get; set; } // İskonto toplamı

    [XmlElement("PayableAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount PayableAmount { get; set; } = new(); // Ödenecek tutar
}

/// <summary>
/// UBL Fatura satırı
/// </summary>
public class UblInvoiceLine
{
    [XmlElement("ID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string Id { get; set; } = string.Empty;

    [XmlElement("Note", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? Note { get; set; }

    [XmlElement("InvoicedQuantity", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblQuantity InvoicedQuantity { get; set; } = new();

    [XmlElement("LineExtensionAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount LineExtensionAmount { get; set; } = new();

    [XmlElement("AllowanceCharge", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblAllowanceCharge? AllowanceCharge { get; set; }

    [XmlElement("TaxTotal", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblTaxTotal TaxTotal { get; set; } = new();

    [XmlElement("WithholdingTaxTotal", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblTaxTotal? WithholdingTaxTotal { get; set; }

    [XmlElement("Item", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblItem Item { get; set; } = new();

    [XmlElement("Price", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblPrice Price { get; set; } = new();
}

/// <summary>
/// UBL İskonto/Ek ücret
/// </summary>
public class UblAllowanceCharge
{
    [XmlElement("ChargeIndicator", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public bool ChargeIndicator { get; set; } = false; // false = iskonto

    [XmlElement("MultiplierFactorNumeric", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public decimal? MultiplierFactorNumeric { get; set; }

    [XmlElement("Amount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount Amount { get; set; } = new();

    [XmlElement("BaseAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount? BaseAmount { get; set; }
}

/// <summary>
/// UBL Ürün/Hizmet bilgisi
/// </summary>
public class UblItem
{
    [XmlElement("Description", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? Description { get; set; }

    [XmlElement("Name", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("SellersItemIdentification", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")]
    public UblItemIdentification? SellersItemIdentification { get; set; }
}

/// <summary>
/// UBL Ürün kimlik bilgisi
/// </summary>
public class UblItemIdentification
{
    [XmlElement("ID", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public string? Id { get; set; }
}

/// <summary>
/// UBL Fiyat bilgisi
/// </summary>
public class UblPrice
{
    [XmlElement("PriceAmount", Namespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")]
    public UblAmount PriceAmount { get; set; } = new();
}

#endregion

#region Yardımcı Sınıflar

/// <summary>
/// GİB Vergi kodları
/// </summary>
public static class GibVergiKodlari
{
    public const string KDV = "0015";           // Katma Değer Vergisi
    public const string OTV_I = "0071";         // ÖTV I. Liste
    public const string OTV_II = "0073";        // ÖTV II. Liste
    public const string OTV_III = "0074";       // ÖTV III. Liste
    public const string OTV_IV = "0075";        // ÖTV IV. Liste
    public const string DAMGA = "0021";         // Damga Vergisi
    public const string OZELTUKETIM = "4171";   // Özel Tüketim Vergisi
    public const string KONAKLAMA = "0059";     // Konaklama Vergisi
}

/// <summary>
/// GİB Tevkifat kodları
/// </summary>
public static class GibTevkifatKodlari
{
    public const string YAPI_DENETIM = "601";
    public const string ETUT_PROJE = "602";
    public const string YEMEK = "603";
    public const string MAKINA_TEÇHIZAT = "606";
    public const string INSAAT = "607";
    public const string PERSONEL_HIZMET = "608";
    public const string YAPIM_ISLERI = "610";
    public const string BAKIM_ONARIM = "612";
    public const string FASON_TEKSTIL = "613";
    public const string BINA_TEMIZLIK = "614";
    public const string ISGUCU_TEMIN = "615";
    public const string TURIZM = "617";
    public const string TASINIR_KIRA = "619";
    public const string SERVIS_TASIMACILIGI = "620";
    public const string HURDA_METAL = "621";
    public const string HURDA_PLASTIK = "622";
    public const string ATIK_KAGIT = "623";
    public const string HURDA_CAM = "624";
    public const string PAMUK_TUY = "625";
    public const string BUYUK_TIC_HAYVAN = "626";
    public const string KUCUK_TIC_HAYVAN = "627";
    public const string ET_URUNLERI = "628";
    public const string SUT_URUNLERI = "629";
    public const string DEMIRBAS_TESLIM = "630";
    public const string REKLAM_HIZMET = "631";
    public const string SPOR_KULUP = "632";
    public const string DIGER = "699";
}

/// <summary>
/// UBL-TR Birim kodları (UN/ECE Rec. 20)
/// </summary>
public static class UblBirimKodlari
{
    public static readonly Dictionary<string, string> BirimEslestirme = new()
    {
        { "Adet", "C62" },
        { "ADET", "C62" },
        { "adet", "C62" },
        { "AD", "C62" },
        { "Kg", "KGM" },
        { "KG", "KGM" },
        { "kg", "KGM" },
        { "Kilogram", "KGM" },
        { "Lt", "LTR" },
        { "LT", "LTR" },
        { "lt", "LTR" },
        { "Litre", "LTR" },
        { "Mt", "MTR" },
        { "MT", "MTR" },
        { "Metre", "MTR" },
        { "M2", "MTK" },
        { "m2", "MTK" },
        { "Metrekare", "MTK" },
        { "M3", "MTQ" },
        { "m3", "MTQ" },
        { "Metreküp", "MTQ" },
        { "Ton", "TNE" },
        { "TON", "TNE" },
        { "Paket", "PA" },
        { "PAKET", "PA" },
        { "Kutu", "BX" },
        { "KUTU", "BX" },
        { "Saat", "HUR" },
        { "SAAT", "HUR" },
        { "Gün", "DAY" },
        { "GÜN", "DAY" },
        { "Ay", "MON" },
        { "AY", "MON" },
        { "Yıl", "ANN" },
        { "YIL", "ANN" },
        { "Takım", "SET" },
        { "TAKIM", "SET" },
        { "Set", "SET" },
        { "Koli", "CS" },
        { "KOLI", "CS" },
        { "Sefer", "C62" },
        { "SEFER", "C62" }
    };

    public static string GetUblKod(string birim)
    {
        return BirimEslestirme.TryGetValue(birim, out var kod) ? kod : "C62"; // Varsayılan: Adet
    }
}

/// <summary>
/// E-Fatura tip kodları
/// </summary>
public static class EFaturaTipKodlari
{
    public const string SATIS = "SATIS";
    public const string IADE = "IADE";
    public const string TEVKIFAT = "TEVKIFAT";
    public const string ISTISNA = "ISTISNA";
    public const string OZELMATRAH = "OZELMATRAH";
    public const string IHRACAT = "IHRACATKAYITLI";
}

/// <summary>
/// Profil ID'leri (Senaryo)
/// </summary>
public static class EFaturaProfilIdleri
{
    public const string TEMEL = "TEMELFATURA";
    public const string TICARI = "TICARIFATURA";
    public const string IHRACAT = "IHRACATKAYITLI";
    public const string YOLCU_BERABER = "YOLCUBERABERFATURA";
    public const string E_ARSIV = "EARSIVFATURA";
}

#endregion


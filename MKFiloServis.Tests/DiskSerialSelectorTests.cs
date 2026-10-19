using MKFiloServis.Shared.Licensing;

namespace MKFiloServis.Tests;

/// <summary>
/// Makine kodu (dolayısıyla lisans) USB bellek takılıp çıkarılınca değişmemeli. Gerçek makinede USB bellek WMI
/// listesinde dahili diskten ÖNCE geliyordu; eskiden ilk boş olmayan seri numarası olarak seçilip kodu değiştiriyordu.
/// </summary>
public sealed class DiskSerialSelectorTests
{
    private const string Usb = @"\\.\PHYSICALDRIVE1";
    private const string Dahili = @"\\.\PHYSICALDRIVE0";

    private static ISet<string> UsbKimlikleri(params string[] kimlikler)
        => new HashSet<string>(kimlikler, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Usb_bellek_listede_dahili_diskten_once_gelse_bile_dahili_diskin_seri_numarasi_secilir()
    {
        var ortamlar = new (string? Tag, string? Serial)[] { (Usb, "USB-SERI"), (Dahili, "NVME-SERI") };

        Assert.Equal("NVME-SERI", DiskSerialSelector.Pick(ortamlar, UsbKimlikleri(Usb)));
    }

    [Fact]
    public void Makine_kodu_usb_takili_olsa_da_olmasa_da_ayni_diskten_uretilir()
    {
        var usbTakili = new (string? Tag, string? Serial)[] { (Usb, "USB-SERI"), (Dahili, "NVME-SERI") };
        var usbCikarilmis = new (string? Tag, string? Serial)[] { (Dahili, "NVME-SERI") };

        Assert.Equal(
            DiskSerialSelector.Pick(usbCikarilmis, UsbKimlikleri()),
            DiskSerialSelector.Pick(usbTakili, UsbKimlikleri(Usb)));
    }

    [Fact]
    public void Usb_yokken_eski_davranis_korunur_ilk_bos_olmayan_seri_kirpilarak_alinir()
    {
        var ortamlar = new (string? Tag, string? Serial)[]
        {
            (@"\\.\PHYSICALDRIVE0", "   "),
            (@"\\.\PHYSICALDRIVE1", null),
            (@"\\.\PHYSICALDRIVE2", "  SERI-2  "),
            (@"\\.\PHYSICALDRIVE3", "SERI-3"),
        };

        Assert.Equal("SERI-2", DiskSerialSelector.Pick(ortamlar, UsbKimlikleri()));
    }

    [Fact]
    public void Yalniz_usb_disk_varsa_secilecek_seri_numarasi_yoktur()
    {
        var ortamlar = new (string? Tag, string? Serial)[] { (Usb, "USB-SERI") };

        Assert.Null(DiskSerialSelector.Pick(ortamlar, UsbKimlikleri(Usb)));
    }

    [Fact]
    public void Etiket_eslesmesi_buyuk_kucuk_harfe_bakmaz()
    {
        var ortamlar = new (string? Tag, string? Serial)[] { (Usb, "USB-SERI"), (Dahili, "NVME-SERI") };

        Assert.Equal("NVME-SERI", DiskSerialSelector.Pick(ortamlar, UsbKimlikleri(Usb.ToLowerInvariant())));
    }

    [Fact]
    public void Etiketi_olmayan_ortam_usb_oldugu_bilinemedigi_icin_atlanmaz()
    {
        var ortamlar = new (string? Tag, string? Serial)[] { (null, "ETIKETSIZ"), (Dahili, "NVME-SERI") };

        Assert.Equal("ETIKETSIZ", DiskSerialSelector.Pick(ortamlar, UsbKimlikleri(Usb)));
    }

    [Theory]
    [InlineData("USB", @"USBSTOR\DISK&VEN_KINGSTON&PROD_DATATRAVELER_3.0&REV_PMAP\0000", true)]   // örnek USB bellek
    [InlineData(" usb ", null, true)]
    [InlineData(null, @"USBSTOR\DISK&VEN_X&PROD_Y&REV_1\0", true)]
    [InlineData("SCSI", @"SCSI\DISK&VEN_NVME&PROD_SANDISK_PC_SN5000S\5&1A2B3C&0&000000", false)]  // örnek dahili NVMe
    [InlineData("IDE", @"IDE\DISKWDC_WD10EZEX\5&0&0", false)]
    [InlineData(null, null, false)]
    public void Usb_veri_yolu_yalniz_arayuz_ve_pnp_kimligine_gore_taninir(string? arayuz, string? pnp, bool beklenen)
    {
        Assert.Equal(beklenen, DiskSerialSelector.IsUsbDrive(arayuz, pnp));
    }
}

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    DPAPI Master Key Recovery & Migration Guide

.DESCRIPTION
    KOAFiloServis'te eski master key ile şifreli dosyaları kurtarma prosedürü.
    Master key değiştiğinde çalıştırılacak adımlar ve tanı bilgileri.

.NOTES
    İçerik:
    1. Problem Tanı
    2. Hızlı Çözüm (Eski Key Restore)
    3. Alternatif: Eski Dosya Migrasyonu
    4. İleriye Dönük Koruma
#>

Write-Host "=" * 80
Write-Host "🔐 KOAFiloServis - DPAPI Master Key Recovery Guide" -ForegroundColor Cyan
Write-Host "=" * 80
Write-Host ""

# ============================================================================
Write-Host "1️⃣  PROBLEM TANI" -ForegroundColor Green
Write-Host "=" * 80
Write-Host ""
Write-Host "Belirtiler:"
Write-Host "  ❌ Dosya dekrypt başarısız: 'Belirtilen durumda kullanım için anahtar geçerli değil'"
Write-Host "  ❌ Eski dosyalar açılmıyor (sadece yeni kaydedilen dosyalar açılıyor)"
Write-Host "  ❌ Application logs'ta: 'DPAPI LocalMachine ve CurrentUser scope denemeleri basarisiz'"
Write-Host ""
Write-Host "Sebepleri:"
Write-Host "  • Master key dosyası korumalı (DPAPI), farklı kullanıcı/makine ortamında çöp gibi gelir"
Write-Host "  • Eski makinedeki key bozulmuş/kayıp"
Write-Host "  • Farklı Windows kurulumunda dosya taşınmış"
Write-Host ""

# ============================================================================
Write-Host ""
Write-Host "2️⃣  HIZLI ÇÖZÜM: ESKİ KEY RESTORE" -ForegroundColor Green
Write-Host "=" * 80
Write-Host ""
Write-Host "Eğer eski master.key yedeklemesi varsa:"
Write-Host ""
Write-Host "  a) Mevcut (bozuk) master key'i yedekle:"
Write-Host "     > Copy-Item 'C:\KOAFiloServis_yedekleme\keys\master.key' " +
            "'C:\KOAFiloServis_yedekleme\keys\master.key.broken'"
Write-Host ""
Write-Host "  b) Eski yedekten restore et:"
Write-Host "     > Copy-Item '<BACKUP_PATH>\master.key' 'C:\KOAFiloServis_yedekleme\keys\master.key' -Force"
Write-Host ""
Write-Host "  c) Uygulamayı yeniden başlat:"
Write-Host "     > iisreset  (veya Blazor uygulaması restart)"
Write-Host ""
Write-Host "✅ Sonuç: Eski dosyalar yeniden açılabilir olacak"
Write-Host ""

# ============================================================================
Write-Host ""
Write-Host "3️⃣  ALTERNATİF: ESKİ DOSYA MİGRASYONU (Master Key Kaybı Durumu)" -ForegroundColor Green
Write-Host "=" * 80
Write-Host ""
Write-Host "⚠️  ESKİ KEY KURTARILAMADIYSALı (master.key permanent kayıp):"
Write-Host ""
Write-Host "  a) Eski şifreli dosyaları quarantine'e taşı:"
Write-Host "     > Move-Item 'C:\KOAFiloServis_yedekleme\Arsiv\Sifreli\*' " +
            "'C:\KOAFiloServis_yedekleme\Arsiv\Sifreli_BACKUP_$(Get-Date -f yyyyMMdd_HHmmss)' -Force"
Write-Host ""
Write-Host "  b) Sistem artık yeni (current) master key ile çalışacak:"
Write-Host "     ✓ Yeni dosyalar normal şekilde şifrelenir"
Write-Host "     ✗ Eski dosyalar açılamaz (key kaybı permanent)"
Write-Host ""
Write-Host "  c) Eski dosyalar manuel prosesle recover edilebilir (expert only):"
Write-Host "     - DPAPI unprotection yerine hardware key, yedeklemeler vb. kullan"
Write-Host "     - IT administrator veya Microsoft support ile danış"
Write-Host ""

# ============================================================================
Write-Host ""
Write-Host "4️⃣  İLERİYE DÖNÜK KORUMA (Best Practices)" -ForegroundColor Green
Write-Host "=" * 80
Write-Host ""
Write-Host "Master key sorunlarından kaçınmak için:"
Write-Host ""
Write-Host "✅ MASTER KEY YEDEĞI:"
Write-Host "   • Haftada 1x: Copy-Item 'C:\KOAFiloServis_yedekleme\keys\master.key' " +
            "'\\BACKUP_SERVER\KOA_Backups\keys\master.key.$(Get-Date -f yyyyMMdd)'"
Write-Host "   • Secure offline storage'da sakla (şifreli USB, vault)"
Write-Host ""
Write-Host "✅ MASTER KEY ROTASİON (Yıllık):"
Write-Host "   • Eski key safran sakla"
Write-Host "   • Yeni key oluştur"
Write-Host "   • Eski dosyaları yeniden şifrele (re-encrypt)"
Write-Host ""
Write-Host "✅ MONITORING:"
Write-Host "   • Dashboard: /api/system/decryption-recovery-status"
Write-Host "   • Alert: Hergün decrypt hataları varsa IT'ye bildir"
Write-Host ""
Write-Host "✅ ENVIRONMENT STABILIZATION:"
Write-Host "   • Production: Farklı user/makine ortamında test et"
Write-Host "   • DPAPI yerine: Environment variable veya HW key kullanı mı?"
Write-Host ""

# ============================================================================
Write-Host ""
Write-Host "5️⃣  DASHBOARD RECOVERY RAPORU" -ForegroundColor Green
Write-Host "=" * 80
Write-Host ""
Write-Host "Sistem sağlığını takip et:"
Write-Host ""
Write-Host "  GET /api/system/health-summary"
Write-Host "    • Status: 'OK' veya ⚠️ hata"
Write-Host "    • Kaç dosya açılamadığı"
Write-Host "    • Recovery attempt sayısı"
Write-Host ""
Write-Host "  GET /api/system/decryption-recovery-status"
Write-Host "    • Son 5 başarısız dosya listesi"
Write-Host "    • Hata nedenleri (master key mismatch, format error vb.)"
Write-Host ""

# ============================================================================
Write-Host ""
Write-Host "6️⃣  SORUN GİDERME (Troubleshooting)" -ForegroundColor Green
Write-Host "=" * 80
Write-Host ""
Write-Host "S: 'CurrentUser scope ile yüklendi' mesajı nedir?"
Write-Host "C: Eski ortamda CurrentUser DPAPI'si ile şifrelenmiş. Uyarı ama çalışıyor."
Write-Host ""
Write-Host "S: 'Belirtilen durumda kullanım için anahtar geçerli değil' hatası?"
Write-Host "C: Master key bozuk veya farklı user/makine. Eski key restore et."
Write-Host ""
Write-Host "S: Yeni dosyalar açılıyor ama eski açılmıyor?"
Write-Host "C: Normal. Master key yeni ortamda yenilendi. Eski key'i restore et."
Write-Host ""

Write-Host ""
Write-Host "=" * 80
Write-Host "📞 Daha fazla yardım: Check 'KOA' Development Team / IT Support"
Write-Host "=" * 80

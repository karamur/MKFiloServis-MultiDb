-- Puantaj Engine SQL Simulasyonu
-- OperasyonKaydi -> PuantajKayit + PuantajDetay + PuantajHesapDonemi

BEGIN;

-- HesapDonemi
INSERT INTO "PuantajHesapDonemleri" ("FirmaId","Yil","Ay","Versiyon","Durum","OnayDurum","HesaplamaTarihi","CreatedAt","IsDeleted")
VALUES (1,2026,5,1,1,0,NOW(),NOW(),false);

-- PuantajKayit: (GuzergahId, AracId, Slot) bazinda grupla
INSERT INTO "PuantajKayitlar" (
  "FirmaId","Yil","Ay","GuzergahId","AracId","SoforId","Slot","SlotAdi","Yon",
  "KurumId","FaturaKesiciCariId","OdemeYapilacakCariId",
  "Gun01","Gun02","Gun03","Gun04","Gun05","Gun06","Gun07","Gun08","Gun09","Gun10",
  "Gun11","Gun12","Gun13","Gun14","Gun15","Gun16","Gun17","Gun18","Gun19","Gun20",
  "Gun21","Gun22","Gun23","Gun24","Gun25","Gun26","Gun27","Gun28","Gun29","Gun30","Gun31",
  "Gun","SeferSayisi","ToplamCalismaGunu",
  "BirimGelir","ToplamGelir","BirimGider","ToplamGider",
  "GelirKdvOrani","GelirKdvOrani20","GelirKdv20Tutari","GelirKdvOrani10","GelirKdv10Tutari",
  "GelirKdvTutari","GelirToplam","GelirKesinti","Alinacak",
  "GiderKdvOrani10","GiderKdvOrani20","GiderKdv10Tutari","GiderKdv20Tutari",
  "GiderKesinti","Odenecek","GelirFaturaKesildi","GiderFaturaAlindi",
  "GelirOdenenTutar","GiderOdenenTutar",
  "OnayDurum","KaynakTipi","FinansYonu","SoforOdemeTipi","Kaynak","Versiyon",
  "SiraNo","HesapDonemiId","CreatedAt","IsDeleted"
)
SELECT
  1,2026,5,
  o."GuzergahId", o."AracId", 0, o."Slot",
  CASE o."Slot" WHEN 1 THEN 'Sabah' WHEN 2 THEN 'Aksam' ELSE 'Diger' END,
  CASE o."Slot" WHEN 1 THEN 1 WHEN 2 THEN 2 ELSE 9 END,
  MAX(o."KurumId"), MAX(o."FaturaKesiciCariId"), MAX(o."OdemeYapilacakCariId"),

  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=1  AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=2  AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=3  AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=4  AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=5  AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=6  AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=7  AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=8  AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=9  AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=10 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=11 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=12 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=13 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=14 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=15 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=16 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=17 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=18 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=19 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=20 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=21 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=22 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=23 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=24 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=25 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=26 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=27 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=28 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=29 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=30 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),
  COALESCE(SUM(CASE WHEN EXTRACT(DAY FROM o."Tarih")=31 AND o."OperasyonDurumu"=1 THEN o."SeferSayisi"*o."PuantajCarpani" ELSE 0 END)::int,0),

  COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0)::decimal AS gun,
  COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0)::int AS sefer,
  COUNT(*) FILTER (WHERE o."OperasyonDurumu"=1)::int AS calisma_gunu,

  1500.00, 1500.00 * COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0),
  800.00,  800.00  * COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0),

  20,20, (1500*COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0))*0.20, 10,0,
  (1500*COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0))*0.20,
  (1500*COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0))*1.20,
  0,
  (1500*COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0))*1.20,

  10,20,0,(800*COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0))*0.20,
  0,(800*COALESCE(SUM(o."SeferSayisi"*o."PuantajCarpani") FILTER (WHERE o."OperasyonDurumu"=1),0))*1.20,
  false,false,
  0,0,
  2,1,2,1,0,1,
  0,(SELECT "Id" FROM "PuantajHesapDonemleri" WHERE "Yil"=2026 AND "Ay"=5 LIMIT 1),NOW(),false
FROM "OperasyonKayitlari" o
WHERE o."Tarih" >= '2026-05-01' AND o."Tarih" <= '2026-05-31' AND o."IsDeleted"=false
GROUP BY o."GuzergahId", o."AracId", o."Slot";

COMMIT;

SELECT 'PuantajKayit: ' || COUNT(*) FROM "PuantajKayitlar" WHERE "Yil"=2026 AND "Ay"=5;

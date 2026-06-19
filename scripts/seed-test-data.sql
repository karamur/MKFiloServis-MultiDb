BEGIN;

-- Guzergah (CariId=608 RECEP USTUN)
INSERT INTO "Guzergahlar" ("FirmaId", "GuzergahAdi", "GuzergahKodu", "CariId", "BirimFiyat", "GiderFiyat", "Aktif", "CreatedAt", "IsDeleted")
VALUES (1, 'Sincan - Merkez', 'SINCAN01', 608, 1500.00, 800.00, true, NOW(), false);

-- OperasyonKaydi kolonlari:
-- FirmaId=1, Tarih, GuzergahId, AracId, Slot, SeferSayisi, PuantajCarpani=1.0,
-- KurumId=1, FaturaKesiciCariId=608, OperasyonDurumu, KaynakTipi=1, FinansYonu=2,
-- SoforOdemeTipi=1, Kaynak=0, Yon=3, KullaniciKilitliMi=false, CreatedAt, IsDeleted=false

-- 01-02 May: Normal (2 sefer, Arac 06C0640 Id=25)
INSERT INTO "OperasyonKayitlari" ("FirmaId","Tarih","GuzergahId","AracId","Slot","SeferSayisi","PuantajCarpani","KurumId","FaturaKesiciCariId","OperasyonDurumu","KaynakTipi","FinansYonu","SoforOdemeTipi","Kaynak","Yon","KullaniciKilitliMi","CreatedAt","IsDeleted")
SELECT 1,d,g,25,s, 1, 1.0, 1,608, 1,1,2, 1,0,3, false,NOW(),false
FROM generate_series('2026-05-01'::date,'2026-05-02'::date,'1 day') d,
     generate_series(1,2) s,
     (SELECT "Id" AS g FROM "Guzergahlar" WHERE "GuzergahKodu"='SINCAN01') gg;

-- 03 May: ARAC ARIZA, yedek arac (06C1333 Id=28) calisti
INSERT INTO "OperasyonKayitlari" ("FirmaId","Tarih","GuzergahId","AracId","Slot","SeferSayisi","PuantajCarpani","KurumId","FaturaKesiciCariId","OperasyonDurumu","KaynakTipi","FinansYonu","SoforOdemeTipi","Kaynak","Yon","KullaniciKilitliMi","CreatedAt","IsDeleted")
SELECT 1,'2026-05-03',g,28,s, 1, 1.0, 1,608, 1,1,2, 1,0,3, false,NOW(),false
FROM generate_series(1,2) s,
     (SELECT "Id" AS g FROM "Guzergahlar" WHERE "GuzergahKodu"='SINCAN01') gg;

-- 04 May: Normal + EK MESAI gunu
INSERT INTO "OperasyonKayitlari" ("FirmaId","Tarih","GuzergahId","AracId","Slot","SeferSayisi","PuantajCarpani","KurumId","FaturaKesiciCariId","OperasyonDurumu","KaynakTipi","FinansYonu","SoforOdemeTipi","Kaynak","Yon","KullaniciKilitliMi","CreatedAt","IsDeleted")
SELECT 1,'2026-05-04',g,25,s, 1, 1.0, 1,608, 1,1,2, 1,0,3, false,NOW(),false
FROM generate_series(1,2) s,
     (SELECT "Id" AS g FROM "Guzergahlar" WHERE "GuzergahKodu"='SINCAN01') gg;

-- 05 May: ARAC GELMEDI, TAKSIYLE GIDILDI (OperasyonDurumu=4)
INSERT INTO "OperasyonKayitlari" ("FirmaId","Tarih","GuzergahId","AracId","Slot","SeferSayisi","PuantajCarpani","KurumId","FaturaKesiciCariId","OperasyonDurumu","KaynakTipi","FinansYonu","SoforOdemeTipi","Kaynak","Yon","KullaniciKilitliMi","CreatedAt","IsDeleted")
SELECT 1,'2026-05-05',g,25,s, 0, 1.0, 1,608, 4,1,2, 1,0,3, false,NOW(),false
FROM generate_series(1,2) s,
     (SELECT "Id" AS g FROM "Guzergahlar" WHERE "GuzergahKodu"='SINCAN01') gg;

-- 06-31 May: Normal calisma
INSERT INTO "OperasyonKayitlari" ("FirmaId","Tarih","GuzergahId","AracId","Slot","SeferSayisi","PuantajCarpani","KurumId","FaturaKesiciCariId","OperasyonDurumu","KaynakTipi","FinansYonu","SoforOdemeTipi","Kaynak","Yon","KullaniciKilitliMi","CreatedAt","IsDeleted")
SELECT 1,d,g,25,s, 1, 1.0, 1,608, 1,1,2, 1,0,3, false,NOW(),false
FROM generate_series('2026-05-06'::date,'2026-05-31'::date,'1 day') d,
     generate_series(1,2) s,
     (SELECT "Id" AS g FROM "Guzergahlar" WHERE "GuzergahKodu"='SINCAN01') gg;

COMMIT;

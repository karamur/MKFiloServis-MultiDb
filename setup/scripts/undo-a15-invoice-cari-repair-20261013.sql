-- Restore only the 17 invoices changed by repair-a15-invoice-cari-20261013.sql.
-- Run with psql against MKFiloServis after a full backup. One transaction.
\set ON_ERROR_STOP on
BEGIN;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '2min';
LOCK TABLE "Faturalar", "FaturaKalemleri", "MuhasebeFisleri",
           "MuhasebeFisKalemleri", "Cariler" IN SHARE ROW EXCLUSIVE MODE;

CREATE TEMP TABLE a15_original_invoice (id integer PRIMARY KEY, updated_at timestamp without time zone) ON COMMIT DROP;
INSERT INTO a15_original_invoice VALUES
 (1209,'2026-04-28 08:21:26.064528'),
 (1221,'2026-04-28 08:21:26.193494'),
 (1222,'2026-04-28 08:21:26.205050'),
 (1223,'2026-04-28 08:21:26.214628'),
 (1224,'2026-04-28 08:21:26.227185'),
 (1225,'2026-04-28 08:21:26.238560'),
 (1226,'2026-04-28 08:21:26.248553'),
 (1234,'2026-04-28 08:21:26.328975'),
 (1242,'2026-04-28 08:21:26.419225'),
 (1243,'2026-04-28 08:21:26.429238'),
 (1244,'2026-04-28 08:21:26.440516'),
 (1298,'2026-04-28 08:43:26.776720'),
 (1299,'2026-04-28 08:43:26.788145'),
 (1300,'2026-04-28 08:43:26.797170'),
 (1301,'2026-04-28 08:43:26.808480'),
 (1302,'2026-04-28 08:43:26.819369'),
 (1303,'2026-04-28 08:43:26.829944');

DO $guard$
DECLARE repair_time timestamp := '2026-10-13 09:58:42.189344';
BEGIN
  IF current_database() <> 'MKFiloServis' THEN
    RAISE EXCEPTION 'Wrong database: %', current_database();
  END IF;
  IF (SELECT count(*) FROM a15_original_invoice) <> 17
     OR (SELECT count(*) FROM "Faturalar" f JOIN a15_original_invoice o ON o.id=f."Id"
         WHERE f."UpdatedAt"=repair_time AND f."CariId" IS NOT NULL
           AND ((f."Id"=1209 AND f."FirmaId"=3 AND f."IsDeleted" AND f."Durum"=4
                 AND f."DeletedAt"=repair_time AND f."MuhasebeFisId"=283)
             OR (f."Id"<>1209 AND f."FirmaId"=1 AND NOT f."IsDeleted"))) <> 17 THEN
    RAISE EXCEPTION 'Invoice state differs from the verified A-15 repair';
  END IF;
  IF (SELECT count(*) FROM "FaturaKalemleri" k JOIN a15_original_invoice o ON o.id=k."FaturaId"
      WHERE k."UpdatedAt"=repair_time AND
            ((k."FaturaId"=1209 AND k."FirmaId"=3 AND k."IsDeleted" AND k."DeletedAt"=repair_time)
              OR (k."FaturaId"<>1209 AND k."FirmaId"=1 AND NOT k."IsDeleted"))) <> 18 THEN
    RAISE EXCEPTION 'Invoice line state differs from the verified A-15 repair';
  END IF;
  IF (SELECT count(*) FROM "FaturaKalemleri" k JOIN a15_original_invoice o ON o.id=k."FaturaId") <> 18 THEN
    RAISE EXCEPTION 'Invoice line count changed after the repair';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM "MuhasebeFisleri" WHERE "Id"=621
      AND "FisNo"='A15-REV-283-20261013' AND "KaynakTip"='IptalKaydi'
      AND "KaynakId"=283 AND "Durum"=2 AND NOT "IsDeleted"
      AND "CreatedAt"=repair_time)
     OR (SELECT count(*) FROM "MuhasebeFisKalemleri" WHERE "FisId"=621
         AND "CreatedAt"=repair_time AND NOT "IsDeleted") <> 3
     OR (SELECT count(*) FROM "MuhasebeFisKalemleri" WHERE "FisId"=621) <> 3 THEN
    RAISE EXCEPTION 'Reversal voucher 621 differs from the verified A-15 repair';
  END IF;
  IF EXISTS (SELECT 1 FROM "AracMasraflari" WHERE "MuhasebeFisId"=621)
     OR EXISTS (SELECT 1 FROM "BankaKasaHareketleri" WHERE "MuhasebeFisId"=621)
     OR EXISTS (SELECT 1 FROM "BordroOdemeler" WHERE "MuhasebeFisId"=621)
     OR EXISTS (SELECT 1 FROM "HakedisPuantajlar" WHERE "GelirFisId"=621 OR "GiderFisId"=621)
     OR EXISTS (SELECT 1 FROM "PersonelBorclar" WHERE "MuhasebeFisId"=621)
     OR EXISTS (SELECT 1 FROM "PersonelBorcOdemeler" WHERE "MuhasebeFisId"=621)
     OR EXISTS (SELECT 1 FROM "Faturalar" WHERE "MuhasebeFisId"=621 OR "IptalFisId"=621) THEN
    RAISE EXCEPTION 'Voucher 621 is referenced by later records';
  END IF;
  IF EXISTS (SELECT 1 FROM "Faturalar" f JOIN "Faturalar" other
      ON other."Id"<>f."Id" AND other."FirmaId"=3 AND NOT other."IsDeleted"
      AND other."FaturaNo"=f."FaturaNo" AND other."FaturaYonu"=f."FaturaYonu"
      JOIN a15_original_invoice o ON o.id=f."Id") THEN
    RAISE EXCEPTION 'Invoice number collision in firm 3';
  END IF;
END $guard$;

DROP TRIGGER IF EXISTS "TR_A15_Fatura_CariFirma" ON "Faturalar";
DROP TRIGGER IF EXISTS "TR_A15_Cari_Firma_Fatura" ON "Cariler";
DROP FUNCTION IF EXISTS a15_guard_fatura_cari_firma();
DROP FUNCTION IF EXISTS a15_guard_cari_firma_fatura();

UPDATE "Faturalar" f SET "FirmaId"=3, "UpdatedAt"=o.updated_at,
  "IsDeleted"=FALSE, "Durum"=CASE WHEN f."Id"=1209 THEN 1 ELSE f."Durum" END,
  "DeletedAt"=CASE WHEN f."Id"=1209 THEN NULL ELSE f."DeletedAt" END
FROM a15_original_invoice o WHERE f."Id"=o.id;

UPDATE "FaturaKalemleri" k SET "FirmaId"=3, "UpdatedAt"=NULL,
  "IsDeleted"=FALSE, "DeletedAt"=NULL
FROM a15_original_invoice o WHERE k."FaturaId"=o.id;

DELETE FROM "MuhasebeFisKalemleri" WHERE "FisId"=621;
DELETE FROM "MuhasebeFisleri" WHERE "Id"=621;

DO $check$
BEGIN
  IF (SELECT count(*) FROM "Faturalar" f JOIN a15_original_invoice o ON o.id=f."Id"
      WHERE f."FirmaId"=3 AND NOT f."IsDeleted" AND f."UpdatedAt"=o.updated_at) <> 17
    OR (SELECT count(*) FROM "FaturaKalemleri" k JOIN a15_original_invoice o ON o.id=k."FaturaId"
      WHERE k."FirmaId"=3 AND NOT k."IsDeleted" AND k."UpdatedAt" IS NULL) <> 18
    OR EXISTS (SELECT 1 FROM "MuhasebeFisleri" WHERE "Id"=621)
    OR EXISTS (SELECT 1 FROM pg_trigger WHERE tgname IN ('TR_A15_Fatura_CariFirma','TR_A15_Cari_Firma_Fatura')
               AND NOT tgisinternal) THEN
    RAISE EXCEPTION 'A-15 rollback verification failed';
  END IF;
END $check$;
COMMIT;

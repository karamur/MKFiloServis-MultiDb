-- Restores eight EF model foreign keys without changing existing business rows.
-- Seven constraints remain NOT VALID because parent rows are missing. Repair
-- those rows with verified business mappings, then VALIDATE each constraint.
\set ON_ERROR_STOP on
BEGIN;
SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '2min';

DO $guard$
DECLARE existing_count integer;
BEGIN
  IF current_database() <> 'MKFiloServis' THEN
    RAISE EXCEPTION 'Wrong database: %', current_database();
  END IF;
  SELECT count(*) INTO existing_count FROM pg_constraint
  WHERE conname IN (
    'FK_BankaHesaplari_Firmalar_FirmaId',
    'FK_Cariler_MuhasebeHesaplari_PersonelAvansHesapId',
    'FK_FaturaKalemleri_Firmalar_FirmaId',
    'FK_HoldingVeriler_Firmalar_FirmaId',
    'FK_MuhasebeFisKalemleri_MuhasebeHesaplari_HesapId',
    'FK_PersonelAvanslar_MuhasebeFisleri_MuhasebeFisId',
    'FK_PuantajIstisnalar_PuantajKayitlar_PuantajKayitId',
    'FK_RolYetkileri_Roller_RolId');
  IF existing_count <> 0 THEN
    RAISE EXCEPTION 'Expected all eight foreign keys to be missing; % already exist', existing_count;
  END IF;
END $guard$;

ALTER TABLE "BankaHesaplari"
  ADD CONSTRAINT "FK_BankaHesaplari_Firmalar_FirmaId"
  FOREIGN KEY ("FirmaId") REFERENCES "Firmalar"("Id") ON DELETE RESTRICT NOT VALID;
ALTER TABLE "Cariler"
  ADD CONSTRAINT "FK_Cariler_MuhasebeHesaplari_PersonelAvansHesapId"
  FOREIGN KEY ("PersonelAvansHesapId") REFERENCES "MuhasebeHesaplari"("Id") ON DELETE SET NULL NOT VALID;
ALTER TABLE "FaturaKalemleri"
  ADD CONSTRAINT "FK_FaturaKalemleri_Firmalar_FirmaId"
  FOREIGN KEY ("FirmaId") REFERENCES "Firmalar"("Id") ON DELETE CASCADE NOT VALID;
ALTER TABLE "HoldingVeriler"
  ADD CONSTRAINT "FK_HoldingVeriler_Firmalar_FirmaId"
  FOREIGN KEY ("FirmaId") REFERENCES "Firmalar"("Id") ON DELETE RESTRICT NOT VALID;
ALTER TABLE "MuhasebeFisKalemleri"
  ADD CONSTRAINT "FK_MuhasebeFisKalemleri_MuhasebeHesaplari_HesapId"
  FOREIGN KEY ("HesapId") REFERENCES "MuhasebeHesaplari"("Id") ON DELETE RESTRICT NOT VALID;
ALTER TABLE "PersonelAvanslar"
  ADD CONSTRAINT "FK_PersonelAvanslar_MuhasebeFisleri_MuhasebeFisId"
  FOREIGN KEY ("MuhasebeFisId") REFERENCES "MuhasebeFisleri"("Id");
ALTER TABLE "PuantajIstisnalar"
  ADD CONSTRAINT "FK_PuantajIstisnalar_PuantajKayitlar_PuantajKayitId"
  FOREIGN KEY ("PuantajKayitId") REFERENCES "PuantajKayitlar"("Id") ON DELETE RESTRICT NOT VALID;
ALTER TABLE "RolYetkileri"
  ADD CONSTRAINT "FK_RolYetkileri_Roller_RolId"
  FOREIGN KEY ("RolId") REFERENCES "Roller"("Id") ON DELETE CASCADE NOT VALID;

DO $check$
BEGIN
  IF (SELECT count(*) FROM pg_constraint WHERE contype='f' AND conname IN (
    'FK_BankaHesaplari_Firmalar_FirmaId',
    'FK_Cariler_MuhasebeHesaplari_PersonelAvansHesapId',
    'FK_FaturaKalemleri_Firmalar_FirmaId',
    'FK_HoldingVeriler_Firmalar_FirmaId',
    'FK_MuhasebeFisKalemleri_MuhasebeHesaplari_HesapId',
    'FK_PersonelAvanslar_MuhasebeFisleri_MuhasebeFisId',
    'FK_PuantajIstisnalar_PuantajKayitlar_PuantajKayitId',
    'FK_RolYetkileri_Roller_RolId')) <> 8 THEN
    RAISE EXCEPTION 'Foreign key installation incomplete';
  END IF;
END $check$;
COMMIT;

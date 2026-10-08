-- Generated write-audit bootstrap from MKFiloServis.Shared/Auditing/postgres-write-audit.sql.
-- Execute with psql; errors stop before subsequent writes.
\set ON_ERROR_STOP on
-- MKFiloServis transactional write audit. Reserved schema is not business backup data.
CREATE SCHEMA IF NOT EXISTS mk_audit;
REVOKE ALL ON SCHEMA mk_audit FROM PUBLIC;
CREATE TABLE IF NOT EXISTS mk_audit.write_journal (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    recorded_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    transaction_id bigint NOT NULL DEFAULT txid_current(),
    database_actor text NOT NULL DEFAULT session_user,
    application_name text,
    table_name text NOT NULL,
    operation text NOT NULL,
    firma_id text,
    entity_id text,
    old_values jsonb,
    new_values jsonb,
    affected_rows bigint
);
CREATE INDEX IF NOT EXISTS ix_write_journal_time ON mk_audit.write_journal(recorded_at);
REVOKE ALL ON mk_audit.write_journal FROM PUBLIC;

CREATE OR REPLACE FUNCTION mk_audit.redact(document jsonb) RETURNS jsonb
LANGUAGE plpgsql IMMUTABLE SET search_path = pg_catalog AS $function$
DECLARE result jsonb := '{}'::jsonb; item record;
BEGIN
    IF document IS NULL THEN RETURN NULL; END IF;
    FOR item IN SELECT key, value FROM jsonb_each(document) LOOP
        -- Value/binary/free-form payload columns can themselves contain credentials.
        IF item.key ~* '(password|sifre|parola|token|secret|connectionstring|key|lisansanahtar|imza|deger|value|payload|body|icerik|dosya|credential|pwd|pass|salt)' THEN
            result := result || jsonb_build_object(item.key, '[GIZLENDI]');
        ELSE
            result := result || jsonb_build_object(item.key, item.value);
        END IF;
    END LOOP;
    RETURN result;
END $function$;

CREATE OR REPLACE FUNCTION mk_audit.capture_write() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog AS $function$
DECLARE before_row jsonb; after_row jsonb; identity_row jsonb; row_count bigint;
BEGIN
    IF TG_OP = 'TRUNCATE' THEN
        EXECUTE format('SELECT count(*) FROM %I.%I', TG_TABLE_SCHEMA, TG_TABLE_NAME) INTO row_count;
    ELSE
        IF TG_OP <> 'INSERT' THEN before_row := to_jsonb(OLD); END IF;
        IF TG_OP <> 'DELETE' THEN after_row := to_jsonb(NEW); END IF;
        identity_row := coalesce(after_row, before_row);
        row_count := 1;
    END IF;
    INSERT INTO mk_audit.write_journal(application_name, table_name, operation, firma_id, entity_id, old_values, new_values, affected_rows)
    VALUES (current_setting('application_name', true), TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME, TG_OP,
        coalesce(identity_row->>'FirmaId', identity_row->>'IsverenFirmaId'), identity_row->>'Id',
        mk_audit.redact(before_row), mk_audit.redact(after_row), row_count);
    RETURN NULL;
END $function$;
REVOKE ALL ON FUNCTION mk_audit.capture_write() FROM PUBLIC;

CREATE OR REPLACE FUNCTION mk_audit.protect_journal() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog AS $function$
BEGIN RAISE EXCEPTION 'MKFiloServis write journal is append-only'; END $function$;
CREATE OR REPLACE FUNCTION mk_audit.install() RETURNS void
LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog AS $install$
DECLARE item record;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgrelid='mk_audit.write_journal'::regclass AND tgname='mk_audit_immutable') THEN
        CREATE TRIGGER mk_audit_immutable BEFORE UPDATE OR DELETE OR TRUNCATE ON mk_audit.write_journal
        FOR EACH STATEMENT EXECUTE FUNCTION mk_audit.protect_journal();
    END IF;
    ALTER TABLE mk_audit.write_journal ENABLE ALWAYS TRIGGER mk_audit_immutable;
    FOR item IN SELECT n.nspname, c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
        WHERE n.nspname='public' AND c.relkind='r' AND c.relname NOT LIKE '__EFMigrations%'
        LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE t.tgname='mk_audit_row' AND c.relname=item.relname AND n.nspname=item.nspname) THEN
            EXECUTE format('CREATE TRIGGER mk_audit_row AFTER INSERT OR UPDATE OR DELETE ON %I.%I FOR EACH ROW EXECUTE FUNCTION mk_audit.capture_write()', item.nspname,item.relname);
        END IF;
        IF NOT EXISTS (SELECT 1 FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE t.tgname='mk_audit_truncate' AND c.relname=item.relname AND n.nspname=item.nspname) THEN
            EXECUTE format('CREATE TRIGGER mk_audit_truncate BEFORE TRUNCATE ON %I.%I FOR EACH STATEMENT EXECUTE FUNCTION mk_audit.capture_write()', item.nspname,item.relname);
        END IF;
        EXECUTE format('ALTER TABLE %I.%I ENABLE ALWAYS TRIGGER mk_audit_row',item.nspname,item.relname);
        EXECUTE format('ALTER TABLE %I.%I ENABLE ALWAYS TRIGGER mk_audit_truncate',item.nspname,item.relname);
    END LOOP;
END $install$;
SELECT mk_audit.install();
REVOKE ALL ON FUNCTION mk_audit.install() FROM PUBLIC;

-- End generated audit bootstrap; original maintenance body follows.
-- Puantaj Engine SQL Simulasyonu
-- OperasyonKaydi -> PuantajKayit + PuantajDetay + PuantajHesapDonemi

BEGIN;

-- HesapDonemi
DO $mk_write_bootstrap$ BEGIN PERFORM mk_audit.install(); END $mk_write_bootstrap$;
INSERT INTO "PuantajHesapDonemleri" ("FirmaId","Yil","Ay","Versiyon","Durum","OnayDurum","HesaplamaTarihi","CreatedAt","IsDeleted")
VALUES (1,2026,5,1,1,0,NOW(),NOW(),false);

-- PuantajKayit: (GuzergahId, AracId, Slot) bazinda grupla
DO $mk_write_bootstrap$ BEGIN PERFORM mk_audit.install(); END $mk_write_bootstrap$;
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

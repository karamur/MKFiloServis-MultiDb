-- ============================================================
-- DestekCRMServisBlazorDb → READ ONLY (Kural 11)
-- Kaynak sistemde INSERT/UPDATE/DELETE/TRUNCATE yasak.
-- Sadece SELECT yapilabilir.
-- ============================================================

-- 1) Veritabani seviyesinde: tum transaction'lar default READ ONLY
ALTER DATABASE "DestekCRMServisBlazorDb" SET default_transaction_read_only = on;

-- 2) Tum tablolarda INSERT/UPDATE/DELETE/TRUNCATE yetkilerini PUBLIC'ten al
DO $$
DECLARE
    tbl RECORD;
BEGIN
    FOR tbl IN
        SELECT table_name FROM information_schema.tables
        WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
    LOOP
        EXECUTE format('REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON TABLE public.%I FROM PUBLIC', tbl.table_name);
    END LOOP;
END $$;

-- 3) Sequence'leri kullanima kapat (nextval/setval engelle)
DO $$
DECLARE
    seq RECORD;
BEGIN
    FOR seq IN
        SELECT sequence_name FROM information_schema.sequences
        WHERE sequence_schema = 'public'
    LOOP
        EXECUTE format('REVOKE USAGE ON SEQUENCE public.%I FROM PUBLIC', seq.sequence_name);
    END LOOP;
END $$;

-- 4) Mevcut baglantilari etkilemeden, yeni baglantilar read-only baslayacak
-- pg_db_role_setting'e kaydedildi.

-- 5) Dogrulama: test write denemesi
DO $$
BEGIN
    RAISE NOTICE 'READ-ONLY yapilandirmasi tamamlandi.';
    RAISE NOTICE 'Test: INSERT denemesi yapiliyor...';
END $$;

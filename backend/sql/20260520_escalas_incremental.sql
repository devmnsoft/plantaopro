DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'plantaopro' AND table_name = 'escalas'
    ) THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='escalas' AND column_name='justificativa') THEN
            ALTER TABLE plantaopro.escalas ADD COLUMN justificativa text;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='plantaopro' AND table_name='escalas' AND column_name='reg_update') THEN
            ALTER TABLE plantaopro.escalas ADD COLUMN reg_update timestamp without time zone;
        END IF;
    END IF;
END $$;

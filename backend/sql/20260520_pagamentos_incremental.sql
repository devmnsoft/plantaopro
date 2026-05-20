ALTER TABLE IF EXISTS plantaopro.pagamentos ADD COLUMN IF NOT EXISTS valor_pago numeric(14,2);
ALTER TABLE IF EXISTS plantaopro.pagamentos ADD COLUMN IF NOT EXISTS data_pagamento date;
ALTER TABLE IF EXISTS plantaopro.pagamentos ADD COLUMN IF NOT EXISTS forma_pagamento varchar(50);
ALTER TABLE IF EXISTS plantaopro.pagamentos ADD COLUMN IF NOT EXISTS observacoes text;
ALTER TABLE IF EXISTS plantaopro.pagamentos ADD COLUMN IF NOT EXISTS reg_update timestamp;

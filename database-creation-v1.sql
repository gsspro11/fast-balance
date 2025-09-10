-- fast_balance_v1.products definition

CREATE TABLE fast_balance_v1.products (
	id int4 NOT NULL,
	description varchar(50) NOT NULL,
	abbreviation varchar(5) NULL,
	CONSTRAINT products_pk PRIMARY KEY (id)
);

-- fast_balance_v1.companies definition

CREATE TABLE fast_balance_v1.companies (
	id bigserial NOT NULL,
	"name" varchar(60) NOT NULL,
	identification varchar(18) NOT NULL,
	CONSTRAINT companies_pk PRIMARY KEY (id)
);

-- fast_balance_v1.credit_line_types definition

CREATE TABLE fast_balance_v1.credit_line_types (
	id int2 NOT NULL,
	type_name varchar(60) NOT NULL,
	CONSTRAINT credit_line_types_pkey PRIMARY KEY (id)
);

-- fast_balance_v1.accounts definition

CREATE TABLE fast_balance_v1.accounts (
	id bigserial NOT NULL,
	company_id int8 NOT NULL,	
	product_id int4 NOT NULL,
	credit_line_type_id int2 NOT NULL,
	customer_name varchar(80) NOT NULL,
	customer_identification varchar(18) NOT NULL,
	contract_number varchar(20) NULL,
	account_number int8 NOT NULL,
	balance int8 DEFAULT 0 NOT NULL,
	status varchar(20) NOT NULL,
	CONSTRAINT account_pk PRIMARY KEY (id),
	CONSTRAINT account_unique UNIQUE (account_number)
);

CREATE INDEX idx_accounts_customer_identification ON fast_balance_v1.accounts (customer_identification);
CREATE INDEX idx_accounts_customer_identification_status ON fast_balance_v1.accounts (customer_identification, status);

-- fast_balance_v1.accounts foreign keys

ALTER TABLE fast_balance_v1.accounts ADD CONSTRAINT accounts_products_fk FOREIGN KEY (product_id) REFERENCES fast_balance_v1.products(id);
ALTER TABLE fast_balance_v1.accounts ADD CONSTRAINT accounts_companies_fk FOREIGN KEY (company_id) REFERENCES fast_balance_v1.companies(id);
ALTER TABLE fast_balance_v1.accounts ADD CONSTRAINT accounts_credit_line_types_fk FOREIGN KEY (credit_line_type_id) REFERENCES fast_balance_v1.credit_line_types(id);

-- fast_balance_v1.credit_line_types definition

CREATE TABLE fast_balance_v1.tecnology_plastic_types (
	id serial NOT NULL,
	type_name varchar(60) NOT NULL,
	CONSTRAINT tecnology_plastic_types_pkey PRIMARY KEY (id)
);

-- fast_balance_v1.cards definition

CREATE TABLE fast_balance_v1.cards (
	id bigserial NOT NULL,
	tecnology_plastic_type_id int4 NOT NULL,
	account_id int8 NOT NULL,
	card_number int8 NOT NULL,
	CONSTRAINT cards_card_number_key UNIQUE (card_number),
	CONSTRAINT cards_pkey PRIMARY KEY (id)
);

CREATE INDEX idx_cards_account_id ON fast_balance_v1.cards (account_id);

-- fast_balance_v1.cards foreign keys


ALTER TABLE fast_balance_v1.cards ADD CONSTRAINT cards_accounts_fk FOREIGN KEY (account_id) REFERENCES fast_balance_v1.accounts(id);
ALTER TABLE fast_balance_v1.cards ADD CONSTRAINT cards_tecnology_plastic_types_fk FOREIGN KEY (tecnology_plastic_type_id) REFERENCES fast_balance_v1.tecnology_plastic_types(id);

-- fast_balance_v1.transaction_operations definition

CREATE TABLE fast_balance_v1.transaction_operations (
	id int8 NOT NULL,
	operation_name varchar(50) NOT NULL,
	transaction_type VARCHAR(1) NOT NULL,
	CONSTRAINT transaction_operations_pkey PRIMARY KEY (id),
	CONSTRAINT transaction_operations_transaction_type_check CHECK ((transaction_type = ANY (ARRAY['C'::VARCHAR, 'D'::VARCHAR])))
);

-- fast_balance_v1.transactions definition

CREATE TABLE fast_balance_v1.transactions (
	id bigserial NOT NULL,
	notification_id uuid NOT NULL,
	account_id int8 NOT NULL,
	card_id int8 NULL,
	operation_id int8 NOT NULL,
	product_id int4 NOT NULL,
	transaction_date timestamptz NOT NULL,
	amount int8 NOT NULL,
	balance_after_transaction int8 NULL,
	description text NULL,
	nsu int8 NULL,
	created_date timestamptz NOT NULL,
	CONSTRAINT transactions_amount_check CHECK ((amount > (0)::int8))
)
PARTITION BY RANGE (transaction_date);

-- fast_balance_v1.transactions foreign keys

ALTER TABLE fast_balance_v1.transactions ADD CONSTRAINT transactions_pk PRIMARY KEY (id, transaction_date);
ALTER TABLE fast_balance_v1.transactions ADD CONSTRAINT transactions_card_id_fkey FOREIGN KEY (card_id) REFERENCES fast_balance_v1.cards(id);
ALTER TABLE fast_balance_v1.transactions ADD CONSTRAINT transactions_products_fk FOREIGN KEY (product_id) REFERENCES fast_balance_v1.products(id);
ALTER TABLE fast_balance_v1.transactions ADD CONSTRAINT transactions_account_id_fkey FOREIGN KEY (account_id) REFERENCES fast_balance_v1.accounts(id);
ALTER TABLE fast_balance_v1.transactions ADD CONSTRAINT transactions_operation_id_fkey FOREIGN KEY (operation_id) REFERENCES fast_balance_v1.transaction_operations(id);

CREATE INDEX idx_transactions_card_id_date
    ON fast_balance_v1.transactions (card_id, transaction_date DESC)
    INCLUDE (amount, operation_id, product_id, description, nsu);

-- For lookups by account with recent-first scans and index-only potential
CREATE INDEX idx_transactions_account_id_date
    ON fast_balance_v1.transactions (account_id, transaction_date DESC)
    INCLUDE (amount, operation_id, product_id, description, nsu);


-- Create partitions for each year (2012 to 2025)
DO $$
BEGIN
   FOR i IN 2012..2025 LOOP
       EXECUTE format('
           CREATE TABLE fast_balance_v1.transactions_%s PARTITION OF fast_balance_v1.transactions
           FOR VALUES FROM (''%s-01-01'') TO (''%s-01-01'')',
           i, i, i+1
       );
   END LOOP;
END $$;

-- Create indexes on each partition for card_id and transaction_date
DO $$
BEGIN
   FOR i IN 2012..2025 LOOP
       EXECUTE format('
           CREATE INDEX idx_transactions_%s_card_id_date
           ON fast_balance_v1.transactions_%s (card_id, transaction_date DESC)
		   INCLUDE (amount, operation_id, product_id, description, nsu)', i, i);
	   EXECUTE format('
           CREATE INDEX idx_transactions_%s_account_id_date
           ON fast_balance_v1.transactions_%s (account_id, transaction_date DESC)
		   INCLUDE (amount, operation_id, product_id, description, nsu)', i, i);
   END LOOP;
END $$;

-- Helper to auto-create next year's partition (run annually)
DO $$
DECLARE
    next_year int := EXTRACT(YEAR FROM CURRENT_DATE)::int + 1;
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'fast_balance_v1'
          AND c.relname = format('transactions_%s', next_year)
    ) THEN
        EXECUTE format($fmt$
            CREATE TABLE fast_balance_v1.transactions_%1$s PARTITION OF fast_balance_v1.transactions
            FOR VALUES FROM ('%1$s-01-01') TO ('%2$s-01-01')
        $fmt$, next_year, next_year + 1);
    END IF;
END $$;


-- fast_balance_v1.balance_statement_summaries definition

CREATE TABLE fast_balance_v1.balance_statement_summaries (
	card_number varchar(100) NULL,
	customer_identification varchar(18) NULL,
	balances jsonb NOT NULL,
	statements jsonb NOT NULL,
	CONSTRAINT card_number_unique UNIQUE (card_number)
);

-- DROP FUNCTION fast_balance_v1.update_balance();

CREATE OR REPLACE FUNCTION fast_balance_v1.update_balance()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
DECLARE
   op_type CHAR(1);
BEGIN
   IF NEW.amount = 0 THEN 
		RETURN NEW;
   END IF;

   -- Get the transaction_type from transaction_operations
   SELECT transaction_type INTO op_type
   FROM fast_balance_v1.transaction_operations
   WHERE id = NEW.operation_id;
   -- Update account
   IF op_type = 'C' THEN
       UPDATE fast_balance_v1.accounts
       SET balance = balance + NEW.amount
       WHERE id = NEW.account_id;
   ELSIF op_type = 'D' THEN
       UPDATE fast_balance_v1.accounts
       SET balance = balance - NEW.amount
       WHERE id = NEW.account_id;
   END IF;
   RETURN NEW;
END;
$function$
;

-- Trigger to automatically update balance after each transaction
CREATE TRIGGER trigger_update_balance
AFTER INSERT ON fast_balance_v1.transactions
FOR EACH ROW
EXECUTE FUNCTION fast_balance_v1.update_balance();

-- Example data insertion (for testing)
INSERT INTO fast_balance_v1.credit_line_types (id, type_name) VALUES
	(1,	'TAE'),
	(2, 'TRE'),
	(3, 'TICKET PARCEIRO'),
	(4, 'TICKET CAR BASICO'),
	(5, 'TICKET RESTAURANTE CHILE'),
	(6, 'TKE'),
	(40, 'TICKET FLEX - MASTER'),
	(41, 'T-FLEX'),
	(42, 'REEMBOLSO TICKET FLEX - PAY'),
	(43, 'RESTAURANTE'),
	(60, 'ALIMENTACAO'),
	(61, 'HOME OFFICE'),
	(62, 'EDUCACAO'),
	(63, 'BEM ESTAR'),
	(66, 'MULTITRILHA'),
	(67, 'EDUCAÇÃO'),
	(68, 'ALIMENTAÇÃO'),
	(69, 'RESTAURANTE'),
	(70, 'BEM ESTAR'),
	(71, 'ALIMENTAÇÃO + RESTAURANTE'),
	(72, 'HOME OFFICE'),
	(81, 'RESTAURANTE'),
	(82, 'ALIMENTACAO'),
	(83, 'HOME OFFICE'),
	(84, 'MULTI-BENEFICIOS'),
	(85, 'EDUCACAO'),
	(91, 'TICKET PRESENTE DIGITAL');


INSERT INTO fast_balance_v1.products (id, description, abbreviation) VALUES
   (9, 'TICKET ALIMENTACAO', 'TAE'),
   (12, 'TICKET RESTAURANTE', 'TRE'),
   (34, 'TICKET CULTURA', 'TKE'),
   (36, 'TICKET CAR BASICO', NULL),
   (40, 'TICKET FLEX - MASTER', 'TFLEX'),
   (41, 'TICKET FLEX', 'TFE'),
   (42, 'TICKET PAY', 'TPY'),
   (43, 'TICKET SUPER FLEX', 'TSF'),
   (44, 'TICKET SUPER FLEX CL', 'TCL'),
   (55, 'TICKET PARCEIRO', 'TPA'),
   (56, 'TICKET RESTAURANTE CHILE', NULL),
   (60, 'TICKET CAR BASICO', 'TCB'),
   (80, 'TICKET ELO', 'TKP'),
   (91, 'TICKET PRESENTE DIGITAL', 'TPD');

INSERT INTO fast_balance_v1.tecnology_plastic_types (type_name) VALUES
	('CARTÃO SENHA'),
	('CARTÃO MAGNETICO'),
	('CARTÃO CHIP EMV SDA + TRILHA MAGNÉTICA'),
	('CARTÃO CHIP EMV DDA + TRILHA MAGNÉTICA'),
	('CARTÃO CHIP EMV SDA'),
	('CARTÃO CHIP EMV DDA');

INSERT INTO fast_balance_v1.transaction_operations (id, operation_name, transaction_type) values
	(1,	'CRÉDITO',	'C'),
	(16, 'AJUSTE A DÉBITO', 'D'),
	(17, 'AJUSTE A CRÉDITO', 'C'),
	(18, 'AJ. DÉBITO REL EXPURGO', 'D'),
	(19, 'AJ. CRÉDITO REL EXPURGO', 'C'),
	(20, 'DISPONIB. BENEFICIO', 'C'),
	(21, 'REVERSION CR PED PROCESADORA', 'D'),
	(25, 'REVERSÃO DE COMPRA', 'C'),
	(27, 'REV DISPON CRÉDITO', 'D'),
	(29, 'REVERSÃO DE CRÉDITO (MIGRAÇÃO)', 'D'),
	(40, 'COMPRAS', 'D'),
	(41, 'REVERSÃO DE COMPRAS', 'C'),
	(42, 'AJ. DÉBITO REL COMPRAS', 'D'),
	(43, 'AJ. CRÉDITO REL COMPRAS', 'C'),
	(44, 'AJ. DÉBITO DIVERSOS', 'D'),
	(45, 'AJ. CRÉDITO DIVERSOS', 'C'),
	(46, 'RESSARCIMENTOS C/DÉBITO EM CC', 'D'),
	(47, 'AJ. CRÉDITO REF SINISTRO', 'C'),
	(49, 'REVERSÃO DE COMPRAS (MIGRAÇÃO)', 'C'),
	(52, 'AJUSTE A DEB. REL. COMPRA', 'D'),
	(53, 'ESTORNO COMPRA DUPLICADA', 'C'),
	(60, 'AJ. DÉBITO REL DEVOLUCAO', 'D'),
	(61, 'AJ. CRÉDITO REL DEVOLUCAO', 'C'),
	(62, 'TRANSFERENCIA', 'D'),
	(63, 'TRANSFERENCIA', 'C'),
	(66, 'CREDITO TRANSF LIN CRED', 'C'),
	(67, 'DEBITO TRANSF LIN CRED', 'D'),
	(93, 'REVERSÃO DE COMPRAS (RISCO)',	'C'),
	(153, 'CUOTA ADMON FACTURADA', 'D'),
	(311, 'DISPONIB. DE CRÉDITO', 'C'),
	(313, 'DÉBITO REL. TRANSFERENCIA', 'D'),
	(314, 'CRÉDITO REL. TRANSFERENCIA', 'C'),
	(316, 'DÉBITO REL. EXPURGO', 'D'),
	(317, 'CRÉDITO REL. EXPURGO', 'C'),
	(318, 'REVERSÃO MANUAL DE CRÉDITO',	'D'),
	(319, 'REVERSÃO MANUAL DE DÉBITO', 'C'),
	(320, 'AJUSTE MANUAL DÉBITO', 'D'),
	(321, 'AJUSTE MANUAL CRÉDITO', 'C'),
	(323, 'REVERSÃO DISP. CRÉDITO',	'D'),
	(324, 'REVERSÃO DISP. BENEFICIO', 'D'),
	(325, 'DÉBITO DISTRIBUCAO NI', 'D'),
	(326, 'DÉBITO POR RECOGIMIENTO', 'D'),
	(327, 'CRÉDITO POR RECOGIMIENTO', 'C'),
	(331, 'CRÉDITO POR DISTRIBUIÇÃO', 'C'),
	(338, 'REVERSION CARGO POR EMISION', 'C'),
	(341, 'RECOLHIMENTO DE CREDITO', 'D'),
	(343, 'DÉBITO POR SALDO NEGATIVO', 'D'),
	(344, 'CRÉDITO POR SALDO NEGATIVO',	'C'),
	(345, 'RESGATE DE CREDITO (PP)', 'D'),
	(353, 'DB POR AJUSTE CARGA MASIVA',	'D'),
	(9999, 'COMPRA ORBITALL 9999', 'D');

	
	--TRUNCATE fast_balance_v1.transactions RESTART IDENTITY CASCADE;
	--TRUNCATE fast_balance_v1.cards RESTART IDENTITY CASCADE;
	--TRUNCATE fast_balance_v1.accounts RESTART IDENTITY CASCADE;
	--TRUNCATE fast_balance_v1.companies RESTART IDENTITY CASCADE;

	-- INSERT COMPANIES (optimized)
	DO $$
	BEGIN
	    -- Insert directly into the companies table, replicate data by multiplying smaller ranges
	    INSERT INTO fast_balance_v1.companies (name, identification)
	    SELECT
	        'Company ' || ((a.id - 1) * 1000 + b.id) AS name, -- Concatenated company name
	        trunc((random() * 899999999999999999) + 100000000000000000)::BIGINT AS identification -- Random 18-digit number
	    FROM 
	        generate_series(1, 50) AS a(id)  -- Outer range for first 50 IDs
	    CROSS JOIN 
	        generate_series(1, 1000) AS b(id); -- Inner range for 1,000 IDs, producing 50 x 1,000 = 50,000 rows
	END $$;
	
		
	-- INSERT ACCOUNTS (optimized)
	DO $$
	BEGIN
	    -- Step 1: Create randomized temporary tables with shuffled foreign keys
	    CREATE TEMP TABLE temp_randomized_companies AS
	    SELECT id, ROW_NUMBER() OVER (ORDER BY random()) AS row_num FROM fast_balance_v1.companies;
	    
	    CREATE TEMP TABLE temp_randomized_products AS
	    SELECT id, ROW_NUMBER() OVER (ORDER BY random()) AS row_num FROM fast_balance_v1.products;
	    
	    CREATE TEMP TABLE temp_randomized_credit_line_types AS
	    SELECT id, ROW_NUMBER() OVER (ORDER BY random()) AS row_num FROM fast_balance_v1.credit_line_types;
	
	    -- Step 2: Generate 200,000 unique records and assign randomized foreign keys
	    INSERT INTO fast_balance_v1.accounts (
	        company_id, product_id, credit_line_type_id,
	        customer_name, customer_identification,
	        contract_number, account_number, balance, status
	    )
	    SELECT
	        -- Assign foreign keys based on row mappings
	        c.id AS company_id,
	        p.id AS product_id,
	        cl.id AS credit_line_type_id,
	        
	        -- Randomly generate other fields directly
	        'Customer ' || g.series_id AS customer_name,
	        trunc(random() * 999999999999999)::BIGINT AS customer_identification,
	        'C' || LPAD((1000000 + g.series_id)::TEXT, 10, '0') AS contract_number,
	        10000 + (1000000 + g.series_id) AS account_number,
	        trunc(random() * 10000)::BIGINT AS balance,
	        CASE WHEN random() < 0.5 THEN 'INACTIVE' ELSE 'ACTIVE' END AS status
	
	    FROM
	        generate_series(1, 200000) AS g(series_id)
	        
	        -- Randomly join with pre-randomized temporary tables, matching row numbers
	        LEFT JOIN temp_randomized_companies c
	            ON c.row_num = g.series_id % (SELECT COUNT(*) FROM temp_randomized_companies) + 1
	        LEFT JOIN temp_randomized_products p
	            ON p.row_num = g.series_id % (SELECT COUNT(*) FROM temp_randomized_products) + 1
	        LEFT JOIN temp_randomized_credit_line_types cl
	            ON cl.row_num = g.series_id % (SELECT COUNT(*) FROM temp_randomized_credit_line_types) + 1;
	
	    -- Step 3: Drop temporary tables
	    DROP TABLE temp_randomized_companies, temp_randomized_products, temp_randomized_credit_line_types;
	END $$;
		
	
	-- INSERT CARDS (optimized)
	DO $$
	BEGIN
	    -- Step 1: Create temporary tables for random IDs
	    -- Randomized account IDs
	    CREATE TEMP TABLE temp_randomized_accounts AS
	    SELECT id, ROW_NUMBER() OVER (ORDER BY random()) AS row_num FROM fast_balance_v1.accounts;
	
	    -- Randomized technology plastic type IDs
	    CREATE TEMP TABLE temp_randomized_tech_types AS
	    SELECT id, ROW_NUMBER() OVER (ORDER BY random()) AS row_num FROM fast_balance_v1.tecnology_plastic_types;
	
	    -- Step 2: Generate unique card numbers and trim to the required number
	    CREATE TEMP TABLE temp_card_numbers AS
	    SELECT DISTINCT trunc((1000000000000000 + random() * 8999999999999999))::BIGINT AS card_number, 
	           ROW_NUMBER() OVER () AS row_num
	    FROM generate_series(1, 500000);
	    DELETE FROM temp_card_numbers
	    WHERE card_number IN (SELECT card_number FROM fast_balance_v1.cards);
	    DELETE FROM temp_card_numbers
	    WHERE card_number NOT IN (
	        SELECT card_number FROM temp_card_numbers LIMIT 250000
	    );
	
	    -- Step 3: Bulk insert cards with properly randomized data
	    INSERT INTO fast_balance_v1.cards (tecnology_plastic_type_id, account_id, card_number)
	    SELECT
	        tt.id AS tecnology_plastic_type_id,  -- Pre-randomized technology plastic type ID
	        ac.id AS account_id,                -- Pre-randomized account ID
	        cn.card_number                      -- Pre-generated unique card number
	    FROM temp_card_numbers cn
	    LEFT JOIN temp_randomized_accounts ac
	        ON ac.row_num = cn.row_num % (SELECT COUNT(*) FROM temp_randomized_accounts) + 1
	    LEFT JOIN temp_randomized_tech_types tt
	        ON tt.row_num = cn.row_num % (SELECT COUNT(*) FROM temp_randomized_tech_types) + 1;
	
	    -- Step 4: Cleanup temporary tables
	    DROP TABLE temp_randomized_accounts, temp_randomized_tech_types, temp_card_numbers;
	END $$;
	
		
	-- INSERT TRANSACTIONS (optimized)
	DO $$
	BEGIN
	    -- Disable triggers to improve bulk insert performance
	    ALTER TABLE fast_balance_v1.transactions DISABLE TRIGGER ALL;
	
	    -- Step 1: Create temporary tables with pre-randomized foreign keys
	    CREATE TEMP TABLE temp_randomized_accounts AS
	    SELECT id, ROW_NUMBER() OVER (ORDER BY random()) AS row_num FROM fast_balance_v1.accounts;
	
	    CREATE TEMP TABLE temp_randomized_cards AS
	    SELECT id, ROW_NUMBER() OVER (ORDER BY random()) AS row_num FROM fast_balance_v1.cards;
	
	    CREATE TEMP TABLE temp_randomized_operations AS
	    SELECT id, ROW_NUMBER() OVER (ORDER BY random()) AS row_num FROM fast_balance_v1.transaction_operations;
	
	    CREATE TEMP TABLE temp_randomized_products AS
	    SELECT id, ROW_NUMBER() OVER (ORDER BY random()) AS row_num FROM fast_balance_v1.products;
	
	    -- Step 2: Insert randomized transactions
	    INSERT INTO fast_balance_v1.transactions (
	        notification_id,
	        account_id,
	        card_id,
	        operation_id,
	        product_id,
	        transaction_date,
	        amount,
	        balance_after_transaction,
	        description,
	        nsu,
	        created_date
	    )
	    SELECT
	        gen_random_uuid(),                               -- Random UUID
	        acc.id AS account_id,                           -- Row-specific randomized account ID
	        crd.id AS card_id,                              -- Row-specific randomized card ID
	        opr.id AS operation_id,                        -- Row-specific randomized operation ID
	        prod.id AS product_id,                         -- Row-specific randomized product ID
	        NOW() - (random() * INTERVAL '365 days'),      -- Random transaction date within the past year
	        trunc(random() * 100000)::BIGINT + 1,          -- Random amount (1 to 100,000)
	        CASE WHEN random() < 0.9 THEN trunc(random() * 1000000)::BIGINT ELSE NULL END, -- Random balance_after_transaction
	        CASE WHEN random() < 0.7 THEN md5(random()::text) ELSE NULL END,             -- Random description
	        CASE WHEN random() < 0.5 THEN trunc(random() * 1000000)::BIGINT ELSE NULL END, -- Random NSU
	        NOW()                                         -- Current timestamp as created_date
	    FROM generate_series(1, 500000) AS g(series_id)   -- Generate 500,000 rows
	    LEFT JOIN temp_randomized_accounts acc
	        ON acc.row_num = g.series_id % (SELECT COUNT(*) FROM temp_randomized_accounts) + 1
	    LEFT JOIN temp_randomized_cards crd
	        ON crd.row_num = g.series_id % (SELECT COUNT(*) FROM temp_randomized_cards) + 1
	    LEFT JOIN temp_randomized_operations opr
	        ON opr.row_num = g.series_id % (SELECT COUNT(*) FROM temp_randomized_operations) + 1
	    LEFT JOIN temp_randomized_products prod
	        ON prod.row_num = g.series_id % (SELECT COUNT(*) FROM temp_randomized_products) + 1;
	
	    -- Step 3: Re-enable triggers
	    ALTER TABLE fast_balance_v1.transactions ENABLE TRIGGER ALL;
	
	    -- Step 4: Cleanup temporary tables
	    DROP TABLE temp_randomized_accounts, temp_randomized_cards, temp_randomized_operations, temp_randomized_products;
	END $$;
	
	-- APP OPERATIONS

	--'NotDefined': Color(Dy.colors.refNeutral5),
    --'Purchase': Color(Dy.colors.refNeutral5),
    --'Credit': Color(Dy.colors.refBrightMint50),
    --'CreditOfTransfer': Color(Dy.colors.refBrightMint50),
    --'Debit': Color(Dy.colors.refNeutral5),
    --'DebitOfTransfer': Color(Dy.colors.refNeutral5),
    --'Transfer': Color(Dy.colors.refNeutral5),
    --'Refund': Color(Dy.colors.refNeutral5),
    --'Recharge': Color(Dy.colors.refBrightMint50),
    --'RechargeScheduled': Color(Dy.colors.refBrightMint50),

	--APP STATUSES

    --Active
    --Canceled
    --Reissued
    --BlockedRH
    --BlockedRisk
    --Blocked
    --BlockedPassword
    --InProgress
    --NeedSetPassword
    --Inactive;
-- ============================================================
-- Inventory Management System - Database Initialization Script
-- ============================================================

-- Schemas
CREATE SCHEMA IF NOT EXISTS product_schema;
CREATE SCHEMA IF NOT EXISTS transaction_schema;

-- ============================================================
-- SCHEMA: product_schema
-- ============================================================

CREATE TABLE IF NOT EXISTS product_schema.products (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(150)    NOT NULL,
    description     TEXT,
    category        VARCHAR(100)    NOT NULL,
    image_url       VARCHAR(500),
    price           NUMERIC(12, 2)  NOT NULL CHECK (price >= 0),
    stock           INTEGER         NOT NULL DEFAULT 0 CHECK (stock >= 0),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_products_category ON product_schema.products(category);
CREATE INDEX IF NOT EXISTS idx_products_name     ON product_schema.products(name);

-- ============================================================
-- SCHEMA: transaction_schema
-- ============================================================

-- El tipo se almacena como VARCHAR para alinearse con EF Core (.HasConversion<string>())
-- Valores válidos: 'Purchase' | 'Sale'

CREATE TABLE IF NOT EXISTS transaction_schema.transactions (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    date            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    type            VARCHAR(20)     NOT NULL CHECK (type IN ('Purchase', 'Sale')),
    product_id      UUID            NOT NULL,
    quantity        INTEGER                                 NOT NULL CHECK (quantity > 0),
    unit_price      NUMERIC(12, 2)                          NOT NULL CHECK (unit_price >= 0),
    total_price     NUMERIC(12, 2)                          NOT NULL,
    detail          TEXT,
    created_at      TIMESTAMPTZ                             NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ                             NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_transactions_product_id ON transaction_schema.transactions(product_id);
CREATE INDEX IF NOT EXISTS idx_transactions_date       ON transaction_schema.transactions(date);
CREATE INDEX IF NOT EXISTS idx_transactions_type       ON transaction_schema.transactions(type);

-- ============================================================
-- Seed data (opcional para pruebas)
-- ============================================================

INSERT INTO product_schema.products (name, description, category, price, stock)
VALUES
    ('Laptop HP Pavilion', 'Laptop 15" Intel Core i5, 8GB RAM, 512GB SSD', 'Electrónica', 1299.99, 25),
    ('Mouse Logitech MX Master 3', 'Mouse inalámbrico ergonómico de alta precisión', 'Periféricos', 89.99, 50),
    ('Teclado Mecánico Keychron K8', 'Teclado TKL inalámbrico con switches Brown', 'Periféricos', 119.99, 30),
    ('Monitor LG 27" 4K', 'Monitor UHD IPS con DisplayHDR 400', 'Electrónica', 549.99, 15),
    ('Auriculares Sony WH-1000XM5', 'Auriculares over-ear con cancelación de ruido', 'Audio', 349.99, 20)
ON CONFLICT DO NOTHING;

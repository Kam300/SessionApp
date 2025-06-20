-- Таблицы для инвентаризации

-- Таблица для документов инвентаризации
CREATE TABLE inventory_documents (
    id SERIAL PRIMARY KEY,
    document_number TEXT NOT NULL,
    document_date DATE NOT NULL,
    warehouse_keeper TEXT NOT NULL,
    total_accounting_amount NUMERIC(12, 2) NOT NULL,
    total_actual_amount NUMERIC(12, 2) NOT NULL,
    difference_amount NUMERIC(12, 2) NOT NULL,
    difference_percent NUMERIC(5, 2) NOT NULL,
    is_approved BOOLEAN DEFAULT FALSE,
    approved_by TEXT,
    approved_date TIMESTAMP,
    is_processed BOOLEAN DEFAULT FALSE,
    created_by TEXT NOT NULL,
    created_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Таблица для позиций документа инвентаризации
CREATE TABLE inventory_document_items (
    id SERIAL PRIMARY KEY,
    document_id INT NOT NULL REFERENCES inventory_documents(id) ON DELETE CASCADE,
    material_article VARCHAR(50) NOT NULL,
    material_type VARCHAR(20) NOT NULL, -- 'fabric' или 'fitting'
    accounting_quantity NUMERIC(12, 3) NOT NULL,
    actual_quantity NUMERIC(12, 3) NOT NULL,
    difference_quantity NUMERIC(12, 3) NOT NULL,
    unit VARCHAR(20) NOT NULL,
    price NUMERIC(12, 2) NOT NULL,
    accounting_amount NUMERIC(12, 2) NOT NULL,
    actual_amount NUMERIC(12, 2) NOT NULL,
    difference_amount NUMERIC(12, 2) NOT NULL
);

-- Таблица для хранения истории движения материалов
CREATE TABLE material_movement_history (
    id SERIAL PRIMARY KEY,
    material_article VARCHAR(50) NOT NULL,
    material_type VARCHAR(20) NOT NULL, -- 'fabric' или 'fitting'
    document_type VARCHAR(50) NOT NULL, -- 'receipt', 'inventory', 'production'
    document_id INT NOT NULL,
    movement_date TIMESTAMP NOT NULL,
    quantity NUMERIC(12, 3) NOT NULL,
    unit VARCHAR(20) NOT NULL,
    price NUMERIC(12, 2) NOT NULL,
    amount NUMERIC(12, 2) NOT NULL,
    movement_type VARCHAR(20) NOT NULL -- 'in' или 'out'
);

-- Индексы для ускорения запросов
CREATE INDEX idx_inventory_document_items_document_id ON inventory_document_items(document_id);
CREATE INDEX idx_material_movement_history_material ON material_movement_history(material_article, material_type);
CREATE INDEX idx_material_movement_history_document ON material_movement_history(document_type, document_id);
CREATE INDEX idx_material_movement_history_date ON material_movement_history(movement_date);
CREATE TABLE nfce (
    id SERIAL PRIMARY KEY,
    chave_acesso VARCHAR(44) UNIQUE NOT NULL,
    numero_nota INTEGER NOT NULL,
    serie INTEGER NOT NULL,
    data_emissao TIMESTAMP NOT NULL,
    total DECIMAL(10,2) NOT NULL
);

CREATE TABLE emitente (
    id SERIAL PRIMARY KEY,
    cnpj VARCHAR(14) UNIQUE NOT NULL,
    nome VARCHAR(255) NOT NULL,
    endereco TEXT NOT NULL
);

CREATE TABLE produto (
    id SERIAL PRIMARY KEY,
    id_nfce INTEGER REFERENCES nfce(id) ON DELETE CASCADE,
    codigo VARCHAR(50) NOT NULL,
    descricao VARCHAR(255) NOT NULL,
    quantidade DECIMAL(10,2) NOT NULL,
    valor_unitario DECIMAL(10,2) NOT NULL,
    valor_total DECIMAL(10,2) NOT NULL
);

CREATE TABLE tributos (
    id SERIAL PRIMARY KEY,
    id_nfce INTEGER REFERENCES nfce(id) ON DELETE CASCADE,
    tipo VARCHAR(50) NOT NULL,
    valor DECIMAL(10,2) NOT NULL
);

CREATE TABLE pagamento (
    id SERIAL PRIMARY KEY,
    id_nfce INTEGER REFERENCES nfce(id) ON DELETE CASCADE,
    forma_pagamento VARCHAR(50) NOT NULL,
    valor_pago DECIMAL(10,2) NOT NULL
);

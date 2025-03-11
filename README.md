# 📥 Importação de NFC-e (Nota Fiscal de Consumidor Eletrônica)

Este projeto realiza a importação de arquivos XML de NFC-e (Nota Fiscal de Consumidor Eletrônica) para um banco de dados PostgreSQL 📊. A solução lê os arquivos XML de um diretório, processa e insere os dados relevantes nas tabelas do banco de dados, como informações sobre a NFC-e, emitente, produtos, tributos e pagamentos.

---

## 🚀 Tecnologias Utilizadas

- 🖥 **.NET Framework** (C#)
- 🗄 **PostgreSQL** (Banco de dados)
- 🔌 **Npgsql** (Biblioteca para comunicação com PostgreSQL)
- 📄 **XML** (Formato de dados para as NFC-e)
- 🔍 **LINQ to XML** (Para manipulação dos dados XML)

---

## ⚡ Funcionalidades

- 📂 Leitura de arquivos XML de NFC-e.
- 📊 Extração e inserção dos dados em um banco de dados PostgreSQL.
- 🛑 Verificação da existência de NFC-e e emitente antes da inserção para evitar duplicidades.
- ⚙ Processamento assíncrono de múltiplos arquivos XML em paralelo.
- 🔄 Controle de concorrência através de semáforos para limitar o número de tarefas simultâneas.

---

## 🗃 Estrutura do Banco de Dados

O banco de dados utilizado é o **PostgreSQL**, com as seguintes tabelas:

- **nfce** 🧾: Armazena informações sobre a NFC-e, como chave de acesso, número, série, data de emissão e total.
- **emitente** 🏢: Contém os dados do emitente da NFC-e, como CNPJ, nome e endereço.
- **produto** 📦: Armazena informações dos produtos presentes na NFC-e, como código, descrição, quantidade, valor unitário e total.
- **impostos_detalhados** 💰: Armazena os tributos relacionados à NFC-e, como PIS e COFINS.
- **pagamento** 💳: Registra os detalhes do pagamento da NFC-e, como forma de pagamento e valor pago.

---

## 📌 Pré-requisitos

- **.NET Framework** 4.7.2 ou superior.
- **PostgreSQL**.
- **Npgsql** instalado via NuGet.
- Arquivos XML de NFC-e válidos.

---

## 📥 Instalação

### 1️⃣ Clone o repositório

```bash
git clone https://github.com/username/importar-xml-nfce.git
cd importar-xml-nfce
```

### 2️⃣ Configure o banco de dados

Crie as tabelas no seu banco de dados PostgreSQL. Utilize o Postgres 13 de preferência. Você pode usar os scripts SQL a seguir para criar a estrutura do banco:

```sql
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

CREATE TABLE impostos_detalhados (
    id SERIAL PRIMARY KEY,
    id_produto INTEGER REFERENCES produto(id) ON DELETE CASCADE,
    tipo VARCHAR(50) NOT NULL,
    cst VARCHAR(10) NOT NULL,
    base_calculo DECIMAL(10,2),
    aliquota DECIMAL(10,4),
    valor DECIMAL(10,2) NOT NULL
);

CREATE TABLE pagamento (
    id SERIAL PRIMARY KEY,
    id_nfce INTEGER REFERENCES nfce(id) ON DELETE CASCADE,
    forma_pagamento VARCHAR(50) NOT NULL,
    valor_pago DECIMAL(10,2) NOT NULL
);
```

### 3️⃣ Configure o arquivo de conexão

Vá até o diretório `bin/debug/` e crie um arquivo chamado **appsettings.json**, contendo:

```json
{
  "Postgres": {
    "Host": "localhost",
    "Port": "porta_banco",
    "Database": "nome_banco",
    "Username": "usuario_postgres",
    "Password": "senha_postgres"
  }
}
```

Preencha com os dados do seu banco de dados PostgreSQL.

### 4️⃣ Adicione seus arquivos XML na pasta `XMLFiles`

No diretório `bin/debug/`, crie uma pasta chamada **XMLFiles** e coloque seus arquivos XML de NFC-e dentro dela. Somente arquivos `.xml` serão processados.

---

## 🎯 Uso

1️⃣ **Execute o programa**:

   Após configurar o banco de dados e os arquivos XML, execute:

   ```bash
   dotnet run
   ```

2️⃣ **Processamento de arquivos**:

   O sistema processará automaticamente todos os arquivos XML de NFC-e encontrados na pasta `XMLFiles`. Para cada arquivo, ele fará a inserção dos dados no banco de dados.

---

## 🤝 Contribuindo

1. Faça o fork do repositório.
2. Crie uma nova branch (`git checkout -b feature-nova-funcionalidade`).
3. Faça suas alterações e commit (`git commit -am 'Adiciona nova funcionalidade'`).
4. Faça o push para a branch (`git push origin feature-nova-funcionalidade`).
5. Abra um Pull Request.

---

## 📜 Licença

Este projeto está licenciado sob a [MIT License](LICENSE).
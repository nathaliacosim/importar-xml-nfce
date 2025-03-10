# Importa��o de NFC-e (Nota Fiscal de Consumidor Eletr�nica)

Este projeto � respons�vel por realizar a importa��o de arquivos XML de NFC-e (Nota Fiscal de Consumidor Eletr�nica) para um banco de dados PostgreSQL. A solu��o l� os arquivos XML de um diret�rio, processa e insere os dados relevantes nas tabelas do banco de dados, como informa��es sobre a NFC-e, emitente, produtos, tributos e pagamentos.

## Tecnologias Utilizadas

- **.NET Framework** (C#)
- **PostgreSQL** (Banco de dados)
- **Npgsql** (Biblioteca para comunica��o com PostgreSQL)
- **XML** (Formato de dados para as NFC-e)
- **LINQ to XML** (Para manipula��o dos dados XML)

## Funcionalidades

- Leitura de arquivos XML de NFC-e.
- Extra��o e inser��o dos dados em um banco de dados PostgreSQL.
- Verifica��o da exist�ncia de NFC-e e emitente antes da inser��o para evitar duplica��es.
- Processamento ass�ncrono de m�ltiplos arquivos XML em paralelo.
- Controle de concorr�ncia atrav�s de sem�foros para limitar o n�mero de tarefas simult�neas.

## Estrutura do Banco de Dados

O banco de dados utilizado � o PostgreSQL, com as seguintes tabelas:

- **nfce**: Armazena informa��es sobre a NFC-e, como chave de acesso, n�mero, s�rie, data de emiss�o e total.
- **emitente**: Cont�m os dados do emitente da NFC-e, como CNPJ, nome e endere�o.
- **produto**: Armazena informa��es dos produtos presentes na NFC-e, como c�digo, descri��o, quantidade, valor unit�rio e total.
- **tributos**: Armazena os tributos relacionados � NFC-e, como PIS e COFINS.
- **pagamento**: Registra os detalhes do pagamento da NFC-e, como forma de pagamento e valor pago.

## Pr�-requisitos

- **.NET Framework** 4.7.2 ou superior.
- **PostgreSQL**.
- **Npgsql** instalado via NuGet.
- Arquivos XML de NFC-e v�lidos.

## Instala��o

### 1. Clone o reposit�rio

```bash
git clone https://github.com/username/importar-xml-nfce.git
cd importar-xml-nfce
```

### 2. Configure o banco de dados

Crie as tabelas no seu banco de dados PostgreSQL. Voc� pode usar os scripts SQL a seguir para criar a estrutura do banco:

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
    id_nfce INTEGER REFERENCES nfce(id),
    codigo VARCHAR(50) NOT NULL,
    descricao VARCHAR(255) NOT NULL,
    quantidade DECIMAL(10,2) NOT NULL,
    valor_unitario DECIMAL(10,2) NOT NULL,
    valor_total DECIMAL(10,2) NOT NULL
);

CREATE TABLE impostos_detalhados (
    id SERIAL PRIMARY KEY,
    id_nfce INTEGER REFERENCES nfce(id) ON DELETE CASCADE,
    tipo VARCHAR(50) NOT NULL,
    cst VARCHAR(10) NOT NULL, 
    base_calculo DECIMAL(10,2),
    aliquota DECIMAL(10,4),
    valor DECIMAL(10,2) NOT NULL
);

CREATE TABLE pagamento (
    id SERIAL PRIMARY KEY,
    id_nfce INTEGER REFERENCES nfce(id),
    forma_pagamento VARCHAR(50) NOT NULL,
    valor_pago DECIMAL(10,2) NOT NULL
);
```

### 3. Configure o arquivo de conex�o

No arquivo de c�digo, voc� precisar� passar a string de conex�o do seu banco PostgreSQL. No exemplo abaixo:

```csharp
string connectionString = "Host=myserver;Port=5432;Username=mylogin;Password=mypass;Database=mydatabase";
var repository = new XmlRepository(connectionString);
```

Altere os par�metros de conex�o de acordo com a sua configura��o do PostgreSQL.

### 4. Coloque seus arquivos XML na pasta `XMLFiles`

Aplique os arquivos XML de NFC-e dentro da pasta `XMLFiles` no diret�rio do projeto (bin/debug/XMLFiles). O sistema ir� ler todos os arquivos `.xml` dentro dessa pasta e process�-los.

## Uso

1. **Execute o programa**:

   Ap�s ter configurado o banco de dados e os arquivos XML, voc� pode executar o programa diretamente.

   ```bash
   dotnet run
   ```

2. **Processamento de arquivos**:

   O sistema ir� automaticamente processar todos os arquivos XML de NFC-e encontrados na pasta `XMLFiles`. Para cada arquivo, ele far� a inser��o dos dados relevantes no banco de dados.

## Contribuindo

1. Fa�a o fork do reposit�rio.
2. Crie uma nova branch (`git checkout -b feature-nova-funcionalidade`).
3. Fa�a suas altera��es e commit (`git commit -am 'Adiciona nova funcionalidade'`).
4. Fa�a o push para a branch (`git push origin feature-nova-funcionalidade`).
5. Abra um Pull Request.

## Licen�a

Este projeto est� licenciado sob a [MIT License](LICENSE).
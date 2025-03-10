using System;
using System.Xml.Serialization;
using Npgsql;
using System.IO;

namespace ImportarXML.Data;

public class XmlRepository
{
    private readonly string _connectionString;

    public XmlRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void ProcessarXmls()
    {
        string xmlDirectory = Path.Combine(Directory.GetCurrentDirectory(), "XMLFiles");
        if (!Directory.Exists(xmlDirectory))
        {
            Console.WriteLine("A pasta XMLFiles não foi encontrada!");
            return;
        }

        string[] xmlFiles = Directory.GetFiles(xmlDirectory, "*.xml");
        Console.WriteLine($"Foram encontrados {xmlFiles.Length} arquivos XML.");

        foreach (var xmlFile in xmlFiles)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            Console.WriteLine($"Processando o arquivo: {xmlFile}");
                            ImportarDadosNfce(xmlFile, connection);

                            transaction.Commit();
                            Console.WriteLine($"✅ Processamento do arquivo {xmlFile} concluído com sucesso!");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"❌ Ocorreu um erro ao processar o arquivo {xmlFile}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao abrir a conexão ou processar o arquivo {xmlFile}: {ex.Message}");
            }
        }

        Console.WriteLine("Processamento de todos os arquivos concluído.");
    }

    public void ImportarDadosNfce(string xmlFilePath, NpgsqlConnection connection)
    {
        try
        {
            // Carregar e desserializar o XML
            var serializer = new XmlSerializer(typeof(NfeProc));
            NfeProc nfeProc;

            using (var fileStream = new FileStream(xmlFilePath, FileMode.Open))
            {
                nfeProc = (NfeProc)serializer.Deserialize(fileStream);
            }

            // Extrair dados básicos da NFC-e
            var chaveAcesso = nfeProc.NFe.InfNFe.Id.Substring(3);
            var numeroNfce = nfeProc.NFe.InfNFe.Ide.NNF;
            var serieNfce = nfeProc.NFe.InfNFe.Ide.Serie;
            var dataEmissao = nfeProc.NFe.InfNFe.Ide.DhEmi;
            var total = nfeProc.NFe.InfNFe.Total.ICMSTot.VProd;

            // Verificar se a NFC-e já foi inserida
            var nfceExistente = VerificarNfceExistente(connection, chaveAcesso);
            if (nfceExistente)
            {
                Console.WriteLine("⚠️ A NFC-e já foi importada. Pulando a inserção...");
                return;
            }

            // Inserir na tabela 'nfce'
            var nfceId = InserirNfce(connection, chaveAcesso, numeroNfce, serieNfce, dataEmissao, total);
            Console.WriteLine("📥 NFC-e inserida com sucesso! ID: " + nfceId);

            // Inserir dados do emitente
            var cnpj = nfeProc.NFe.InfNFe.Emit.CNPJ;
            var nomeEmitente = nfeProc.NFe.InfNFe.Emit.Nome;
            var enderecoEmitente = $"{nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.Logradouro}, {nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.Numero}, {nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.Bairro}, {nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.Municipio}-{nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.UF}";
            InserirEmitente(connection, cnpj, nomeEmitente, enderecoEmitente);
            Console.WriteLine("🏢 Emitente inserido com sucesso!");

            // Inserir os produtos
            foreach (var det in nfeProc.NFe.InfNFe.Det)
            {
                var codigoProduto = det.Prod.Codigo;
                var descricaoProduto = det.Prod.Descricao;
                var quantidade = det.Prod.Quantidade;
                var valorUnitario = det.Prod.ValorUnitario;
                var valorTotal = det.Prod.ValorTotal;
                InserirProduto(connection, nfceId, codigoProduto, descricaoProduto, quantidade, valorUnitario, valorTotal);
            }
            Console.WriteLine("🛒 Produtos inseridos com sucesso!");

            // Inserir os impostos detalhados
            foreach (var det in nfeProc.NFe.InfNFe.Det)
            {
                if (det.Imposto != null)
                {
                    // Inserir PIS detalhado
                    if (det.Imposto.PIS != null && det.Imposto.PIS.PISAliq != null)
                    {
                        InserirImpostoDetalhado(
                            connection,
                            nfceId,
                            "PIS",
                            det.Imposto.PIS.PISAliq.CST.ToString(),
                            det.Imposto.PIS.PISAliq.BaseCalculo,
                            det.Imposto.PIS.PISAliq.Aliquota,
                            det.Imposto.PIS.PISAliq.Valor);
                    }
                    // Inserir COFINS detalhado
                    if (det.Imposto.COFINS != null && det.Imposto.COFINS.COFINSAliq != null)
                    {
                        InserirImpostoDetalhado(
                            connection,
                            nfceId,
                            "COFINS",
                            det.Imposto.COFINS.COFINSAliq.CST.ToString(),
                            det.Imposto.COFINS.COFINSAliq.BaseCalculo,
                            det.Imposto.COFINS.COFINSAliq.Aliquota,
                            det.Imposto.COFINS.COFINSAliq.Valor);
                    }
                    // Inserir ICMS detalhado (ICMS60)
                    if (det.Imposto.ICMS != null && det.Imposto.ICMS.ICMS60 != null)
                    {
                        InserirImpostoDetalhado(
                            connection,
                            nfceId,
                            "ICMS",
                            det.Imposto.ICMS.ICMS60.CST.ToString(),
                            null, // Base de cálculo não informada no ICMS60
                            null, // Alíquota não informada no ICMS60
                            0);   // Valor não informado, definido como 0
                    }
                }
            }
            Console.WriteLine("📊 Impostos detalhados inseridos com sucesso!");

            // Inserir pagamento
            foreach (var pag in nfeProc.NFe.InfNFe.Pag.DetPag)
            {
                var formaPagamento = pag.FormaPagamento;
                var valorPago = pag.ValorPago;
                InserirPagamento(connection, nfceId, formaPagamento, valorPago);
            }
            Console.WriteLine("💳 Pagamento inserido com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ocorreu um erro ao processar os dados do arquivo {xmlFilePath}: {ex.Message}");
        }
    }

    // Métodos auxiliares para verificar e inserir dados no banco de dados

    private bool VerificarNfceExistente(NpgsqlConnection connection, string chaveAcesso)
    {
        var query = "SELECT COUNT(1) FROM nfce WHERE chave_acesso = @ChaveAcesso";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("ChaveAcesso", chaveAcesso);
            return (long)cmd.ExecuteScalar() > 0;
        }
    }

    private int InserirNfce(NpgsqlConnection connection, string chaveAcesso, int? numero, int? serie, DateTime? dataEmissao, decimal? total)
    {
        var query = "INSERT INTO nfce (chave_acesso, numero_nota, serie, data_emissao, total) VALUES (@ChaveAcesso, @Numero, @Serie, @DataEmissao, @Total) RETURNING id;";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("ChaveAcesso", chaveAcesso);
            cmd.Parameters.AddWithValue("Numero", numero ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("Serie", serie ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("DataEmissao", dataEmissao ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("Total", total ?? (object)DBNull.Value);
            return (int)cmd.ExecuteScalar();
        }
    }

    private void InserirEmitente(NpgsqlConnection connection, string cnpj, string nome, string endereco)
    {
        // Verificar se o emitente já existe com o CNPJ informado
        var queryVerificacao = "SELECT COUNT(1) FROM emitente WHERE cnpj = @CNPJ";
        using (var cmd = new NpgsqlCommand(queryVerificacao, connection))
        {
            cmd.Parameters.AddWithValue("CNPJ", cnpj);
            var count = (long)cmd.ExecuteScalar();
            if (count > 0)
            {
                Console.WriteLine("⚠️ Emitente já existe. Pulando inserção...");
                return;  // Se o emitente já existir, não insere
            }
        }

        // Se não existir, inserir o novo emitente
        var queryInsercao = "INSERT INTO emitente (cnpj, nome, endereco) VALUES (@CNPJ, @Nome, @Endereco)";
        using (var cmd = new NpgsqlCommand(queryInsercao, connection))
        {
            cmd.Parameters.AddWithValue("CNPJ", cnpj);
            cmd.Parameters.AddWithValue("Nome", nome);
            cmd.Parameters.AddWithValue("Endereco", endereco);
            cmd.ExecuteNonQuery();
        }
    }

    private void InserirProduto(NpgsqlConnection connection, int nfceId, string codigo, string descricao, decimal? quantidade, decimal? valorUnitario, decimal? valorTotal)
    {
        var query = "INSERT INTO produto (id_nfce, codigo, descricao, quantidade, valor_unitario, valor_total) VALUES (@IdNfce, @Codigo, @Descricao, @Quantidade, @ValorUnitario, @ValorTotal)";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("IdNfce", nfceId);
            cmd.Parameters.AddWithValue("Codigo", codigo);
            cmd.Parameters.AddWithValue("Descricao", descricao);
            cmd.Parameters.AddWithValue("Quantidade", quantidade ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ValorUnitario", valorUnitario ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ValorTotal", valorTotal ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    private void InserirImpostoDetalhado(NpgsqlConnection connection, int nfceId, string tipo, string cst, decimal? baseCalculo, decimal? aliquota, decimal valor)
    {
        var query = "INSERT INTO impostos_detalhados (id_nfce, tipo, cst, base_calculo, aliquota, valor) VALUES (@IdNfce, @Tipo, @CST, @BaseCalculo, @Aliquota, @Valor)";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("IdNfce", nfceId);
            cmd.Parameters.AddWithValue("Tipo", tipo);
            cmd.Parameters.AddWithValue("CST", cst);
            cmd.Parameters.AddWithValue("BaseCalculo", baseCalculo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("Aliquota", aliquota ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("Valor", valor);
            cmd.ExecuteNonQuery();
        }
    }

    private void InserirPagamento(NpgsqlConnection connection, int nfceId, string formaPagamento, decimal? valorPago)
    {
        var query = "INSERT INTO pagamento (id_nfce, forma_pagamento, valor_pago) VALUES (@IdNfce, @FormaPagamento, @ValorPago)";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("IdNfce", nfceId);
            cmd.Parameters.AddWithValue("FormaPagamento", formaPagamento);
            cmd.Parameters.AddWithValue("ValorPago", valorPago ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }
}

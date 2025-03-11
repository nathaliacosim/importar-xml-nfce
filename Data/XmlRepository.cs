using Npgsql;
using System;
using System.IO;
using System.Xml.Serialization;

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
            Console.WriteLine("❌ A pasta XMLFiles não foi encontrada!");
            return;
        }

        string[] xmlFiles = Directory.GetFiles(xmlDirectory, "*.xml");
        Console.WriteLine($"📄 Foram encontrados {xmlFiles.Length} arquivos XML.");

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
                            Console.WriteLine($"🔄 Processando o arquivo: {xmlFile}");
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

        Console.WriteLine("🎉 Processamento de todos os arquivos concluído.");
    }

    public void ImportarDadosNfce(string xmlFilePath, NpgsqlConnection connection)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(NfeProc));
            NfeProc nfeProc;

            using (var fileStream = new FileStream(xmlFilePath, FileMode.Open))
            {
                nfeProc = (NfeProc)serializer.Deserialize(fileStream);
            }

            var chaveAcesso = nfeProc.NFe.InfNFe.Id.Substring(3);
            var numeroNfce = nfeProc.NFe.InfNFe.Ide.NNF;
            var serieNfce = nfeProc.NFe.InfNFe.Ide.Serie;
            var dataEmissao = nfeProc.NFe.InfNFe.Ide.DhEmi;
            var total = nfeProc.NFe.InfNFe.Total.ICMSTot.VProd;

            var nfceExistente = VerificarNfceExistente(connection, chaveAcesso);
            if (nfceExistente) return;

            var nfceId = InserirNfce(connection, chaveAcesso, numeroNfce, serieNfce, dataEmissao, total);
            Console.WriteLine("📥 NFC-e inserida com sucesso! ID: " + nfceId);

            var cnpj = nfeProc.NFe.InfNFe.Emit.CNPJ;
            var nomeEmitente = nfeProc.NFe.InfNFe.Emit.Nome;
            var enderecoEmitente = $"{nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.Logradouro}, {nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.Numero}, {nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.Bairro}, {nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.Municipio}-{nfeProc.NFe.InfNFe.Emit.EnderecoEmitente.UF}";
            InserirEmitente(connection, cnpj, nomeEmitente, enderecoEmitente);

            foreach (var det in nfeProc.NFe.InfNFe.Det)
            {
                var produtoId = InserirProduto(connection, nfceId, det.Prod.Codigo, det.Prod.Descricao, det.Prod.Quantidade, det.Prod.ValorUnitario, det.Prod.ValorTotal);

                if (det.Imposto != null)
                {
                    if (det.Imposto.PIS?.PISAliq != null)
                    {
                        InserirImpostoDetalhado(connection, produtoId, "PIS", det.Imposto.PIS.PISAliq.CST.ToString(), det.Imposto.PIS.PISAliq.BaseCalculo, det.Imposto.PIS.PISAliq.Aliquota, det.Imposto.PIS.PISAliq.Valor);
                    }
                    if (det.Imposto.COFINS?.COFINSAliq != null)
                    {
                        InserirImpostoDetalhado(connection, produtoId, "COFINS", det.Imposto.COFINS.COFINSAliq.CST.ToString(), det.Imposto.COFINS.COFINSAliq.BaseCalculo, det.Imposto.COFINS.COFINSAliq.Aliquota, det.Imposto.COFINS.COFINSAliq.Valor);
                    }
                    if (det.Imposto.ICMS?.ICMS60 != null)
                    {
                        InserirImpostoDetalhado(connection, produtoId, "ICMS", det.Imposto.ICMS.ICMS60.CST.ToString(), null, null, 0);
                    }
                }
            }
            Console.WriteLine("🛒 Produtos e impostos detalhados inseridos com sucesso!");

            foreach (var pag in nfeProc.NFe.InfNFe.Pag.DetPag)
            {
                InserirPagamento(connection, nfceId, pag.FormaPagamento, pag.ValorPago);
            }
            Console.WriteLine("💳 Pagamento inserido com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ocorreu um erro ao processar os dados do arquivo {xmlFilePath}: {ex.Message}");
        }
    }

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
        var queryVerificacao = "SELECT COUNT(1) FROM emitente WHERE cnpj = @CNPJ";
        using (var cmd = new NpgsqlCommand(queryVerificacao, connection))
        {
            cmd.Parameters.AddWithValue("CNPJ", cnpj);
            var count = (long)cmd.ExecuteScalar();
            if (count > 0) return;
        }

        var queryInsercao = "INSERT INTO emitente (cnpj, nome, endereco) VALUES (@CNPJ, @Nome, @Endereco)";
        using (var cmd = new NpgsqlCommand(queryInsercao, connection))
        {
            cmd.Parameters.AddWithValue("CNPJ", cnpj);
            cmd.Parameters.AddWithValue("Nome", nome);
            cmd.Parameters.AddWithValue("Endereco", endereco);
            cmd.ExecuteNonQuery();
        }
    }

    private int InserirProduto(NpgsqlConnection connection, int nfceId, string codigo, string descricao, decimal? quantidade, decimal? valorUnitario, decimal? valorTotal)
    {
        var query = "INSERT INTO produto (id_nfce, codigo, descricao, quantidade, valor_unitario, valor_total) VALUES (@IdNfce, @Codigo, @Descricao, @Quantidade, @ValorUnitario, @ValorTotal) RETURNING id;";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("IdNfce", nfceId);
            cmd.Parameters.AddWithValue("Codigo", codigo);
            cmd.Parameters.AddWithValue("Descricao", descricao);
            cmd.Parameters.AddWithValue("Quantidade", quantidade ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ValorUnitario", valorUnitario ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ValorTotal", valorTotal ?? (object)DBNull.Value);

            return (int)cmd.ExecuteScalar();
        }
    }

    private void InserirImpostoDetalhado(NpgsqlConnection connection, int produtoId, string tipo, string cst, decimal? baseCalculo, decimal? aliquota, decimal valor)
    {
        var query = "INSERT INTO impostos_detalhados (id_produto, tipo, cst, base_calculo, aliquota, valor) VALUES (@IdProduto, @Tipo, @CST, @BaseCalculo, @Aliquota, @Valor)";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("IdProduto", produtoId);
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

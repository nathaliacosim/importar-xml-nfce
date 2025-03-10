namespace ImportarXML.Data
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("nfeProc", Namespace = "http://www.portalfiscal.inf.br/nfe")]
    public class NfeProc
    {
        [XmlElement("NFe")]
        public NFe NFe { get; set; }
    }

    public class NFe
    {
        [XmlElement("infNFe")]
        public InfNFe InfNFe { get; set; }
    }

    public class InfNFe
    {
        [XmlAttribute("Id")]
        public string Id { get; set; }

        [XmlElement("ide")]
        public Ide Ide { get; set; }

        [XmlElement("emit")]
        public Emit Emit { get; set; }

        [XmlElement("det")]
        public List<Det> Det { get; set; }

        [XmlElement("total")]
        public Total Total { get; set; }

        [XmlElement("pag")]
        public Pag Pag { get; set; }
    }

    public class Ide
    {
        [XmlElement("cUF")]
        public int CUF { get; set; }

        [XmlElement("cNF")]
        public int CNF { get; set; }

        [XmlElement("natOp")]
        public string NatOp { get; set; }

        [XmlElement("mod")]
        public int Mod { get; set; }

        [XmlElement("serie")]
        public int Serie { get; set; }

        [XmlElement("nNF")]
        public int NNF { get; set; }

        [XmlElement("dhEmi")]
        public DateTime DhEmi { get; set; }

        [XmlElement("vNF")]
        public decimal VNF { get; set; }
    }

    public class Emit
    {
        [XmlElement("CNPJ")]
        public string CNPJ { get; set; }

        [XmlElement("xNome")]
        public string Nome { get; set; }

        [XmlElement("enderEmit")]
        public EnderEmit EnderecoEmitente { get; set; }
    }

    public class EnderEmit
    {
        [XmlElement("xLgr")]
        public string Logradouro { get; set; }

        [XmlElement("nro")]
        public string Numero { get; set; }

        [XmlElement("xBairro")]
        public string Bairro { get; set; }

        [XmlElement("xMun")]
        public string Municipio { get; set; }

        [XmlElement("UF")]
        public string UF { get; set; }

        [XmlElement("CEP")]
        public string CEP { get; set; }
    }

    public class Det
    {
        [XmlElement("prod")]
        public Prod Prod { get; set; }

        [XmlElement("imposto")]
        public Imposto Imposto { get; set; }
    }

    public class Prod
    {
        [XmlElement("cProd")]
        public string Codigo { get; set; }

        [XmlElement("xProd")]
        public string Descricao { get; set; }

        [XmlElement("qCom")]
        public decimal Quantidade { get; set; }

        [XmlElement("vUnCom")]
        public decimal ValorUnitario { get; set; }

        [XmlElement("vProd")]
        public decimal ValorTotal { get; set; }
    }

    public class Imposto
    {
        [XmlElement("vTotTrib")]
        public decimal ValorTotalTributos { get; set; }

        [XmlElement("ICMS")]
        public ICMS ICMS { get; set; }

        [XmlElement("PIS")]
        public PIS PIS { get; set; }

        [XmlElement("COFINS")]
        public COFINS COFINS { get; set; }
    }

    public class PIS
    {
        [XmlElement("PISAliq")]
        public PISAliq PISAliq { get; set; }
    }

    public class PISAliq
    {
        [XmlElement("CST")]
        public int CST { get; set; }

        [XmlElement("vBC")]
        public decimal BaseCalculo { get; set; }

        [XmlElement("pPIS")]
        public decimal Aliquota { get; set; }

        [XmlElement("vPIS")]
        public decimal Valor { get; set; }
    }

    public class COFINS
    {
        [XmlElement("COFINSAliq")]
        public COFINSAliq COFINSAliq { get; set; }
    }

    public class COFINSAliq
    {
        [XmlElement("CST")]
        public int CST { get; set; }

        [XmlElement("vBC")]
        public decimal BaseCalculo { get; set; }

        [XmlElement("pCOFINS")]
        public decimal Aliquota { get; set; }

        [XmlElement("vCOFINS")]
        public decimal Valor { get; set; }
    }

    public class ICMS
    {
        [XmlElement("ICMS60")]
        public ICMS60 ICMS60 { get; set; }
    }

    public class ICMS60
    {
        [XmlElement("orig")]
        public int Orig { get; set; }

        [XmlElement("CST")]
        public int CST { get; set; }
    }

    public class Total
    {
        [XmlElement("ICMSTot")]
        public ICMSTot ICMSTot { get; set; }
    }

    public class ICMSTot
    {
        [XmlElement("vProd")]
        public decimal VProd { get; set; }
    }

    public class Pag
    {
        [XmlElement("detPag")]
        public List<DetPag> DetPag { get; set; }
    }

    public class DetPag
    {
        [XmlElement("tPag")]
        public string FormaPagamento { get; set; }

        [XmlElement("vPag")]
        public decimal ValorPago { get; set; }
    }
}
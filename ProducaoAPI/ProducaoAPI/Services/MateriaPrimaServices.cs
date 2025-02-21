﻿using ProducaoAPI.Exceptions;
using ProducaoAPI.Models;
using ProducaoAPI.Repositories.Interfaces;
using ProducaoAPI.Requests;
using ProducaoAPI.Responses;
using ProducaoAPI.Services.Interfaces;
using System.Xml;

namespace ProducaoAPI.Services
{
    public class MateriaPrimaServices : IMateriaPrimaService
    {
        private readonly IMateriaPrimaRepository _materiaPrimaRepository;

        public MateriaPrimaServices(IMateriaPrimaRepository materiaPrimaRepository)
        {
            _materiaPrimaRepository = materiaPrimaRepository;
        }

        public MateriaPrimaResponse EntityToResponse(MateriaPrima materiaPrima)
        {
            return new MateriaPrimaResponse(materiaPrima.Id, materiaPrima.Nome, materiaPrima.Fornecedor, materiaPrima.Unidade, materiaPrima.Preco, materiaPrima.Ativo);
        }

        public ICollection<MateriaPrimaResponse> EntityListToResponseList(IEnumerable<MateriaPrima> materiaPrima)
        {
            return materiaPrima.Select(m => EntityToResponse(m)).ToList();
        }

        public async Task<MateriaPrima> CriarMateriaPrimaPorXML(IFormFile arquivoXML)
        {
            var documentoXML = SalvarXML(arquivoXML);

            XmlNamespaceManager nsManager = new XmlNamespaceManager(documentoXML.NameTable);
            nsManager.AddNamespace("ns", "http://www.portalfiscal.inf.br/nfe");

            XmlNode? fornecedorNode = documentoXML.SelectSingleNode("//ns:nfeProc/ns:NFe/ns:infNFe/ns:emit/ns:xNome", nsManager);
            if (fornecedorNode == null) throw new Exception("Erro ao ler arquivo XML: Fornecedor não encontrado.");
            string fornecedor = fornecedorNode.InnerText;

            XmlNode? produtoNode = documentoXML.SelectSingleNode("//ns:nfeProc/ns:NFe/ns:infNFe/ns:det/ns:prod/ns:xProd", nsManager);
            if (produtoNode == null) throw new Exception("Erro ao ler arquivo XML: Produto não encontrado.");
            string produto = produtoNode.InnerText;

            XmlNode? unidadeNode = documentoXML.SelectSingleNode("//ns:nfeProc/ns:NFe/ns:infNFe/ns:det/ns:prod/ns:uCom", nsManager);
            if (unidadeNode == null) throw new Exception("Erro ao ler arquivo XML: Unidade não encontrada.");
            string unidade = unidadeNode.InnerText == "T" ? "KG" : unidadeNode.InnerText;

            XmlNode? precoNode = documentoXML.SelectSingleNode("//ns:nfeProc/ns:NFe/ns:infNFe/ns:det/ns:prod/ns:vUnCom", nsManager);
            if (precoNode == null) throw new Exception("Erro ao ler arquivo XML: Preço não encontrado.");
            double preco = Convert.ToDouble(precoNode.InnerText.Replace(".", ","));
            if (unidade == "KG") preco /= 1000;

            MateriaPrimaRequest request = new MateriaPrimaRequest(produto, fornecedor, unidade, preco);
            await ValidarDadosParaCadastrar(request);
            MateriaPrima materiaPrima = new MateriaPrima(request.Nome, request.Fornecedor, request.Unidade, request.Preco);
            await _materiaPrimaRepository.AdicionarAsync(materiaPrima);

            var filePatch = Path.Combine("Storage", arquivoXML.FileName);

            return materiaPrima;
        }

        public XmlDocument SalvarXML(IFormFile arquivoXML)
        {
            var filePatch = Path.Combine("Storage", arquivoXML.FileName);
            using Stream fileStream = new FileStream(filePatch, FileMode.Create);
            arquivoXML.CopyTo(fileStream);
            fileStream.Close();
            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(filePatch));
            return doc;
        }

        public Task<IEnumerable<MateriaPrima>> ListarMateriasPrimasAtivas() => _materiaPrimaRepository.ListarMateriasPrimasAtivas();

        public Task<IEnumerable<MateriaPrima>> ListarTodasMateriasPrimas() => _materiaPrimaRepository.ListarTodasMateriasPrimas();

        public Task<MateriaPrima> BuscarMateriaPorIdAsync(int id) => _materiaPrimaRepository.BuscarMateriaPorIdAsync(id);

        public async Task<MateriaPrima> AdicionarAsync(MateriaPrimaRequest request)
        {
            await ValidarDadosParaCadastrar(request);
            var materiaPrima = new MateriaPrima(request.Nome, request.Fornecedor, request.Unidade, request.Preco);
            await _materiaPrimaRepository.AdicionarAsync(materiaPrima);
            return materiaPrima;
        }

        public async Task<MateriaPrima> AtualizarAsync(int id, MateriaPrimaRequest request)
        {
            try
            {
                await ValidarDadosParaAtualizar(request, id);
                var materiaPrima = await _materiaPrimaRepository.BuscarMateriaPorIdAsync(id);

                materiaPrima.Nome = request.Nome;
                materiaPrima.Fornecedor = request.Fornecedor;
                materiaPrima.Unidade = request.Unidade;
                materiaPrima.Preco = request.Preco;

                await _materiaPrimaRepository.AtualizarAsync(materiaPrima);
                return materiaPrima;
            }
            catch (BadRequestException)
            {
                throw;
            }
        }

        public async Task<MateriaPrima> InativarMateriaPrima(int id)
        {
            var materiaPrima = await BuscarMateriaPorIdAsync(id);
            materiaPrima.Ativo = false;
            await _materiaPrimaRepository.AtualizarAsync(materiaPrima);
            return materiaPrima;
        }

        public async Task ValidarDadosParaCadastrar(MateriaPrimaRequest request)
        {
            try
            {
                var materiasPrimas = await _materiaPrimaRepository.ListarTodasMateriasPrimas();
                var nomeMateriasPrimas = new List<string>();
                foreach (var materia in materiasPrimas)
                {
                    nomeMateriasPrimas.Add(materia.Nome);
                }

                if (nomeMateriasPrimas.Contains(request.Nome)) throw new BadRequestException("Já existe uma matéria-prima com este nome!");

                if (string.IsNullOrWhiteSpace(request.Nome)) throw new BadRequestException("O campo \"Nome\" não pode estar vazio.");
                if (string.IsNullOrWhiteSpace(request.Fornecedor)) throw new BadRequestException("O campo \"Fornecedor\" não pode estar vazio.");
                if (string.IsNullOrWhiteSpace(request.Unidade)) throw new BadRequestException("O campo \"Unidade\" não pode estar vazio.");
                if (request.Unidade.Length > 5) throw new BadRequestException("A sigla da unidade não pode ter mais de 5 caracteres.");
                if (request.Preco <= 0) throw new BadRequestException("O preço não pode ser igual ou menor que 0.");
            }
            catch (BadRequestException)
            {
                throw;
            }
        }

        public async Task ValidarDadosParaAtualizar(MateriaPrimaRequest request, int id)
        {
            try
            {
                var materiaAtualizada = await _materiaPrimaRepository.BuscarMateriaPorIdAsync(id);

                var materiasPrimas = await _materiaPrimaRepository.ListarTodasMateriasPrimas();
                var nomeMateriasPrimas = new List<string>();
                foreach (var materia in materiasPrimas)
                {
                    nomeMateriasPrimas.Add(materia.Nome);
                }

                if (nomeMateriasPrimas.Contains(request.Nome) && materiaAtualizada.Nome != request.Nome) throw new BadRequestException("Já existe uma matéria-prima com este nome!");

                if (string.IsNullOrWhiteSpace(request.Nome)) throw new BadRequestException("O campo \"Nome\" não pode estar vazio.");
                if (string.IsNullOrWhiteSpace(request.Fornecedor)) throw new BadRequestException("O campo \"Fornecedor\" não pode estar vazio.");
                if (string.IsNullOrWhiteSpace(request.Unidade)) throw new ArgumentException("O campo \"Unidade\" não pode estar vazio.");
                if (request.Unidade.Length > 5) throw new BadRequestException("A sigla da unidade não pode ter mais de 5 caracteres.");
                if (request.Preco <= 0) throw new BadRequestException("O preço não pode ser igual ou menor que 0.");
            }
            catch (BadRequestException)
            {
                throw;
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using ProducaoAPI.Models;
using ProducaoAPI.Requests;
using ProducaoAPI.Responses;
using ProducaoAPI.Services.Interfaces;

namespace ProducaoAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    //[Authorize]
    public class ProcessoProducaoController : Controller
    {
        private readonly IProcessoProducaoService _processoProducaoService;
        private readonly IProducaoMateriaPrimaService _producaoMateriaPrimaService;
        public ProcessoProducaoController(IProcessoProducaoService processoProducaoService, IProducaoMateriaPrimaService producaoMateriaPrimaService)
        {
            _processoProducaoService = processoProducaoService;
            _producaoMateriaPrimaService = producaoMateriaPrimaService;
        }

        /// <summary>
        /// Obter produções
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProcessoProducaoResponse>>> ListarProducoes()
        {
            var producoes = await _processoProducaoService.ListarProducoesAsync();
            if (producoes == null) return NotFound();
            return Ok(_processoProducaoService.EntityListToResponseList(producoes));
        }

        /// <summary>
        /// Obter produção por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProcessoProducaoResponse>> BuscarProducaoPorId(int id)
        {
            var producao = await _processoProducaoService.BuscarProducaoPorIdAsync(id);
            if (producao == null) return NotFound();
            return Ok(_processoProducaoService.EntityToResponse(producao));
        }

        /// <summary>
        /// Criar uma nova produção
        /// </summary>
        /// <response code="200">Produto cadastrado com sucesso</response>
        /// <response code="400">Request incorreto</response>
        [HttpPost]
        public async Task<ActionResult<ProcessoProducaoResponse>> CadastrarProducao(ProcessoProducaoRequest req)
        {
            var forma = await _processoProducaoService.BuscarFormaPorIdAsync(req.FormaId);
            //var forma = await _context.Formas.FirstOrDefaultAsync(f => f.Id == req.FormaId);
            var producao = new ProcessoProducao(req.Data, req.MaquinaId, forma.Id, forma.ProdutoId, req.Ciclos);

            await _processoProducaoService.AdicionarAsync(producao);
            await _processoProducaoService.AtualizarAsync(producao);

            var producaoMateriasPrimas = _processoProducaoService.CriarProducoesMateriasPrimas(req.MateriasPrimas, producao.Id);
            foreach (var producaMateriaPrima in producaoMateriasPrimas)
            {
                await _producaoMateriaPrimaService.AdicionarAsync(producaMateriaPrima);
            }

            return Ok(producao);
        }

        /// <summary>
        /// Atualizar uma produção
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ProcessoProducaoResponse>> AtualizarProducao(int id, ProcessoProducaoRequest req)
        {
            var producao = await _processoProducaoService.BuscarProducaoPorIdAsync(id);
            if (producao == null) return NotFound();

            _producaoMateriaPrimaService.VerificarProducoesMateriasPrimasExistentes(id, req.MateriasPrimas);

            producao.Data = req.Data;
            producao.MaquinaId = req.MaquinaId;
            producao.FormaId = req.FormaId;
            producao.Ciclos = req.Ciclos;

            await _processoProducaoService.AtualizarAsync(producao);
            return Ok(producao);
        }

        /// <summary>
        /// Inativar uma produção
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ProcessoProducaoResponse>> InativarProducao(int id)
        {
            var producao = await _processoProducaoService.BuscarProducaoPorIdAsync(id);
            if (producao == null) return NotFound();
            producao.Ativo = false;

            await _processoProducaoService.AtualizarAsync(producao);
            return Ok(producao);
        }

        /// <summary>
        /// Calcular uma produção
        /// </summary>
        [HttpPost("CalcularProducao/{id}")]
        public async Task<ActionResult<ProcessoProducao>> CalcularProducao(int id)
        {
            await _processoProducaoService.CalcularProducao(id);
            return Ok();
        }
    }
}

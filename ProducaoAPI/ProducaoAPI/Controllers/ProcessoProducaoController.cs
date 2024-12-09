﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProducaoAPI.Data;
using ProducaoAPI.Models;
using ProducaoAPI.Requests;
using ProducaoAPI.Services;

namespace ProducaoAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ProcessoProducaoController : Controller
    {
        private readonly ProducaoContext _context;
        public ProcessoProducaoController(ProducaoContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProcessoProducao>>> ListarProducoes()
        {
            var producoes = await _context.Producoes.Include(p => p.ProducaoMateriasPrimas).ThenInclude(p => p.MateriaPrima).Where(m => m.Ativo == true).ToListAsync();
            if (producoes == null) return NotFound();
            return Ok(ProcessoProducaoServices.EntityListToResponseList(producoes));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProcessoProducao>> BuscarProducaoPorId(int id)
        {
            var producao = await _context.Producoes.FindAsync(id);
            if (producao == null) return NotFound();
            return Ok(ProcessoProducaoServices.EntityToResponse(producao));
        }

        [HttpPost]
        public async Task<ActionResult<ProcessoProducao>> CadastrarProducao(ProcessoProducaoRequest req)
        {
            var forma = await _context.Formas.FirstOrDefaultAsync(f => f.Id == req.FormaId);
            var producao = new ProcessoProducao(req.Data, req.MaquinaId, req.FormaId, forma.ProdutoId, req.Ciclos);
            await _context.Producoes.AddAsync(producao);
            await _context.SaveChangesAsync();

            var producaoMateriasPrimas = ProcessoProducaoServices.CriarProducoesMateriasPrimas(_context, req.MateriasPrimas, producao.Id);
            foreach (var producaMateriaPrima in producaoMateriasPrimas)
            {
                await _context.ProducoesMateriasPrimas.AddAsync(producaMateriaPrima);
                await _context.SaveChangesAsync();
            }

            return Ok(producao);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ProcessoProducao>> AtualizarProducao(int id, ProcessoProducaoRequest req)
        {
            var producao = await _context.Producoes.FindAsync(id);
            if (producao == null) return NotFound();

            producao.Data = req.Data;
            producao.MaquinaId = req.MaquinaId;
            producao.FormaId = req.FormaId;
            producao.Ciclos = req.Ciclos;
            

            await _context.SaveChangesAsync();
            return Ok(producao);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<ProcessoProducao>> InativarProducao(int id)
        {
            var producao = await _context.Producoes.FindAsync(id);
            if (producao == null) return NotFound();
            producao.Ativo = false;

            await _context.SaveChangesAsync();
            return Ok(producao);
        }
    }
}

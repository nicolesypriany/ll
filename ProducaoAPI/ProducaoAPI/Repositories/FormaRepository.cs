﻿using Microsoft.EntityFrameworkCore;
using ProducaoAPI.Data;
using ProducaoAPI.Models;
using ProducaoAPI.Repositories.Interfaces;

namespace ProducaoAPI.Repositories
{
    public class FormaRepository : IFormaRepository
    {
        private readonly ProducaoContext _context;
        public FormaRepository(ProducaoContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Forma>> ListarFormasAsync()
        {
            try
            {
                var formas = await _context.Formas.Where(m => m.Ativo == true).Include(f => f.Maquinas).Include(f => f.Produto).ToListAsync();
                if (formas == null || formas.Count == 0) throw new NullReferenceException("Nenhuma forma encontrada.");
                return formas;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<Forma> BuscarFormaPorIdAsync(int id)
        {
            try
            {
                var forma = await _context.Formas.Include(f => f.Maquinas).Include(f => f.Produto).FirstOrDefaultAsync(f => f.Id == id);
                if (forma == null) throw new NullReferenceException("ID da forma não encontrado.");
                return forma;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task AdicionarAsync(Forma forma)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(forma.Nome)) throw new ArgumentException("O campo \"Nome\" não pode estar vazio.");

                if (forma.PecasPorCiclo < 1) throw new ArgumentException("O número de peças por ciclo deve ser maior do que 0.");

                //OBS.: Alterar coluna de peças por ciclo no banco de dados para adicionar regra de que o número deve ser maior que 0.

                await _context.Formas.AddAsync(forma);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task AtualizarAsync(Forma forma)
        {
            _context.Formas.Update(forma);
            await _context.SaveChangesAsync();
        }
    }
}
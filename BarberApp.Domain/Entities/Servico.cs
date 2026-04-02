using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberApp.Domain.Entities
{
    public class Servico : BaseEntity
    {
        public string Nome { get; private set; } = string.Empty;
        public string? Descricao { get; private set; }
        public decimal Preco { get; private set; }
        public int DuracaoMinuto { get; private set; }
        public bool Ativo { get; protected set; } = true;

        //EF Core Precisa de construtor vazio
        protected Servico() { }

        public Servico(string nome, decimal preco, int duracaoMinuto, string? descricao = null)
        {
            Nome = nome;
            Preco = preco;
            DuracaoMinuto = duracaoMinuto;
            Descricao = descricao;
        }

        public void Atualizar(string nome, decimal preco, int duracaoMinuto, string? descricao )
        {
            Nome = nome;
            Preco = preco;
            DuracaoMinuto = duracaoMinuto;
            Descricao = descricao;
            AtualizadoEm = DateTime.UtcNow;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberApp.Domain.Entities
{
    public class Barbeiro : BaseEntity
    {
        public string Nome { get; private set; } = string.Empty;
        public string Telefone { get; private set; } = string.Empty;
        public string? Foto { get; private set; }
        public bool Ativo { get; protected set; } = true;

        //EF Core Precisa de construtor vazio
        protected Barbeiro() { }

        public Barbeiro(string nome, string telefone, string? foto)
        {
            Nome = nome;
            Telefone = telefone;
            Foto = foto;
        }

        public void Atualizar(string nome, string telefone, string? foto)
        {
            Nome = nome;
            Telefone = telefone;
            Foto = foto;
            AtualizadoEm = DateTime.UtcNow;

        }
        public void Desativar() => Ativo = false;
        
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberApp.Domain.Entities
{
    public class Cliente : BaseEntity
    {
        public string Nome { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Telefone { get; private set; } = string.Empty;

        //EF Core Precisa de construtor vazio
        protected Cliente() { }

        public Cliente(string nome, string email, string telefone)
        {
            Nome = nome;
            Email = email;
            Telefone = telefone;
        }
    }
}
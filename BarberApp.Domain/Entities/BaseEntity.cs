using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BarberApp.Domain.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public DateTime CridoEm { get; private set; } = DateTime.UtcNow;
        public DateTime? AtualizadoEm { get; protected set; }
    }
}
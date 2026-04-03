# ✂️ BarberApp API

> API REST para sistema de agendamento de barbearia — construída com .NET 10, Clean Architecture e autenticação JWT.

---

## 🚀 Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET / ASP.NET Core | 10 | Framework principal |
| Entity Framework Core | 10 | ORM |
| PostgreSQL | 15+ | Banco de dados |
| ASP.NET Identity | 10 | Gestão de usuários |
| JWT Bearer | 10 | Autenticação |
| Swashbuckle | 6.9 | Documentação Swagger |

---

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture**, separando responsabilidades em camadas independentes:

```
BarberApp/
├── BarberApp.Domain          → Entidades, interfaces e regras de negócio
│   ├── Entities/             → Barbeiro, Cliente, Servico, Agendamento
│   ├── Enums/                → StatusAgendamento
│   └── Interfaces/           → Contratos dos repositórios
│
├── BarberApp.Application     → Casos de uso e serviços
│   ├── Services/             → BarbeiroService, AgendamentoService, TokenService...
│   └── DTOs/                 → Objetos de transferência de dados
│
├── BarberApp.Infrastructure  → Implementações de infraestrutura
│   ├── Data/                 → AppDbContext, Migrations
│   ├── Repositories/         → Implementações dos repositórios
│   └── Identity/             → ApplicationUser
│
└── BarberApp.API             → Camada de entrada HTTP
    ├── Controllers/          → BarbeirosController, AgendamentosController...
    └── Program.cs            → Configuração e injeção de dependência
```

> Cada camada depende apenas das camadas internas — o Domain não conhece nada externo, garantindo que as regras de negócio sejam independentes de tecnologia.

---

## ⚙️ Como rodar localmente

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 15+](https://www.postgresql.org/download/)

### 1. Clone o repositório

```bash
git clone https://github.com/SEU_USUARIO/barberapp-api.git
cd barberapp-api
```

### 2. Configure os secrets locais

Crie o arquivo `BarberApp.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=barberapp;Username=postgres;Password=sua_senha"
  },
  "JwtSettings": {
    "SecretKey": "sua-chave-secreta-minimo-32-caracteres",
    "Issuer": "BarberApp",
    "Audience": "BarberAppUsers",
    "ExpiracaoHoras": 8
  }
}
```

### 3. Execute as migrations

```bash
dotnet ef database update --project BarberApp.Infrastructure --startup-project BarberApp.API
```

### 4. Rode a API

```bash
dotnet run --project BarberApp.API
```

### 5. Acesse o Swagger

```
http://localhost:5087/swagger
```

> Na primeira inicialização, o sistema cria automaticamente as roles `Admin` e `Cliente`, além de um usuário administrador padrão:
> - **Email:** admin@barberapp.com
> - **Senha:** Admin@123

---

## 🔐 Autenticação

A API usa **JWT Bearer Token**. Para acessar endpoints protegidos:

1. Faça login em `POST /api/auth/login`
2. Copie o token retornado
3. No Swagger, clique em **Authorize** e informe: `Bearer {seu_token}`

### Roles disponíveis

| Role | Descrição |
|---|---|
| `Admin` | Acesso total — gerencia barbeiros, serviços e agendamentos |
| `Cliente` | Acesso limitado — cria e cancela seus próprios agendamentos |

---

## 📡 Endpoints

### Autenticação

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `POST` | `/api/auth/registro` | Público | Registra novo usuário |
| `POST` | `/api/auth/login` | Público | Retorna token JWT |

### Barbeiros

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/barbeiros` | Público | Lista barbeiros ativos |
| `GET` | `/api/barbeiros/{id}` | Público | Busca barbeiro por ID |
| `POST` | `/api/barbeiros` | Admin | Cadastra novo barbeiro |

### Serviços

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/servicos` | Público | Lista serviços ativos |
| `POST` | `/api/servicos` | Admin | Cadastra novo serviço |

### Clientes

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/clientes/{id}` | Autenticado | Busca cliente por ID |
| `POST` | `/api/clientes` | Público | Cadastra novo cliente |

### Agendamentos

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/agendamentos` | Admin | Lista todos os agendamentos |
| `GET` | `/api/agendamentos/{id}` | Autenticado | Busca agendamento por ID |
| `POST` | `/api/agendamentos` | Autenticado | Cria novo agendamento |
| `PATCH` | `/api/agendamentos/{id}/confirmar` | Admin | Confirma agendamento |
| `PATCH` | `/api/agendamentos/{id}/cancelar` | Autenticado | Cancela agendamento |

---

## 🧠 Regras de negócio

- Não é possível agendar dois clientes no mesmo horário para o mesmo barbeiro
- O sistema considera a duração do serviço na verificação de conflito
- Agendamentos só podem ser confirmados se estiverem `Pendente`
- Agendamentos só podem ser cancelados se não estiverem `Concluido`
- O e-mail do cliente deve ser único no sistema

---

## 💳 Pagamento (em desenvolvimento)

A arquitetura já prevê um gap para integração com gateway de pagamento. A interface `IPaymentService` será plugável, permitindo integrar **MercadoPago** ou **Stripe** sem alterar as regras de negócio.

---

## 📁 Modelo de dados

```
Cliente ──────────────────────────────┐
                                      ↓
Barbeiro ──→ Agendamento ←── Servico (com preço e duração)
                  ↓
           [Status: Pendente → Confirmado → Concluido]
                              ↓
                       [Pagamento - gap]
```

---

## 👨‍💻 Autor: José Tyllan Pinto Almeida

Desenvolvido como projeto de portfólio para demonstrar domínio de:

- Clean Architecture em .NET
- API REST com boas práticas
- Autenticação e autorização com JWT
- Entity Framework Core com PostgreSQL
- Padrões SOLID e separação de responsabilidades

---

*Projeto em desenvolvimento ativo — contribuições e sugestões são bem-vindas.*
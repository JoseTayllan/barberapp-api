# ✂️ BarberApp API

> API REST para sistema de agendamento de barbearia — construída com .NET 10, Clean Architecture e autenticação JWT.

---

## 🚀 Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET / ASP.NET Core | 10 | Framework principal |
| Entity Framework Core | 10 | ORM |
| PostgreSQL | 18+ | Banco de dados |
| ASP.NET Identity | 10 | Gestão de usuários |
| JWT Bearer | 10 | Autenticação |
| FluentValidation | latest | Validação de entrada |
| Swashbuckle | 6.9 | Documentação Swagger |

---

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture**, separando responsabilidades em camadas independentes:

```
BarberApp/
├── BarberApp.Domain          → Entidades, interfaces e regras de negócio
│   ├── Entities/             → Barbeiro, Cliente, Servico, Agendamento, Pagamento
│   ├── Enums/                → StatusAgendamento, StatusPagamento
│   └── Interfaces/           → Contratos dos repositórios e IPaymentService
│
├── BarberApp.Application     → Casos de uso e serviços
│   ├── Services/             → BarbeiroService, AgendamentoService, PagamentoService, TokenService...
│   ├── DTOs/                 → Objetos de transferência de dados
│   └── Validators/           → Validações com FluentValidation
│
├── BarberApp.Infrastructure  → Implementações de infraestrutura
│   ├── Data/                 → AppDbContext, Migrations
│   ├── Repositories/         → Implementações dos repositórios
│   ├── Identity/             → ApplicationUser
│   └── Payment/              → MockPaymentService (plugável)
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
- [PostgreSQL 18+](https://www.postgresql.org/download/)

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

## 🔄 Fluxo de uso

```
1. POST /api/auth/registro          → cria conta + perfil de cliente automaticamente
2. POST /api/auth/login             → obtém o token JWT
3. POST /api/agendamentos           → informa barbeiro, serviço e horário
4. POST /api/pagamentos/{id}        → realiza o pagamento
5. PATCH /api/agendamentos/{id}/confirmar → Admin confirma o agendamento
```

> O `ClienteId` é resolvido automaticamente pelo token JWT — o cliente não precisa informá-lo.

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
| `Cliente` | Acesso limitado — visualiza e cancela seus próprios agendamentos |

---

## 📡 Endpoints

### Autenticação

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `POST` | `/api/auth/registro` | Público | Registra usuário e cria perfil de cliente |
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
| `GET` | `/api/servicos` | Público | Lista serviços ativos com preços e duração |
| `POST` | `/api/servicos` | Admin | Cadastra novo serviço |

### Clientes

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/clientes/meu-perfil` | Autenticado | Retorna perfil do cliente logado |
| `GET` | `/api/clientes/{id}` | Autenticado | Busca cliente por ID |

### Agendamentos

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/agendamentos` | Admin / Cliente | Admin vê todos; Cliente vê os próprios |
| `GET` | `/api/agendamentos/{id}` | Autenticado | Busca agendamento por ID |
| `POST` | `/api/agendamentos` | Autenticado | Cria agendamento (ClienteId resolvido pelo token) |
| `PATCH` | `/api/agendamentos/{id}/confirmar` | Admin | Confirma agendamento |
| `PATCH` | `/api/agendamentos/{id}/cancelar` | Autenticado | Cancela agendamento |

### Pagamentos

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `POST` | `/api/pagamentos/{agendamentoId}` | Autenticado | Processa pagamento do agendamento |

---

## 🧠 Regras de negócio

- Registro cria automaticamente o perfil de cliente — sem etapa dupla
- O `ClienteId` no agendamento é resolvido automaticamente pelo token JWT
- Não é possível agendar dois clientes no mesmo horário para o mesmo barbeiro
- O sistema considera a duração do serviço na verificação de conflito de horário
- Agendamentos só podem ser confirmados se estiverem com status `Pendente`
- Agendamentos só podem ser cancelados se não estiverem `Concluido`
- Pagamentos já aprovados não podem ser processados novamente
- Apenas pagamentos aprovados podem ser reembolsados
- O e-mail do cliente deve ser único no sistema

---

## 💳 Pagamento

A arquitetura usa o padrão **plugável** via interface `IPaymentService`. Atualmente utiliza `MockPaymentService` que simula aprovações. Para integrar um gateway real, basta criar uma nova implementação da interface:

```csharp
// Futuro — sem alterar nenhuma regra de negócio
public class MercadoPagoService : IPaymentService { ... }
public class StripeService : IPaymentService { ... }
```

---

## 📁 Modelo de dados

```
Cliente ──────────────────────────────────┐
                                          ↓
Barbeiro ──→ Agendamento ←────── Servico (preço + duração)
                  │
                  ↓
           [Status: Pendente → Confirmado → Concluido]
                  │
                  ↓
             Pagamento
           [Status: Pendente → Aprovado → Reembolsado]
                  │
                  ↓
          [Gateway: Mock → MercadoPago → Stripe]
```

---

## 👨‍💻 Autor: José Tyllan Pinto Almeida

Desenvolvido como projeto de portfólio para demonstrar domínio de:

- Clean Architecture em .NET 10
- API REST com boas práticas e versionamento
- Autenticação e autorização com JWT e roles
- Entity Framework Core com PostgreSQL
- Validações com FluentValidation
- Padrões SOLID e separação de responsabilidades
- Arquitetura extensível para integrações futuras

---

*Projeto em desenvolvimento ativo — contribuições e sugestões são bem-vindas.*

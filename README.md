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
| FluentValidation | latest | Validação de entrada |
| Swashbuckle | 6.9 | Documentação Swagger |

---

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture**:

```
BarberApp/
├── BarberApp.Domain          → Entidades, interfaces e regras de negócio
│   ├── Entities/             → Barbeiro, Cliente, Servico, Agendamento, Pagamento, DisponibilidadeBarbeiro
│   ├── Enums/                → StatusAgendamento, StatusPagamento, DiaSemana
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
├── BarberApp.API             → Camada de entrada HTTP
│   ├── Controllers/          → Controllers REST
│   └── Program.cs            → Configuração e injeção de dependência
│
├── BarberApp.UnitTests       → Testes unitários (xUnit + Moq)
└── BarberApp.IntegrationTests→ Testes de integração (WebApplicationFactory)
```

---

## 👥 Roles e Permissões

O sistema tem três perfis de usuário:

| Role | Descrição |
|---|---|
| `Admin` | Dono da barbearia — gerencia tudo, cria barbeiros |
| `Barbeiro` | Funcionário — vê seus agendamentos, gerencia sua agenda |
| `Cliente` | Usuário final — agenda, paga e cancela seus próprios agendamentos |

### Como cada role é criada

- **Admin** — criado automaticamente na primeira inicialização
- **Barbeiro** — criado pelo Admin via `POST /api/barbeiros`
- **Cliente** — criado pelo próprio usuário via `POST /api/auth/registro`

---

## 🔄 Fluxos principais

### Fluxo do Cliente
```
1. POST /api/auth/registro          → cria conta + perfil automaticamente
2. POST /api/auth/login             → obtém token JWT
3. GET  /api/barbeiros              → lista barbeiros disponíveis
4. GET  /api/servicos               → lista serviços e preços
5. GET  /api/agendamentos/horarios-disponiveis?barbeiroId=&servicoId=&data=
                                    → consulta horários livres
6. POST /api/agendamentos           → cria agendamento (sem informar ClienteId)
7. POST /api/pagamentos/{id}        → realiza pagamento
8. GET  /api/agendamentos           → acompanha seus agendamentos
```

### Fluxo do Barbeiro
```
1. POST /api/auth/login             → obtém token JWT
2. POST /api/barbeiros/{id}/disponibilidades
                                    → define sua agenda por dia da semana
3. GET  /api/agendamentos           → vê seus agendamentos do dia
4. GET  /api/perfil                 → visualiza seu perfil
5. PUT  /api/perfil                 → atualiza seus dados
```

### Fluxo do Admin
```
1. POST /api/auth/login             → obtém token JWT
2. POST /api/barbeiros              → cadastra barbeiro com login
3. POST /api/servicos               → cadastra serviços e preços
4. GET  /api/agendamentos           → vê todos os agendamentos
5. PATCH /api/agendamentos/{id}/confirmar → confirma agendamento
```

---

## 🔐 Autenticação

A API usa **JWT Bearer Token**.

Para acessar endpoints protegidos, envie o header:
```
Authorization: Bearer {token}
```

O token contém os seguintes claims:
```json
{
  "nameid": "user-id",
  "email": "usuario@email.com",
  "unique_name": "Nome Completo",
  "role": "Cliente | Barbeiro | Admin",
  "BarbeiroId": "guid (apenas para role Barbeiro)",
  "exp": 1234567890
}
```

---

## 📡 Endpoints completos

### 🔑 Autenticação — `/api/auth`

| Método | Rota | Acesso | Descrição | Body |
|---|---|---|---|---|
| `POST` | `/api/auth/registro` | Público | Cria conta + perfil de cliente | `{ nomeCompleto, email, telefone, password }` |
| `POST` | `/api/auth/login` | Público | Retorna token JWT | `{ email, senha }` |

**Resposta do login/registro:**
```json
{
  "token": "eyJ...",
  "nome": "João Silva",
  "email": "joao@email.com",
  "roles": ["Cliente"],
  "expiraEm": "2026-05-01T09:00:00Z"
}
```

---

### 👤 Perfil — `/api/perfil`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/perfil` | Autenticado | Retorna perfil do usuário logado |
| `PUT` | `/api/perfil` | Autenticado | Atualiza nome e telefone |
| `PATCH` | `/api/perfil/alterar-senha` | Autenticado | Altera a senha |

**Body PUT:**
```json
{ "nomeCompleto": "Nome Atualizado", "telefone": "62999990000" }
```

**Body PATCH alterar-senha:**
```json
{ "senhaAtual": "Senha@123", "novaSenha": "NovaSenha@456" }
```

---

### 💈 Barbeiros — `/api/barbeiros`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/barbeiros` | Público | Lista barbeiros ativos |
| `GET` | `/api/barbeiros/{id}` | Público | Busca barbeiro por ID |
| `POST` | `/api/barbeiros` | Admin | Cria barbeiro com login |

**Body POST (Admin cria barbeiro):**
```json
{
  "nomeCompleto": "Carlos Silva",
  "email": "carlos@barbearia.com",
  "telefone": "62999990001",
  "senha": "Barbeiro@123",
  "foto": null
}
```

**Resposta GET lista:**
```json
[
  { "id": "guid", "nome": "Carlos Silva", "telefone": "62999990001", "foto": null }
]
```

---

### 📅 Disponibilidades — `/api/barbeiros/{barbeiroId}/disponibilidades`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/barbeiros/{id}/disponibilidades` | Público | Lista agenda do barbeiro |
| `POST` | `/api/barbeiros/{id}/disponibilidades` | Admin / Barbeiro | Define agenda por dia |

**Body POST:**
```json
{
  "diaSemana": "Segunda",
  "horarioInicio": "08:00",
  "horarioFim": "18:00"
}
```

**Dias válidos:** `Domingo, Segunda, Terca, Quarta, Quinta, Sexta, Sabado`

**Resposta:**
```json
{
  "id": "guid",
  "diaSemana": "Segunda",
  "horarioInicio": "08:00",
  "horarioFim": "18:00",
  "ativo": true
}
```

---

### ✂️ Serviços — `/api/servicos`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/servicos` | Público | Lista serviços ativos |
| `POST` | `/api/servicos` | Admin | Cadastra novo serviço |

**Body POST:**
```json
{
  "nome": "Corte Degradê",
  "preco": 45.00,
  "duracaoMinutos": 45,
  "descricao": "Corte moderno com máquina e tesoura"
}
```

**Resposta GET lista:**
```json
[
  {
    "id": "guid",
    "nome": "Corte Degradê",
    "descricao": "Corte moderno com máquina e tesoura",
    "preco": 45.00,
    "duracaoMinutos": 45
  }
]
```

---

### 👤 Clientes — `/api/clientes`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/clientes/meu-perfil` | Autenticado | Retorna perfil do cliente logado |
| `GET` | `/api/clientes/{id}` | Autenticado | Busca cliente por ID |

---

### 🗓️ Agendamentos — `/api/agendamentos`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/agendamentos` | Autenticado | Admin vê todos; Barbeiro vê os seus; Cliente vê os seus |
| `GET` | `/api/agendamentos/{id}` | Autenticado | Busca agendamento por ID |
| `GET` | `/api/agendamentos/horarios-disponiveis` | Público | Retorna slots disponíveis |
| `POST` | `/api/agendamentos` | Autenticado | Cria agendamento |
| `PATCH` | `/api/agendamentos/{id}/confirmar` | Admin | Confirma agendamento |
| `PATCH` | `/api/agendamentos/{id}/cancelar` | Autenticado | Cancela agendamento |

**Query params — horarios-disponiveis:**
```
GET /api/agendamentos/horarios-disponiveis
  ?barbeiroId=guid
  &servicoId=guid
  &data=10/05/2026
```

**Resposta horarios-disponiveis:**
```json
[
  { "horario": "08:00", "disponivel": true },
  { "horario": "08:45", "disponivel": false },
  { "horario": "09:30", "disponivel": true }
]
```

**Body POST agendamento:**
```json
{
  "barbeiroId": "guid",
  "servicoId": "guid",
  "dataHora": "2026-05-10T10:00:00",
  "observacao": "Primeira visita"
}
```

> ⚠️ `ClienteId` não é necessário — é resolvido automaticamente pelo token JWT.

**Resposta GET lista:**
```json
[
  {
    "id": "guid",
    "nomeCliente": "João Silva",
    "nomeBarbeiro": "Carlos Silva",
    "nomeServico": "Corte Degradê",
    "precoServico": 45.00,
    "dataHora": "2026-05-10T10:00:00Z",
    "status": "Pendente"
  }
]
```

**Status possíveis:** `Pendente → Confirmado → Concluido` / `Cancelado`

---

### 💳 Pagamentos — `/api/pagamentos`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `POST` | `/api/pagamentos/{agendamentoId}` | Autenticado | Processa pagamento |

**Resposta:**
```json
{
  "id": "guid",
  "valor": 45.00,
  "status": "Aprovado",
  "gatewayTransacaoId": "MOCK-guid",
  "gateway": "Mock"
}
```

**Status possíveis:** `Pendente → Aprovado → Reembolsado` / `Recusado`

---

## 🧠 Regras de negócio

- Registro cria automaticamente perfil de cliente — sem etapa dupla
- `ClienteId` resolvido pelo token JWT — cliente não precisa informá-lo
- Barbeiro não pode agendar no seu próprio dia sem agenda cadastrada
- Sistema valida se horário está dentro do expediente do barbeiro
- Não é possível agendar dois clientes no mesmo horário para o mesmo barbeiro
- Conflito considera a duração do serviço
- Agendamentos `Cancelado` liberam o horário para novos agendamentos
- Agendamentos só confirmam se estiverem `Pendente`
- Agendamentos só cancelam se não estiverem `Concluido`
- Pagamentos aprovados não podem ser reprocessados
- E-mail único por usuário no sistema

---

## ⚠️ Códigos de resposta

| Código | Significado |
|---|---|
| `200` | Sucesso |
| `201` | Criado com sucesso |
| `204` | Sucesso sem conteúdo |
| `400` | Dados inválidos — ver campo `erros` ou `mensagem` |
| `401` | Não autenticado — token ausente ou inválido |
| `403` | Sem permissão — role insuficiente |
| `404` | Recurso não encontrado |
| `500` | Erro interno do servidor |

**Formato de erro:**
```json
{ "mensagem": "Descrição do erro" }
```
```json
{ "erros": ["Campo X é obrigatório.", "Campo Y inválido."] }
```

---

## 💳 Pagamento

Arquitetura plugável via `IPaymentService`. Atualmente usa `MockPaymentService`. Para integrar gateway real:

```csharp
public class MercadoPagoService : IPaymentService { ... }
public class StripeService : IPaymentService { ... }
```

---

## 📁 Modelo de dados

```
ApplicationUser (Identity)
  ├── Role: Admin
  ├── Role: Barbeiro ──→ Barbeiro (entidade)
  │                         └── DisponibilidadeBarbeiro (por dia da semana)
  └── Role: Cliente  ──→ Cliente (entidade)

Agendamento
  ├── ClienteId ──→ Cliente
  ├── BarbeiroId ──→ Barbeiro
  ├── ServicoId ──→ Servico
  ├── Status: Pendente → Confirmado → Concluido / Cancelado
  └── Pagamento
        └── Status: Pendente → Aprovado → Reembolsado / Recusado
```

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
Crie `BarberApp.API/appsettings.Development.json`:
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
  },
  "Barbearia": {
    "HorarioAbertura": "08:00",
    "HorarioFechamento": "18:00"
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

> Na primeira inicialização o sistema cria automaticamente:
> - Roles: `Admin`, `Barbeiro`, `Cliente`
> - Usuário admin padrão: `admin@barberapp.com` / `Admin@123`

---

## 🧪 Testes

```bash
# Todos os testes
dotnet test

# Apenas unitários
dotnet test BarberApp.UnitTests/BarberApp.UnitTests.csproj

# Apenas integração
dotnet test BarberApp.IntegrationTests/BarberApp.IntegrationTests.csproj
```

---

## 👨‍💻 Autor: José Tyllan Pinto Almeida

Desenvolvido como projeto de portfólio para demonstrar domínio de:

- Clean Architecture em .NET 10
- API REST com boas práticas
- Autenticação e autorização com JWT e roles
- Entity Framework Core com PostgreSQL
- Validações com FluentValidation
- Padrões SOLID e separação de responsabilidades
- Arquitetura extensível para integrações futuras
- Testes automatizados com xUnit, Moq e FluentAssertions

---

*Projeto em desenvolvimento ativo — contribuições e sugestões são bem-vindas.*

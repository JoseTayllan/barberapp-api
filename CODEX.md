# 📖 CODEX — BarberApp API

> Este documento é a referência viva do projeto. Todo novo código, decisão de arquitetura ou padrão adotado deve ser refletido aqui. O objetivo principal deste projeto é **aprendizado ativo** — cada linha de código deve ser lida, entendida e questionada antes de ser aceita.

---

## 🎯 Propósito do projeto

O BarberApp é uma API REST de agendamento para barbearia construída com foco em **aprendizado de engenharia de software na prática**. Isso significa que:

- Todo código novo deve ser compreendido por quem o escreve — não apenas copiado
- Decisões de arquitetura têm motivo — e o motivo está documentado
- Erros e refatorações fazem parte do processo e são bem-vindos
- A qualidade do código importa tanto quanto a funcionalidade

---

## 🏗️ Visão geral da arquitetura

O projeto segue **Clean Architecture**, dividida em quatro camadas com dependências unidirecionais — camadas internas nunca conhecem as externas.

### Domain
O núcleo do sistema. Contém entidades, enums e interfaces. Não depende de nenhum framework externo — apenas C# puro. As regras de negócio vivem aqui dentro das próprias entidades (ex: `Agendamento.Confirmar()` valida o estado antes de mudar). Essa camada nunca muda por causa de troca de banco ou framework.

### Application
Orquestra os casos de uso. Os Services coordenam repositórios, aplicam regras que envolvem múltiplas entidades e retornam dados via DTOs. Não conhece EF Core, Identity ou HTTP — só interfaces e entidades do Domain. Os DTOs definem o contrato de entrada e saída da API.

### Infrastructure
Implementa as interfaces definidas no Domain. Aqui vivem o `AppDbContext`, os repositórios com EF Core, o `ApplicationUser` do Identity e o `MockPaymentService`. É a única camada que conhece PostgreSQL, migrations e pacotes externos de persistência.

### API
Camada de entrada HTTP. Os Controllers recebem requisições, chamam os Services e retornam respostas HTTP. Não contém lógica de negócio — apenas orquestração de entrada/saída, autenticação via JWT e tratamento de erros.

```
Domain ← Application ← Infrastructure
   ↑                          ↑
   └──────────── API ─────────┘
```

---

## 📁 Estrutura de pastas

```
BarberApp/
├── BarberApp.Domain/
│   ├── Entities/          → BaseEntity, Barbeiro, Cliente, Servico,
│   │                         Agendamento, Pagamento, DisponibilidadeBarbeiro,
│   │                         ExcecaoAgenda
│   ├── Enums/             → StatusAgendamento, StatusPagamento, DiaSemana
│   └── Interfaces/        → IRepositórios, IPaymentService
│
├── BarberApp.Application/
│   ├── DTOs/              → Records de entrada e saída por domínio
│   ├── Services/          → Um service por agregado de negócio
│   └── Validators/        → FluentValidation por DTO de entrada
│
├── BarberApp.Infrastructure/
│   ├── Data/              → AppDbContext + Migrations
│   ├── Identity/          → ApplicationUser
│   ├── Payment/           → MockPaymentService
│   └── Repositories/      → Implementações dos repositórios
│
├── BarberApp.API/
│   ├── Controllers/       → Um controller por recurso REST
│   └── Program.cs         → Composição de toda a aplicação
│
├── BarberApp.UnitTests/
│   ├── Domain/            → Testes das entidades e regras de negócio
│   └── Services/          → Testes dos services com mocks
│
└── BarberApp.IntegrationTests/
    └── *IntegrationTests  → Testes de fluxo completo via HTTP
```

---

## 🔐 Sistema de autenticação e roles

A API usa **JWT Bearer Token** com três roles:

| Role | Criado por | Acesso |
|---|---|---|
| `Admin` | Sistema (startup) | Tudo |
| `Barbeiro` | Admin via `POST /api/barbeiros` | Próprios agendamentos e agenda |
| `Cliente` | Auto-registro via `POST /api/auth/registro` | Próprios agendamentos |

O token carrega os claims: `nameid`, `email`, `unique_name`, `role` e `BarbeiroId` (apenas para Barbeiros). Tokens não são invalidados no servidor — expiram em 8 horas por padrão.

---

## ✏️ Convenções de código

### Nomenclatura

```csharp
// Classes, interfaces, métodos, propriedades → PascalCase
public class AgendamentoService { }
public interface IAgendamentoRepository { }
public async Task<Agendamento> CriarAsync() { }
public string NomeCompleto { get; private set; }

// Variáveis locais e parâmetros → camelCase
var agendamento = await _repo.ObterPorIdAsync(id);
public async Task CriarAsync(Guid barbeiroId, string emailCliente)

// Campos privados → _camelCase com underscore
private readonly IAgendamentoRepository _agendamentoRepo;

// DTOs → sufixo Request (entrada) e Response (saída)
public record CriarAgendamentoRequest(...);
public record AgendamentoResponse(...);
```

### Entidades

- Propriedades com `private set` — só a própria entidade muda seu estado
- Construtor protegido vazio para o EF Core: `protected Agendamento() { }`
- Construtor público com parâmetros obrigatórios
- Métodos de negócio com nomes claros: `Confirmar()`, `Cancelar()`, `Desativar()`
- Validações de estado dentro dos métodos — nunca no controller

```csharp
// ✅ Correto — regra dentro da entidade
public void Confirmar()
{
    if (Status != StatusAgendamento.Pendente)
        throw new InvalidOperationException("Só pendentes podem ser confirmados.");
    Status = StatusAgendamento.Confirmado;
}

// ❌ Errado — regra no controller
if (agendamento.Status != StatusAgendamento.Pendente)
    return BadRequest("...");
agendamento.Status = StatusAgendamento.Confirmado;
```

### Services

- Um service por agregado de negócio
- Nunca retornam entidades diretamente para o controller — usam DTOs
- Lançam `Exception` com mensagem clara para erros de negócio
- Dependem apenas de interfaces — nunca de implementações concretas

### Controllers

- Apenas recebem, delegam e respondem
- Nunca contêm `if` de regra de negócio
- Sempre retornam `IActionResult`
- Usam `try/catch` apenas para converter exceptions em respostas HTTP

### Repositórios

- Métodos assíncronos com sufixo `Async`
- Nomes descritivos: `ObterPorBarbeiroEDataAsync`, não `GetData`
- Nunca expõem `IQueryable` — sempre retornam objetos materializados
- `SaveChangesAsync` dentro do próprio repositório

---

## 🗄️ Banco de dados

- **PostgreSQL** com EF Core Code First
- Migrations versionadas e nomeadas de forma descritiva
- Enums salvos como `string` no banco (`HasConversion<string>()`)
- Datas sempre em UTC no banco — conversão para Brasília feita na camada de aplicação
- Índices únicos explícitos onde há restrição de negócio

---

## ⌨️ Comandos essenciais

### Desenvolvimento

```bash
# Rodar a API
dotnet run --project BarberApp.API

# Build completo
dotnet build

# Watch mode (reload automático)
dotnet watch run --project BarberApp.API
```

### Banco de dados

```bash
# Criar nova migration
dotnet ef migrations add NomeDaMigration \
  --project BarberApp.Infrastructure \
  --startup-project BarberApp.API

# Aplicar migrations
dotnet ef database update \
  --project BarberApp.Infrastructure \
  --startup-project BarberApp.API

# Reverter última migration
dotnet ef migrations remove \
  --project BarberApp.Infrastructure \
  --startup-project BarberApp.API

# Ver migrations aplicadas
dotnet ef migrations list \
  --project BarberApp.Infrastructure \
  --startup-project BarberApp.API
```

### Testes

```bash
# Todos os testes
dotnet test

# Apenas unitários
dotnet test BarberApp.UnitTests/BarberApp.UnitTests.csproj

# Apenas integração
dotnet test BarberApp.IntegrationTests/BarberApp.IntegrationTests.csproj

# Com detalhes de cada teste
dotnet test --verbosity normal

# Com cobertura de código
dotnet test --collect:"XPlat Code Coverage"
```

### Git

```bash
# Padrão de commit (Conventional Commits)
git commit -m "feat: descrição da funcionalidade"
git commit -m "fix: descrição da correção"
git commit -m "refactor: descrição da refatoração"
git commit -m "docs: atualização de documentação"
git commit -m "test: adição ou correção de testes"
```

---

## 🧪 Estratégia de testes

### Testes unitários (`BarberApp.UnitTests`)
Testam uma classe isolada sem dependências externas.

- **Entidades** — toda regra de negócio deve ter teste. Se `Confirmar()` lança exceção, existe um teste para isso.
- **Services** — dependências mockadas com Moq. Testa os cenários: caminho feliz, entidade não encontrada, conflito de negócio.
- Nomenclatura: `MetodoTestado_DeveComportamento_QuandoCondicao`

```csharp
// Exemplo
[Fact]
public void Confirmar_DeveLancarExcecao_QuandoNaoPendente() { }

[Fact]
public async Task CriarAsync_DeveLancarExcecao_QuandoClienteNaoEncontrado() { }
```

### Testes de integração (`BarberApp.IntegrationTests`)
Testam o fluxo completo via HTTP com banco real.

- Usam `WebApplicationFactory<Program>`
- Emails com `Guid.NewGuid()` para evitar conflito de dados
- Testam status HTTP, estrutura da resposta e persistência

---

## 🚫 Regras de ouro — o que NUNCA fazer

### Arquitetura
- **NUNCA** referenciar `Infrastructure` a partir de `Application` ou `Domain`
- **NUNCA** usar `AppDbContext` diretamente no controller ou service — sempre via repositório
- **NUNCA** retornar entidades do Domain diretamente na API — sempre DTOs
- **NUNCA** colocar lógica de negócio no controller
- **NUNCA** colocar queries SQL ou LINQ complexas no service — isso é responsabilidade do repositório

### Segurança
- **NUNCA** commitar `appsettings.Development.json` — ele está no `.gitignore`
- **NUNCA** hardcodar senhas, chaves ou connection strings no código
- **NUNCA** expor stack trace ou detalhes internos em respostas de erro da API
- **NUNCA** confiar no `ClienteId` vindo do body — sempre resolver pelo token JWT

### Banco de dados
- **NUNCA** criar migration sem revisar o SQL gerado antes de aplicar
- **NUNCA** deletar migrations já aplicadas em produção
- **NUNCA** usar `DateTime.Now` — sempre `DateTime.UtcNow` e converter para Brasília na aplicação

### API e contratos
- **NUNCA** alterar o contrato público de um endpoint sem versionar a API
- **NUNCA** remover ou renomear campos de um DTO de resposta sem avaliar impacto no frontend
- **NUNCA** criar endpoint novo sem adicionar ao menos um teste de integração

### Testes
- **NUNCA** pular testes que falharam sem entender o motivo
- **NUNCA** mockar o que não precisa ser mockado — testes simples são melhores
- **NUNCA** testar implementação — teste comportamento

### Aprendizado
- **NUNCA** aceitar código sem entender o que ele faz
- **NUNCA** copiar solução sem questionar por que ela resolve o problema
- **NUNCA** avançar para a próxima feature com bugs conhecidos não resolvidos

---

## 💡 Princípios que guiam o projeto

**S — Single Responsibility:** cada classe faz uma coisa só. Service não sabe de HTTP. Controller não sabe de banco.

**O — Open/Closed:** `IPaymentService` permite adicionar MercadoPago sem alterar o `PagamentoService`.

**L — Liskov Substitution:** qualquer implementação de `IPaymentService` pode substituir o Mock sem quebrar o sistema.

**I — Interface Segregation:** interfaces pequenas e focadas (`IBarbeiroRepository` não carrega métodos de `IAgendamentoRepository`).

**D — Dependency Inversion:** services dependem de interfaces, não de classes concretas. O `Program.cs` é o único lugar que conhece as implementações reais.

---

## 🗺️ Roadmap atual

| Status | Item |
|---|---|
| ✅ | Clean Architecture com 4 camadas |
| ✅ | Entidades com regras de negócio |
| ✅ | EF Core + PostgreSQL + Migrations |
| ✅ | JWT com roles: Admin, Barbeiro, Cliente |
| ✅ | CRUD de Barbeiros, Serviços, Clientes |
| ✅ | Agendamento com validação de conflito e expediente |
| ✅ | Agenda padrão por dia da semana |
| ✅ | Exceções de agenda por data específica |
| ✅ | Horários disponíveis em tempo real |
| ✅ | Gap de pagamento plugável |
| ✅ | Gestão de perfil por role |
| ✅ | FluentValidation em todos os endpoints |
| ✅ | Segurança: cliente só acessa próprios dados |
| ⏳ | Testes automatizados completos |
| ⏳ | Deploy em produção |
| ⏳ | Gateway de pagamento real (MercadoPago) |
| ⏳ | Frontend |

---

*Este documento deve ser atualizado sempre que uma decisão de arquitetura for tomada, um padrão novo for adotado ou uma regra de negócio importante for implementada.*

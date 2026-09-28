# ✂️ BarberApp API

> API REST para sistema de agendamento de barbearia — construída com .NET 10, arquitetura em camadas, API key global e autenticação JWT.

---

## 🚀 Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET / ASP.NET Core | 10 | Framework principal |
| Entity Framework Core | 10 | ORM |
| PostgreSQL | 15+ | Banco de dados |
| ASP.NET Identity | 10 | Gestão de usuários |
| API key | — | Autenticação global do consumidor |
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
│   ├── Middleware/           → Barreira global de API key
│   ├── OpenApi/              → Requisitos ApiKey/Bearer no Swagger
│   └── Program.cs            → Configuração e injeção de dependência
│
└── BarberApp.IntegrationTests→ Testes de integração (xUnit + WebApplicationFactory)
```

No pipeline HTTP, CORS vem antes do middleware de API key; depois dele são executadas autenticação e autorização JWT. Assim, a API key decide se o consumidor pode alcançar a aplicação, enquanto JWT, roles, claims e regras de propriedade continuam decidindo o que o usuário pode fazer.

---

## 👥 Roles e Permissões

O sistema tem três perfis de usuário:

| Role | Descrição |
|---|---|
| `Admin` | Dono da barbearia — gerencia tudo, cria barbeiros |
| `Barbeiro` | Funcionário — vê seus agendamentos, gerencia sua agenda |
| `Cliente` | Usuário final — agenda, paga e cancela seus próprios agendamentos |

### Como cada role é criada

- **Admin** — criado somente pelo bootstrap administrativo opcional descrito em [Bootstrap inicial do administrador](#bootstrap-inicial-do-administrador)
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

A API usa duas barreiras complementares:

1. **API key global:** identifica a aplicação consumidora. Todos os endpoints da API, inclusive login, registro e rotas chamadas de “públicas” nas tabelas abaixo, exigem `X-API-Key`.
2. **JWT Bearer:** identifica o usuário e suas roles nas rotas protegidas por autenticação/autorização.

A API key válida não autentica um usuário e não substitui JWT. Para um endpoint protegido, envie os dois headers:

```
X-API-Key: {api-key-do-ambiente}
Authorization: Bearer {token}
```

Respostas da barreira de API key:

| Situação | Resposta |
|---|---|
| Header `X-API-Key` ausente | `401 Unauthorized` e `WWW-Authenticate: ApiKey` |
| API key inválida, vazia ou múltipla | `403 Forbidden` |
| API key válida | A requisição segue o fluxo normal, incluindo validação JWT quando aplicável |

Em `Development`, a interface e o documento Swagger (`/swagger` e `/swagger/v1/swagger.json`) permanecem públicos para viabilizar descoberta e testes. O Swagger declara o esquema `ApiKey`: operações públicas pedem somente a API key; operações protegidas pedem API key e Bearer. O preflight CORS também é processado antes da barreira de API key.

### Impacto para consumidores atuais

Consumidores existentes devem passar `X-API-Key` em **todas** as chamadas. URLs, verbos, corpos, respostas de sucesso e regras JWT existentes não mudam; a única adaptação é o novo header. Por exemplo:

```http
# Endpoint público para usuário, mas protegido por API key
GET /api/servicos HTTP/1.1
Host: localhost:5087
X-API-Key: {api-key-do-ambiente}

# Endpoint protegido pelas duas barreiras
GET /api/agendamentos HTTP/1.1
Host: localhost:5087
X-API-Key: {api-key-do-ambiente}
Authorization: Bearer {token-jwt}
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

Nas tabelas abaixo, **Público** significa “não exige JWT”; a API key global continua obrigatória.

### 🔑 Autenticação — `/api/auth`

| Método | Rota | Acesso | Descrição | Body |
|---|---|---|---|---|
| `POST` | `/api/auth/registro` | Público | Cria conta + perfil de cliente | `{ nomeCompleto, email, telefone, password }` |
| `POST` | `/api/auth/login` | Público | Retorna token JWT | `{ email, password }` |

**Resposta do login/registro:**
```json
{
  "token": "<TOKEN_JWT_RETORNADO>",
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
{ "senhaAtual": "<SENHA_ATUAL_PRIVADA>", "novaSenha": "<NOVA_SENHA_PRIVADA>" }
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
  "senha": "<SENHA_FORTE_PRIVADA_DO_BARBEIRO>",
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
| `401` | API key ausente, ou JWT ausente/inválido em endpoint protegido |
| `403` | API key inválida, ou usuário sem permissão suficiente |
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

Use variáveis de ambiente, Secret Manager ou um cofre do ambiente. Os valores abaixo são placeholders deliberadamente não utilizáveis; substitua-os somente no seu ambiente e nunca os versione:

```powershell
$env:ConnectionStrings__DefaultConnection = "<CONNECTION_STRING_PRIVADA>"
$env:JwtSettings__SecretKey = "<JWT_SECRET_PRIVADO_COM_ENTROPIA_ADEQUADA>"
$env:JwtSettings__Issuer = "<ISSUER_DO_AMBIENTE>"
$env:JwtSettings__Audience = "<AUDIENCE_DO_AMBIENTE>"
$env:JwtSettings__ExpiracaoHoras = "<HORAS_DE_EXPIRACAO>"
$env:ApiKey__Value = "<API_KEY_PRIVADA_GERADA_ALEATORIAMENTE>"
```

Na configuração do .NET, `__` representa `:`; por exemplo, `ApiKey__Value` preenche `ApiKey:Value`. A aplicação recusa iniciar se a API key estiver ausente, vazia ou contiver apenas espaços. Nunca coloque credenciais em `appsettings*.json`, documentação, arquivos `.http`, código ou logs.

### Bootstrap inicial do administrador

O bootstrap não é um endpoint. Ele é executado durante a inicialização da aplicação e vem desabilitado quando `BootstrapAdmin:Enabled` está ausente ou é `false`. As roles `Admin`, `Barbeiro` e `Cliente` continuam sendo garantidas independentemente do bootstrap.

Para a criação inicial, configure temporariamente estas variáveis no ambiente seguro de execução:

```powershell
$env:BootstrapAdmin__Enabled = "true"
$env:BootstrapAdmin__NomeCompleto = "<NOME_DO_ADMINISTRADOR_INICIAL>"
$env:BootstrapAdmin__Email = "<EMAIL_PRIVADO_DO_ADMINISTRADOR_INICIAL>"
$env:BootstrapAdmin__Password = "<SENHA_FORTE_GERADA_FORA_DO_REPOSITORIO>"
```

Inicie a aplicação uma vez, confirme por um canal seguro que a conta foi criada com a role `Admin` e então desabilite o bootstrap e remova o segredo do ambiente:

```powershell
$env:BootstrapAdmin__Enabled = "false"
Remove-Item Env:BootstrapAdmin__Password -ErrorAction SilentlyContinue
```

Remova também `BootstrapAdmin__NomeCompleto` e `BootstrapAdmin__Email` quando não forem mais necessários. Em produção, retire esses valores do mecanismo de deploy/cofre, não apenas da sessão local.

O bootstrap usa fail-fast:

- `Enabled` precisa ser booleano; quando for `true`, nome, email e senha precisam estar preenchidos. Campo inválido ou ausente impede a inicialização sem imprimir seu valor.
- Falha ao criar uma role ou rejeição do Identity impede a inicialização e expõe somente códigos de erro, não senha ou descrições potencialmente sensíveis.
- Se o email configurado já pertencer a uma conta sem a role `Admin`, a aplicação falha e **não promove** essa conta automaticamente. A situação deve ser investigada manualmente.
- Se outra instância estiver criando o mesmo admin, a instância perdedora consulta o estado até 10 vezes, com nove esperas de 100 ms (aproximadamente 900 ms). Ela só continua quando a conta concorrente já possui a role `Admin`; nunca atribui a role por conta própria.
- Se a criação do usuário funcionar, mas a associação à role falhar, o bootstrap tenta apagar o usuário como compensação. Se o cleanup retornar falha ou o provedor lançar uma exception, a inicialização falha e o banco pode exigir inspeção e correção manual antes de uma nova tentativa.

Não mantenha o bootstrap habilitado como mecanismo permanente de administração. Criação, recuperação ou promoção posterior de administradores exige um fluxo operacional próprio e auditável.

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

No Swagger, use **Authorize** para informar `X-API-Key`; em operações autenticadas, informe também o Bearer token. O Swagger só é publicado no ambiente `Development`.

---

## 🧪 Testes

Há um projeto real de integração, `BarberApp.IntegrationTests`. Ele sobe a API com `WebApplicationFactory`, usa um banco EF Core InMemory isolado e valida autenticação por API key, compatibilidade JWT, casos de borda, contrato Swagger/OpenAPI, ausência de segredos na configuração versionada e o bootstrap administrativo.

```powershell
# Toda a solução; confira no resumo quantos testes foram descobertos e executados.
dotnet test BarberApp.slnx --no-restore

# Saída detalhada
dotnet test BarberApp.slnx --logger "console;verbosity=normal"

# Apenas integração
dotnet test .\BarberApp.IntegrationTests\BarberApp.IntegrationTests.csproj --no-restore
```

Ainda não existe projeto de testes unitários. O banco InMemory não substitui testes contra PostgreSQL para migrations e comportamentos específicos do provedor.

O desenvolvimento segue TDD para tudo que for criado ou modificado: primeiro um teste falhando pelo motivo correto (RED), depois a implementação mínima (GREEN) e, por fim, refatoração com a suíte sempre verde. Quando houver etapa de revisão humana, os testes devem ser aprovados antes da implementação.

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

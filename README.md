# NetLearn Battle — Versão C#

Migração do projeto NetLearn Battle de Python/Flask para C# com ASP.NET Core Razor Pages.

## Como executar

Requisitos: [.NET 8.0 SDK](https://dotnet.microsoft.com/download)

### Web

```powershell
dotnet run
```

Abrir `http://localhost:5002` no navegador.

### TCP Server

```powershell
dotnet run -- tcp-server --host 127.0.0.1 --port 5001
```

### TCP Client

```powershell
dotnet run -- tcp-client --host 127.0.0.1 --port 5001
```

### Build

```powershell
dotnet build
```

## Estrutura do projeto

```
├── Models/             # Modelos (User, Question, Attempt, etc.)
├── Services/           # Lógica (JsonService, AuthService, ScoreService, IpService, AclService, StatsService)
├── Data/               # Ficheiros JSON
│   ├── acls.json
│   ├── questions.json
│   └── examples/       # Modelos de dados (versionados)
├── Pages/              # Razor Pages
├── wwwroot/css/        # Estilos
├── Network/            # Demonstração TCP (TcpServer, TcpClientDemo, TcpMessage)
├── Program.cs          # Ponto de entrada
├── NetLearnBattle.CSharp.csproj
├── README.md
├── .gitignore
└── Tests/              # Testes
    ├── *Tests.cs           # Testes xUnit isolados
    ├── TestHelpers.cs      # Criação de dados temporários
    ├── NetLearnBattle.CSharp.Tests.csproj
    └── Robot/              # Testes funcionais Robot Framework
        ├── resources/
        │   └── csharp_common.resource
        ├── *.robot
        └── results/
```

## Ficheiros de dados

- `Data/questions.json` — perguntas de exemplo para os níveis 1 a 4
- `Data/acls.json` — cenários de ACL para o nível 5
- `Data/examples/` — modelos da estrutura esperada dos JSONs locais
- `Data/users.json`, `Data/scores.json`, `Data/attempts.json` — dados locais gerados pela aplicação, ignorados pelo Git

## Funcionalidades implementadas

### Fase 1 — Autenticação e base

- Registo de conta com hash SHA-256 + salt
- Login e logout com sessão
- Dashboard com score atual
- Persistência em JSON

### Fase 2 — Jogo web com sessão de 5 perguntas

- Escolha de nível (1 a 5)
- Sessão de jogo com 5 perguntas usando Queue FIFO
- A primeira pergunta inserida na Queue é a primeira apresentada ao aluno
- Feedback imediato (correto/errado) com pontos
- Resumo da sessão com total de certas, erradas e pontos
- Tentativas guardadas em `Data/attempts.json`
- Histórico de tentativas por utilizador
- Pontuação por nível (nível 1: +10/-5, nível 2: +20/-10, etc.)

### Fase 3 — Motores reais de IPv4, IPv6 e ACL (concluída)

- **IpService** — motor de IPv4 e IPv6 com geração dinâmica de perguntas
- **AclService** — motor de ACL com avaliação de regras por ordem
- Perguntas geradas dinamicamente, não fixas
- Níveis 1 a 5 com pontuação real
- **Correção:** `CalculateIpv4Broadcast` — cast para `(byte)` no cálculo bitwise para evitar resultados incorretos em prefixos não alinhados a 8 bits (ex: /25, /26, /27, /21, /22, /23)

## Níveis de jogo

| Nível | Tópico | Pontuação |
|-------|--------|-----------|
| 1 | IPv4 básico (/8, /16, /24) | +10 / -5 |
| 2 | Sub-redes IPv4 (/25, /26, /27) | +20 / -10 |
| 3 | Super-redes IPv4 (/21, /22, /23) | +30 / -15 |
| 4 | IPv6 (Network ID, mesmo segmento, sub-redes, conceitos) | +40 / -20 |
| 5 | ACLs (permit/deny, primeira regra, ordenação, ACE e servidor) | +50 / -25 |

## Motores implementados

### IPv4 (IpService)

O motor IPv4 gera perguntas de:

- **Network ID** — calcular o endereço de rede a partir de um IP e prefixo
- **Broadcast** — calcular o endereço de broadcast
- **Mesmo segmento** — determinar se dois IPs estão na mesma rede

Usa `System.Net.IPAddress` e operações bitwise para calcular máscaras, network IDs e broadcasts.

Redes privadas usadas: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16.

### IPv6 (IpService)

O motor IPv6 gera perguntas de:

- **Network ID IPv6** — calcular o prefixo de rede
- **Mesmo segmento IPv6** — comparar dois endereços IPv6
- **Sub-redes IPv6** — calcular sub-rede com prefixo maior
- **Conceito de broadcast** — pergunta conceptual sobre broadcast em IPv6

**Nota importante:** IPv6 não tem broadcast tradicional como IPv4. O projeto trata esse caso como conceito, explicando que "IPv6 não usa broadcast tradicional".

### ACL (AclService)

O motor ACL:

- Percorre as regras por ordem
- Aplica a primeira regra compatível (first-match)
- Se nenhuma regra combinar, devolve "deny" (comportamento padrão)
- Protocolo "any" ou "ip" combina com qualquer protocolo IP
- Source/Destination "any" combina com qualquer IP
- Suporta notação CIDR para IPs (ex: 192.168.1.0/24)

O nível 5 alterna cinco tipos de perguntas: `permit/deny`, primeira regra
compatível, ordenação de regras, ACE em falta e ACL para servidor. Uma sessão
de cinco perguntas apresenta os cinco tipos, o que facilita a demonstração
académica.

### Fase 4 — Estatísticas e ranking (concluída)

- **StatsService** — serviço de estatísticas do aluno e do professor
- **Página Stats** — estatísticas do aluno autenticado:
  - Score atual, total de perguntas, certas, erradas, taxa de acerto
  - Taxa por nível e por tópico
  - Média, mediana e moda do tempo de resposta
  - Tópico onde mais falha (mínimo 5 tentativas)
  - Evolução do score por sessão
- **Página Ranking** — Top 5 público (ordenado por score decrescente)
- **Página Teacher** — estatísticas globais públicas:
  - Totais globais, taxa global
  - Taxa por nível e por tópico
  - Distribuição de scores (quartis: min, Q1, mediana, Q3, max)
  - Tentativas recentes (últimas 20)
  - Ranking Top 5
  - Nota académica sobre a área ser pública
- **Dashboard** atualizado com links para Estatísticas, Ranking e Professor
- Robusto com JSON vazio — mostra mensagens amigáveis
- Tempo de resposta calculado no servidor entre a apresentação e a resposta
- Evolução de score agregada por sessão: nível, perguntas, certas, erradas,
  pontos da sessão e score final

### Testes Robot Framework da versão C#

Testes funcionais com Robot Framework + Selenium que validam a aplicação web C# em cenários reais:

- `Tests/Robot/web_csharp_e2e.robot` — fluxo completo: registo, login, jogar, histórico, estatísticas, ranking, teacher, logout
- `Tests/Robot/web_csharp_session.robot` — sessão de 5 perguntas com resumo
- `Tests/Robot/web_csharp_invalids.robot` — páginas protegidas, login errado, páginas públicas
- `Tests/Robot/web_csharp_persistence.robot` — dados mantêm-se entre sessões

Os testes:
- Usam porta **5012** (não conflitua com a app normal na porta 5002)
- Iniciam a aplicação C# automaticamente com `dotnet run --urls http://127.0.0.1:5012`
- Usam `resources/csharp_common.resource` com keywords partilhadas
- Guardam logs em `Tests/Robot/results/`

Pré-requisitos:
- Python 3 com Robot Framework e SeleniumLibrary instalados
- ChromeDriver no PATH

```powershell
pip install robotframework selenium robotframework-seleniumlibrary
```

Executar todos os testes:
```powershell
robot --outputdir Tests/Robot/results Tests/Robot
```

### Fase 6 — Testes unitários (concluída)

- Projeto de testes: `Tests/` (xUnit)
- Framework: **xUnit** com `Microsoft.NET.Test.Sdk`
- 100 testes distribuídos por 8 ficheiros, cobrindo todos os serviços:

| Ficheiro | Testes | Cobertura |
|----------|--------|-----------|
| `JsonServiceTests.cs` | 6 | Load/Save, ficheiro inexistente, ficheiro vazio, round-trip |
| `AuthServiceTests.cs` | 9 | Registo, login, hash, salt, duplicados |
| `ScoreServiceTests.cs` | 8 | Pontuação, ranking Top 5, sem dados sensíveis |
| `GameServiceTests.cs` | 11 | Sessão 5 perguntas, Queue, níveis, pontos, tentativas |
| `IpServiceTests.cs` | 25 | IPv4 Network ID, Broadcast, SameNetwork, geração; IPv6 |
| `AclServiceTests.cs` | 13 | RuleMatches, EvaluateAcl, geração 5 tipos |
| `StatsServiceTests.cs` | 13 | Aluno, professor, quartis, vazio, tempos |
| `TcpHandlerTests.cs` | 13 | Mensagens TCP, autenticação, resposta repetida e erros |

Isolamento:
- Todos os testes usam pastas temporárias (`Path.GetTempPath`)
- `JsonService` aceita `basePath` opcional para testes sem `IWebHostEnvironment`
- Nenhum teste altera `Data/*.json` real

Executar:
```powershell
dotnet test
```
ou:
```powershell
dotnet test Tests/NetLearnBattle.CSharp.Tests.csproj
```

### Fase 5 — Demonstração TCP cliente-servidor (concluída)

- Servidor TCP (`Network/TcpServer.cs`) — aceita um cliente de cada vez e responde a mensagens JSON:
  - **AUTH_REQUEST** → AUTH_RESPONSE (autenticação do cliente)
  - **QUESTION_REQUEST** → QUESTION_PUSH (devolve pergunta sem CorrectIndex)
  - **ANSWER_SUBMIT** → ANSWER_RESULT (corrige resposta, atualiza score)
  - **SCORE_UPDATE** → SCORE_UPDATE (score atual do cliente)
  - **RANKING_REQUEST** → RANKING_RESPONSE (top 5)
  - **STATS_REQUEST** → STATS_RESPONSE (estatísticas do cliente)
  - **END_SESSION** → END_SESSION (fim da sessão)
  - **ERROR** → mensagem de erro genérico
- Cliente TCP (`Network/TcpClientDemo.cs`) — demonstrador interativo no terminal com fluxo completo: autenticação → pergunta → resposta → score → ranking → estatísticas → fim
- Tipos de mensagem (`Network/TcpMessage.cs`) — modelo com `Type` e suporte para campos extra via `JsonExtensionData`
- Suporta três modos de execução:
  ```powershell
  dotnet run                          # Aplicação web (porta 5002)
  dotnet run -- tcp-server --host 127.0.0.1 --port 5001
  dotnet run -- tcp-client --host 127.0.0.1 --port 5001
  ```
- O servidor TCP reutiliza o WebApplication builder para aceder aos serviços configurados (JsonService com ContentRootPath correto), evitando um DummyEnvironment frágil
- **Nota:** O servidor TCP não expõe o CorrectIndex da pergunta, mantendo a integridade académica do jogo

## Páginas da aplicação

| Rota | Página | Autenticação |
|------|--------|-------------|
| `/` | Home | Pública |
| `/Register` | Registo | Pública |
| `/Login` | Login | Pública |
| `/Dashboard` | Dashboard do aluno | Requer login |
| `/Play` | Jogo (escolher nível e responder) | Requer login |
| `/Result` | Resultado da pergunta | Requer login |
| `/Summary` | Resumo da sessão | Requer login |
| `/History` | Histórico de tentativas | Requer login |
| `/Stats` | Estatísticas do aluno | Requer login |
| `/Ranking` | Ranking Top 5 | Pública |
| `/Teacher` | Área do professor (estatísticas globais) | Pública |

## Limitações académicas

- O TCP é uma demonstração simples e atende um cliente de cada vez.
- A persistência usa JSON; uma base de dados seria mais adequada em produção.
- O hash com salt é adequado para o âmbito académico, não para segurança profissional.
- A área do professor é pública nesta versão académica.

## Migração

Esta versão replica a lógica do projeto original em Python/Flask, mantendo a mesma estrutura de dados JSON e o mesmo fluxo de autenticação, mas utilizando ASP.NET Core Razor Pages e C#.

# NetLearn Battle — C# / ASP.NET Core

Projeto académico de Redes e Algoritmos feito em **C#**, com **ASP.NET Core Razor Pages** e persistência em **JSON**.

O NetLearn Battle é um jogo educativo. O aluno cria conta, faz login, escolhe um nível, responde perguntas de redes e acompanha score, histórico, estatísticas e ranking.

## 1. O que o projeto faz

- Registo, login e logout.
- Dashboard do aluno.
- Jogo com sessão de 5 perguntas.
- Perguntas de IPv4, IPv6 e ACL.
- Feedback imediato após cada resposta.
- Score por nível.
- Histórico de tentativas.
- Estatísticas do aluno.
- Ranking Top 5.
- Área do professor com estatísticas globais.
- Demonstração TCP cliente-servidor.
- Testes xUnit e Robot Framework.
- RobotMCP opcional para validação.
- Comando seguro para limpar dados locais.

## 2. Tecnologias usadas

- **C# / .NET 8**
- **ASP.NET Core Razor Pages**
- **JSON** para persistência
- **xUnit** para testes unitários
- **Robot Framework + Selenium** para testes no navegador
- **TCP sockets** para demonstração cliente-servidor

## 3. Como executar

### Aplicação web

```powershell
dotnet run
```

Abrir no navegador:

```text
http://localhost:5002
```

### Build

```powershell
dotnet build
```

### Testes xUnit

```powershell
dotnet test
```

### Testes Robot

```powershell
robot --outputdir Tests/Robot/results Tests/Robot
```

### TCP Server

```powershell
dotnet run -- tcp-server --host 127.0.0.1 --port 5001
```

### TCP Client

```powershell
dotnet run -- tcp-client --host 127.0.0.1 --port 5001
```

### Reset de dados locais

```powershell
dotnet run -- reset-data
```

Este comando apaga apenas dados locais gerados pela aplicação. Não apaga perguntas, ACLs nem exemplos.

## 4. Estrutura do projeto

```text
Models/        Dados do sistema: User, Question, Attempt, GameSession
Services/      Lógica principal: jogo, login, JSON, score, IP, ACL, stats
Pages/         Páginas Razor da aplicação web
Network/       TCP Server, TCP Client e mensagens JSON
Data/          Ficheiros JSON
Tests/         Testes xUnit e Robot Framework
tools/         RobotMCP opcional
wwwroot/       CSS e ficheiros estáticos
Program.cs     Ponto de entrada da aplicação
```

## 5. Como funciona o jogo

1. O aluno faz login.
2. Escolhe um nível.
3. O sistema cria uma sessão com 5 perguntas.
4. As perguntas ficam numa `Queue`, ou seja, uma fila FIFO.
5. O aluno responde uma pergunta.
6. O sistema corrige no servidor.
7. O score é atualizado.
8. A tentativa é guardada em JSON.
9. No fim aparece o resumo da sessão.

O `CorrectIndex` é usado internamente para corrigir a pergunta e não deve aparecer antes da resposta.

## 6. Níveis

| Nível | Tema | Pontuação |
|---|---|---|
| 1 | IPv4 básico: /8, /16, /24 | +10 / -5 |
| 2 | Sub-redes IPv4: /25, /26, /27 | +20 / -10 |
| 3 | Super-redes IPv4: /21, /22, /23 | +30 / -15 |
| 4 | IPv6: rede, segmento, sub-redes e conceito de broadcast | +40 / -20 |
| 5 | ACL: permit/deny, first match, ordem, ACE e servidor | +50 / -25 |

## 7. JSON

O projeto não usa base de dados. A persistência é feita com ficheiros JSON.

Ficheiros fixos que ficam no projeto:

- `Data/questions.json`
- `Data/acls.json`
- `Data/examples/`

Ficheiros locais gerados durante uso, testes ou TCP:

- `Data/users.json`
- `Data/scores.json`
- `Data/attempts.json`
- `Data/sessions.json`
- `Data/*.tmp`

Os ficheiros locais estão no `.gitignore` e não devem ir para o GitHub.

## 8. Autenticação

O sistema não guarda passwords em texto simples.

No registo:

- cria um `salt`;
- calcula hash SHA-256;
- guarda apenas `username`, `salt` e `hash`.

No login:

- recalcula o hash da password digitada;
- compara com o hash guardado.

## 9. Estatísticas e ranking

O aluno pode ver:

- total de perguntas;
- certas e erradas;
- taxa de acerto;
- taxa por nível;
- taxa por tópico;
- média, mediana e moda do tempo;
- evolução do score por sessão.

O professor pode ver:

- ranking Top 5;
- estatísticas globais;
- taxa por nível;
- taxa por tópico;
- quartis dos scores;
- tentativas recentes.

A página Teacher é pública nesta versão académica para facilitar a demonstração.

## 10. TCP

O TCP é uma demonstração simples cliente-servidor.

Mensagens principais:

- `AUTH_REQUEST`
- `AUTH_RESPONSE`
- `QUESTION_REQUEST`
- `QUESTION_PUSH`
- `ANSWER_SUBMIT`
- `ANSWER_RESULT`
- `SCORE_UPDATE`
- `RANKING_REQUEST`
- `RANKING_RESPONSE`
- `STATS_REQUEST`
- `STATS_RESPONSE`
- `END_SESSION`
- `ERROR`

O servidor TCP reutiliza serviços reais do projeto. Por isso pode gerar scores e tentativas locais.

## 11. Testes

O projeto tem:

- **106 testes xUnit** para serviços, jogo, IPv4, IPv6, ACL, estatísticas, TCP e reset.
- **10 testes Robot** para fluxos reais no navegador.

Os testes Robot cobrem:

- registo;
- login;
- páginas protegidas;
- sessão de 5 perguntas;
- histórico;
- estatísticas;
- ranking;
- área Teacher;
- persistência.

Pré-requisitos Robot:

- Python 3;
- Robot Framework;
- SeleniumLibrary;
- Google Chrome instalado;
- Selenium Manager ou ChromeDriver compatível.

Instalação:

```powershell
pip install robotframework selenium robotframework-seleniumlibrary
```

## 12. RobotMCP opcional

O RobotMCP é uma ferramenta auxiliar. Ele executa build, xUnit, Robot e gera relatório.

```powershell
python tools/robotmcp/robotmcp.py
```

Também existe exploração segura com Selenium:

```powershell
python tools/robotmcp/robotmcp.py --explore
```

ou:

```powershell
python tools/robotmcp/robotmcp.py --explorer
```

O RobotMCP não faz parte da lógica principal da aplicação.

## 13. Manual do código

O ficheiro `MANUAL_DO_CODIGO.md` explica o projeto em linguagem simples.

Os comentários no código usam referências como:

```csharp
// [M15] Corrige a resposta, atualiza score e guarda a tentativa.
```

Essas referências apontam para secções do manual e ajudam na defesa oral.

## 14. Limitações assumidas

- JSON é adequado para o projeto académico, mas uma base de dados seria melhor em produção.
- TCP é demonstrativo e simples.
- A área Teacher é pública nesta versão.
- Não há perfis avançados de permissões.
- A interface é simples e minimalista.

## 15. Como explicar em 1 minuto

O NetLearn Battle é uma aplicação educativa em C# para treinar IPv4, IPv6 e ACLs. O aluno faz login, escolhe um nível e responde uma sessão de 5 perguntas organizadas numa Queue. O sistema corrige no servidor, atualiza score e guarda tentativas em JSON. Depois o aluno pode consultar histórico, estatísticas e ranking. O projeto também tem área Teacher, demonstração TCP, testes xUnit, testes Robot e reset seguro de dados locais.

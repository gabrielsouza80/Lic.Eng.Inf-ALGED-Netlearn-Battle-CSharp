# Manual do Código — NetLearn Battle C#

Este manual explica o projeto em linguagem simples. Os comentários no código usam
referências como `[M11]`, `[M15]` e `[M34]` para apontar para estas secções.

## [M01] Objetivo do projeto

O NetLearn Battle é uma aplicação educativa sobre redes. O aluno cria conta,
faz login, escolhe um nível, responde perguntas e acumula score.

## [M02] Estrutura geral

A raiz contém `Models/`, `Services/`, `Pages/`, `Network/`, `Data/`, `Tests/`,
`wwwroot/`, `Program.cs` e os ficheiros do projeto .NET.

## [M03] Program.cs

`Program.cs` é o ponto de entrada. Ele escolhe entre modo web, servidor TCP e
cliente TCP, regista serviços, ativa sessão e inicia Razor Pages.

## [M04] ASP.NET Core Razor Pages

As páginas ficam em `Pages/`. Cada `.cshtml` mostra HTML e cada `.cshtml.cs`
contém a lógica da página.

## [M05] C# básico usado no projeto

O projeto usa classes, propriedades, listas, dicionários, filas `Queue<T>`,
serviços e injeção de dependência.

## [M06] Model User

`User` representa um utilizador. Guarda `Username`, `Salt` e `Hash`, nunca a
password em texto simples.

## [M07] Model Question

`Question` representa uma pergunta. `CorrectIndex` indica a resposta correta,
mas deve ficar interno e não aparecer antes da resposta.

## [M08] Model Attempt

`Attempt` guarda uma tentativa do aluno: pergunta, resposta escolhida, resposta
correta, pontos, sessão e tempo.

## [M09] Model GameSession

`GameSession` guarda uma sessão de jogo com 5 perguntas. Usa `Queue<Question>`
para apresentar perguntas em ordem FIFO.

## [M10] Pasta Data

`Data/` guarda JSONs. `questions.json` e `acls.json` são versionados. JSONs reais
como `users.json`, `scores.json` e `attempts.json` ficam ignorados pelo Git.

## [M11] JsonService

`JsonService` lê e grava JSON. Se o ficheiro não existir, estiver vazio ou estiver
malformado, devolve uma estrutura vazia para evitar erro 500.

## [M12] AuthService

`AuthService` faz registo e login. No registo cria um salt aleatório e guarda
apenas hash SHA-256 com salt.

## [M13] ScoreService

`ScoreService` lê e atualiza pontuações. O ranking mostra apenas username e score,
sem dados sensíveis.

## [M14] GameSessionStore

`GameSessionStore` guarda sessões temporárias em memória. Cada sessão tem um dono
e só deve ser usada pelo respetivo utilizador.

## [M15] GameService

`GameService` cria sessões, gera perguntas, corrige respostas, valida
`selectedIndex`, atualiza score e grava tentativas.

## [M16] IpService

`IpService` gera e corrige perguntas IPv4/IPv6. IPv4 inclui Network ID, Broadcast
e mesma rede. IPv6 não usa broadcast tradicional.

## [M17] AclService

`AclService` avalia ACLs. As regras são percorridas por ordem e aplica-se a
primeira compatível. Se nenhuma regra combinar, o padrão é `deny`.

## [M18] StatsService

`StatsService` calcula estatísticas do aluno e do professor: totais, taxas,
média, mediana, moda, quartis e evolução por sessão.

## [M19] Sessão web

A sessão do ASP.NET guarda o utilizador autenticado e a sessão de jogo ativa.

## [M20] Fluxo das páginas

O fluxo principal é: Home → Register/Login → Dashboard → Play → Result → Summary
→ History/Stats/Ranking.

## [M21] Register

`Register` cria utilizador chamando `AuthService.Register`.

## [M22] Login

`Login` valida credenciais com `AuthService.Login` e guarda `Username` na sessão.

## [M23] Logout

`Logout` limpa a sessão para terminar a autenticação.

## [M24] Dashboard

`Dashboard` exige login e mostra o score atual do aluno.

## [M25] Play

`Play` permite escolher nível, iniciar sessão e submeter respostas. Rejeita nível
ou `selectedIndex` inválido.

## [M26] Result

`Result` mostra feedback após a submissão: certo/errado, resposta correta, pontos
e tempo.

## [M27] Summary

`Summary` mostra resumo da sessão: total, certas, erradas, pontos e score final.

## [M28] History

`History` mostra apenas tentativas do utilizador autenticado.

## [M29] Stats

`Stats` mostra estatísticas individuais do aluno autenticado.

## [M30] Ranking

`Ranking` é público e mostra Top 5, sem hash, salt ou password.

## [M31] Teacher

`Teacher` é público nesta versão académica e mostra estatísticas globais.

## [M32] wwwroot/css

`wwwroot/css/site.css` contém estilos visuais simples. Não contém lógica do jogo.

## [M33] TcpMessage

`TcpMessage` representa mensagens JSON usadas na comunicação TCP.

## [M34] TcpServer

`TcpServer` recebe mensagens como `AUTH_REQUEST`, `QUESTION_REQUEST`,
`ANSWER_SUBMIT`, `SCORE_UPDATE`, `RANKING_REQUEST`, `STATS_REQUEST` e
`END_SESSION`.

## [M35] TcpClientDemo

`TcpClientDemo` é um cliente de terminal para demonstrar comunicação TCP.

## [M36] TCP e CorrectIndex

O servidor TCP nunca envia `CorrectIndex` em `QUESTION_PUSH`. A resposta correta
só aparece depois da submissão.

## [M37] Testes xUnit

Os testes em `Tests/*.cs` validam serviços, IPv4, IPv6, ACL, estatísticas, jogo,
autenticação, JSON e TCP.

## [M38] Testes Robot

Os testes Robot validam fluxos reais no browser: E2E, sessão de 5 perguntas,
cenários inválidos e persistência.

## [M39] RobotMCP

`tools/robotmcp/` é opcional. Executa build, xUnit, Robot e pode explorar o site
com Selenium para gerar relatórios.

## [M40] JSON

JSON é usado como persistência académica. É simples de ler, editar e explicar.

## [M41] Hash

Hash transforma a password em texto irreversível para comparação no login.

## [M42] Salt

Salt é um valor aleatório adicionado à password antes do hash.

## [M43] Score

Score é a pontuação acumulada. Acertos somam pontos e erros subtraem pontos.

## [M44] Queue

Queue é fila FIFO: o primeiro item a entrar é o primeiro a sair. No jogo organiza
as perguntas.

## [M45] Stack

Stack é pilha LIFO. O conceito é explicado no projeto original; nesta versão C#
as tentativas ficam ligadas à sessão e persistidas em JSON.

## [M46] IPv4

IPv4 usa endereços como `192.168.1.10`. O projeto calcula rede, broadcast e se
dois IPs estão no mesmo segmento.

## [M47] Network ID

Network ID identifica a rede de um IP com prefixo.

## [M48] Broadcast

Broadcast IPv4 é normalmente o último endereço da rede.

## [M49] Same Network

Same Network verifica se dois IPs pertencem à mesma rede.

## [M50] IPv6

IPv6 usa endereços maiores. Não tem broadcast tradicional como IPv4.

## [M51] Sub-redes

Sub-redes dividem uma rede em redes menores.

## [M52] Super-redes

Super-redes agrupam redes menores numa rede maior.

## [M53] ACL

ACL é uma lista de regras que permite ou bloqueia tráfego.

## [M54] First match

Em ACL, a primeira regra compatível decide o resultado.

## [M55] Deny padrão

Se nenhuma regra ACL combinar, o resultado padrão é negar.

## [M56] Fluxo completo do jogo

Login → Play → cria sessão → Queue com 5 perguntas → resposta → Result → próxima
pergunta → Summary → History/Stats.

## [M57] Como executar

Use `dotnet build`, `dotnet test`, `dotnet run` e `robot --outputdir
Tests/Robot/results Tests/Robot`.

## [M58] Ordem para estudar o código

Começar por `Program.cs`, depois `Models/`, `Services/`, `Pages/`, `Network/`,
`Tests/` e por fim `tools/robotmcp/`.

## [M59] Segurança académica

O projeto protege password, sessão, páginas privadas, JSON malformado e evita
expor `CorrectIndex` antes da resposta.

## [M60] Limitações

JSON é suficiente para o projeto académico, mas uma base de dados seria melhor em
produção. TCP é demonstrativo e a área Teacher é pública nesta versão.

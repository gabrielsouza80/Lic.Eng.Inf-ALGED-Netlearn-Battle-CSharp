# Manual do Código — NetLearn Battle C#

Manual de estudo e defesa oral do projeto **NetLearn Battle — C# / ASP.NET Core Razor Pages**.

Os comentários no código usam referências como `[M03]`, `[M08]`, `[M15]`, `[M34]` e `[M65]`. Cada referência aponta para uma explicação deste manual.

## [M01] Objetivo do projeto

O NetLearn Battle é uma aplicação educativa de Redes de Computadores. O aluno cria conta, faz login, escolhe um nível, responde perguntas de IPv4, IPv6 e ACL, ganha ou perde pontos e consulta histórico, ranking e estatísticas.

Forma curta para defesa: “É um jogo educativo para treinar redes, com persistência em JSON, testes automáticos e uma demonstração TCP cliente-servidor.”

## [M02] Estrutura geral

A raiz do projeto contém `Models/`, `Services/`, `Pages/`, `Network/`, `Data/`, `Tests/`, `tools/`, `wwwroot/`, `Program.cs` e ficheiros `.csproj`/`.sln`.

Cada pasta tem uma responsabilidade clara. Isto facilita explicar o projeto e encontrar código durante a defesa.

## [M03] Program.cs

`Program.cs` é o ponto de entrada. Ele decide o modo de execução:

- `dotnet run`: inicia o site web;
- `dotnet run -- tcp-server`: inicia o servidor TCP;
- `dotnet run -- tcp-client`: inicia o cliente TCP;
- `dotnet run -- reset-data`: limpa dados locais.

Também regista os serviços, ativa sessão, configura Razor Pages e define a porta web padrão `http://localhost:5002`.

## [M04] ASP.NET Core Razor Pages

Razor Pages separa a interface e a lógica:

- `.cshtml`: HTML e parte visual;
- `.cshtml.cs`: lógica em C# da página.

Exemplo: `Login.cshtml` mostra o formulário; `Login.cshtml.cs` chama `AuthService` para validar o login.

## [M05] C# básico usado no projeto

O projeto usa classes, propriedades, listas, dicionários, `Queue<T>`, serviços, injeção de dependência e métodos assíncronos no TCP.

Ideia simples: os Models representam dados, os Services fazem a lógica, as Pages mostram a interface.

## [M06] Model User

`User` representa um utilizador. Guarda `Username`, `Salt` e `Hash`.

A password real nunca é guardada. Isto é importante para defender segurança académica básica.

## [M07] Model Question

`Question` representa uma pergunta do jogo. Tem texto, opções, nível, tópico, pontos e `CorrectIndex`.

`CorrectIndex` é interno: serve para corrigir, mas não deve aparecer no HTML nem no TCP antes da resposta.

## [M08] Model Attempt

`Attempt` representa uma tentativa do aluno. Cada resposta gera um Attempt com:

- pergunta;
- resposta escolhida;
- resposta correta;
- pontos;
- tempo de resposta;
- sessão;
- score após a tentativa.

O histórico e as estatísticas dependem destes dados.

## [M09] Model GameSession

`GameSession` representa uma sessão de jogo. Guarda utilizador, nível, pergunta atual, tentativas e uma `Queue<Question>`.

A Queue organiza as 5 perguntas em ordem FIFO: primeira a entrar, primeira a sair.

## [M10] Pasta Data

`Data/` guarda os ficheiros JSON.

Ficheiros fixos versionados:

- `questions.json`;
- `acls.json`;
- `examples/`.

Ficheiros locais ignorados:

- `users.json`;
- `scores.json`;
- `attempts.json`;
- `sessions.json`.

## [M11] JsonService

`JsonService` centraliza leitura e escrita de JSON.

Ele trata ficheiro inexistente, vazio ou malformado devolvendo estrutura vazia. Isto evita erro 500 em páginas públicas e facilita testes.

Na gravação usa ficheiro `.tmp` antes de substituir, reduzindo risco de corromper dados.

## [M12] AuthService

`AuthService` faz registo e login.

No registo:

1. valida username/password;
2. cria salt aleatório;
3. calcula hash SHA-256;
4. guarda username, salt e hash.

No login, calcula novamente o hash da password digitada e compara com o hash guardado.

## [M13] ScoreService

`ScoreService` consulta e atualiza scores em `scores.json`.

Se um utilizador ainda não tem score, começa em 0. O ranking mostra apenas username e score, sem hash, salt ou password.

## [M14] GameSessionStore

`GameSessionStore` guarda sessões temporárias em memória.

Cada sessão tem um dono (`Username`). Isto impede que um utilizador responda à sessão de outro.

## [M15] GameService

`GameService` controla o fluxo do jogo:

1. cria sessão;
2. gera 5 perguntas;
3. coloca perguntas numa Queue;
4. corrige respostas;
5. valida `selectedIndex`;
6. atualiza score;
7. guarda tentativa;
8. avança para a próxima pergunta ou termina a sessão.

É um dos serviços principais para explicar na defesa.

## [M16] IpService

`IpService` gera perguntas de IPv4 e IPv6.

IPv4 inclui Network ID, Broadcast e mesmo segmento. IPv6 inclui Network ID, mesmo segmento, sub-redes e conceito de broadcast.

## [M17] AclService

`AclService` avalia ACLs.

Uma ACL é uma lista de regras. O motor percorre as regras por ordem e aplica a primeira compatível. Se nenhuma regra combinar, aplica `deny` padrão.

O nível 5 usa perguntas de permit/deny, primeira regra, ordenação, ACE em falta e ACL para servidor.

## [M18] StatsService

`StatsService` transforma tentativas em estatísticas.

Para o aluno calcula totais, taxa de acerto, tempos, tópico fraco e evolução por sessão.

Para o professor calcula ranking, estatísticas globais, taxa por nível/tópico, quartis e tentativas recentes.

## [M19] Sessão web

A Session do ASP.NET guarda dados temporários do utilizador no servidor.

No projeto guarda o username autenticado e a sessão de jogo ativa.

Sem Session, o site não saberia se o utilizador fez login.

## [M20] Fluxo das páginas

Fluxo principal:

`Index → Register/Login → Dashboard → Play → Result → Summary → History/Stats/Ranking`.

As páginas protegidas redirecionam para Login se não houver utilizador autenticado.

## [M21] Register

`Register` mostra formulário de criação de conta.

No POST chama `AuthService.Register`. A página não calcula hash diretamente; essa responsabilidade fica no serviço.

## [M22] Login

`Login` recebe username/password e chama `AuthService.Login`.

Se as credenciais estiverem corretas, guarda `Username` na Session e redireciona para Dashboard.

## [M23] Logout

`Logout` limpa a Session.

Depois disso o utilizador deixa de estar autenticado e páginas privadas voltam a exigir login.

## [M24] Dashboard

`Dashboard` é a página principal do aluno autenticado.

Mostra username, score atual e ligações para jogar, histórico, estatísticas, ranking e logout.

## [M25] Play

`Play` permite escolher nível, iniciar sessão e submeter resposta.

Pontos importantes:

- valida nível;
- valida `selectedIndex`;
- confirma dono da sessão;
- não mostra `CorrectIndex`;
- envia a resposta para `GameService`.

## [M26] Result

`Result` mostra feedback após a resposta:

- acertou ou errou;
- resposta correta;
- pontos;
- tempo de resposta;
- botão para próxima pergunta ou resumo.

## [M27] Summary

`Summary` aparece no fim da sessão.

Mostra total de perguntas, certas, erradas, pontos da sessão e score final.

## [M28] History

`History` lê `attempts.json`, filtra pelo utilizador autenticado e mostra apenas as tentativas desse aluno.

Serve para rever respostas anteriores.

## [M29] Stats

`Stats` mostra estatísticas individuais do aluno autenticado.

Usa `StatsService` e os dados de tentativas/scores.

## [M30] Ranking

`Ranking` é público e mostra Top 5.

Não expõe password, hash nem salt. Mostra apenas username e score.

## [M31] Teacher

`Teacher` mostra estatísticas globais.

Nesta versão académica é pública para facilitar demonstração. Numa versão real deveria ter perfil/permissão de professor.

## [M32] wwwroot/css

`wwwroot/css/site.css` guarda estilos visuais simples.

Não contém lógica de jogo, autenticação, score ou estatísticas.

## [M33] TcpMessage

`TcpMessage` representa mensagens JSON usadas pelo TCP.

Ajuda a manter o formato de comunicação organizado.

## [M34] TcpServer

`TcpServer` recebe mensagens JSON por TCP e chama os serviços reais do projeto.

Mensagens principais:

- `AUTH_REQUEST`;
- `QUESTION_REQUEST`;
- `ANSWER_SUBMIT`;
- `SCORE_UPDATE`;
- `RANKING_REQUEST`;
- `STATS_REQUEST`;
- `END_SESSION`;
- `ERROR`.

O servidor valida autenticação, não envia `CorrectIndex` antes da resposta e limpa a pergunta ativa depois da submissão para evitar duplicar score.

## [M35] TcpClientDemo

`TcpClientDemo` é um cliente de terminal.

Serve para demonstrar comunicação aluno/servidor: autentica, pede pergunta, responde e consulta dados.

## [M36] TCP e CorrectIndex

No TCP, `QUESTION_PUSH` envia pergunta, opções, nível e tópico.

Não envia `CorrectIndex`. A resposta correta só aparece depois de `ANSWER_SUBMIT`.

## [M37] Testes xUnit

Os testes xUnit validam serviços e regras internas:

- JSON;
- autenticação;
- score;
- jogo;
- IPv4;
- IPv6;
- ACL;
- estatísticas;
- TCP;
- reset de dados.

Eles usam pastas temporárias e não alteram dados reais do projeto.

## [M38] Testes Robot

Os testes Robot usam navegador real.

Validam registo, login, páginas protegidas, sessão de 5 perguntas, histórico, estatísticas, ranking, Teacher e persistência.

## [M39] RobotMCP

RobotMCP é uma ferramenta opcional em `tools/robotmcp/`.

Executa build, xUnit, Robot e pode explorar o site com Selenium. Serve como apoio de validação, não como lógica principal.

## [M40] JSON

JSON foi usado porque é simples, legível e adequado ao contexto académico.

Também permite mostrar facilmente ao professor como os dados ficam guardados.

## [M41] Hash

Hash transforma a password num valor que não deve permitir recuperar a password original.

No projeto usa SHA-256.

## [M42] Salt

Salt é um valor aleatório criado para cada utilizador.

Ele é juntado à password antes do hash para evitar hashes iguais para passwords iguais.

## [M43] Score

Score é a pontuação acumulada.

Cada pergunta define pontos de acerto e erro. O GameService decide o resultado e o ScoreService atualiza o total.

## [M44] Queue

Queue é fila FIFO: primeiro a entrar, primeiro a sair.

No jogo, as 5 perguntas entram na fila e são consumidas uma a uma.

Frase para defesa: “Usei Queue para representar a ordem natural das perguntas da sessão.”

## [M45] Stack

Stack é pilha LIFO: último a entrar, primeiro a sair.

Na versão Python original o conceito era mais visível. Na versão C#, as tentativas ficam ligadas à sessão e persistidas em JSON.

## [M46] IPv4

IPv4 usa endereços como `192.168.1.10`.

O projeto trabalha com redes privadas e prefixos `/8`, `/16`, `/24`, `/25`, `/26`, `/27`, `/21`, `/22` e `/23`.

## [M47] Network ID

Network ID identifica a rede de um IP com determinado prefixo.

Exemplo: `192.168.1.10/24` pertence normalmente à rede `192.168.1.0`.

## [M48] Broadcast

Broadcast IPv4 é o último endereço da rede.

O projeto calcula broadcast com máscara e operações bitwise, funcionando também em prefixos como `/25` e `/26`.

## [M49] Same Network

Same Network compara se dois IPs têm o mesmo Network ID para o mesmo prefixo.

Se tiverem, estão no mesmo segmento.

## [M50] IPv6

IPv6 usa endereços maiores, como `2001:db8::1`.

O projeto trabalha com Network ID, mesmo segmento, sub-redes e pergunta conceptual sobre broadcast.

Ponto importante: IPv6 não usa broadcast tradicional como IPv4.

## [M51] Sub-redes

Sub-redes dividem uma rede em partes menores.

No projeto aparecem em IPv4 e IPv6, com perguntas de escolha múltipla.

## [M52] Super-redes

Super-redes agrupam redes menores.

No projeto aparecem no nível 3 com prefixos `/21`, `/22` e `/23`.

## [M53] ACL

ACL significa Access Control List.

É uma lista de regras que permite ou bloqueia pacotes com base em origem, destino, protocolo, porta e ordem.

## [M54] First match

First match significa que a primeira regra compatível decide o resultado.

As regras seguintes não são avaliadas.

## [M55] Deny padrão

Se nenhuma regra ACL combinar, o resultado padrão é `deny`.

Isto significa que só passa o que foi explicitamente permitido.

## [M56] Fluxo completo do jogo

1. Login.
2. Dashboard.
3. Play.
4. Escolha de nível.
5. GameService cria sessão.
6. Queue recebe 5 perguntas.
7. Aluno responde.
8. GameService corrige.
9. ScoreService atualiza score.
10. Attempt é gravado.
11. Result mostra feedback.
12. Summary mostra resumo.

## [M57] Como executar

Comandos principais:

```powershell
dotnet run
dotnet build
dotnet test
robot --outputdir Tests/Robot/results Tests/Robot
```

## [M58] Ordem para estudar o código

Ordem recomendada:

1. `Program.cs`;
2. `Models/`;
3. `Services/AuthService.cs`;
4. `Services/GameService.cs`;
5. `Services/IpService.cs`;
6. `Services/AclService.cs`;
7. `Pages/`;
8. `Network/`;
9. `Tests/`;
10. `tools/robotmcp/`.

## [M59] Segurança académica

O projeto protege passwords com hash e salt, valida páginas privadas, evita expor `CorrectIndex`, trata JSON inválido e impede resposta inválida de alterar score.

Não é segurança profissional completa, mas é adequada para o contexto académico.

## [M60] Limitações

Limitações assumidas:

- JSON em vez de base de dados;
- TCP demonstrativo;
- Teacher público;
- sem perfis avançados;
- interface simples.

## [M61] Dados locais gerados pela aplicação

A aplicação pode criar:

- `Data/users.json`;
- `Data/scores.json`;
- `Data/attempts.json`;
- `Data/sessions.json`.

Estes ficheiros são locais e não devem ir para o GitHub.

## [M62] Dados fixos que devem ficar no projeto

Devem ficar no projeto:

- `Data/questions.json`;
- `Data/acls.json`;
- `Data/examples/`.

Estes ficheiros fazem parte da entrega.

## [M63] O TCP é demonstração, mas usa lógica real

O TCP é simples, mas chama os mesmos serviços usados pela web.

Por isso pode criar utilizadores, scores e tentativas locais.

## [M64] Dados gerados pelo RobotMCP Explorer

O Explorer cria utilizadores temporários e joga no site para testar fluxos.

Isto pode gerar dados locais, que depois podem ser limpos com `reset-data`.

## [M65] Comando de reset dos dados locais

`dotnet run -- reset-data` remove apenas dados locais:

- users;
- scores;
- attempts;
- sessions;
- ficheiros `.tmp` correspondentes.

## [M66] Quando usar o reset

Usar antes de demonstração limpa, antes de preparar ZIP ou depois de testes Robot/RobotMCP.

## [M67] Segurança do reset

O reset usa lista fechada de ficheiros permitidos.

Não apaga a pasta `Data/`, não usa wildcard perigoso e nunca remove `questions.json`, `acls.json` ou `examples/`.

## [M68] Como explicar isto ao professor

Frase simples:

“Os dados de uso são locais e ignorados pelo Git. O reset limpa utilizadores, scores e tentativas sem apagar perguntas nem ACLs.”

## [M69] Comandos importantes finais

```powershell
dotnet run
dotnet build
dotnet test
dotnet run -- reset-data
robot --outputdir Tests/Robot/results Tests/Robot
python tools/robotmcp/robotmcp.py
python tools/robotmcp/robotmcp.py --explore
```

## [M70] Resumo dos dados

Dados fixos ficam no projeto. Dados reais de uso ficam locais e ignorados. O reset limpa só os dados locais.

Resumo para decorar:

“O projeto junta C#, Razor Pages, JSON, Queue, redes, estatísticas, TCP e testes automáticos numa aplicação educativa simples e defensável.”

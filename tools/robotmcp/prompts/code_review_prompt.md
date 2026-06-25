# Code Review Prompt

Faz uma revisão final do código C# do NetLearn Battle procurando buracos académicos e riscos simples.

Objetivo:
Encontrar apenas problemas reais e relevantes antes da entrega.

Verificar:

* se há dados sensíveis versionados;
* se JSON real está ignorado;
* se CorrectIndex não é enviado ao browser nem ao TCP antes da resposta;
* se password não é guardada em texto simples;
* se hash + salt continuam corretos;
* se pages protegidas exigem login;
* se sessão só pode ser respondida pelo utilizador dono;
* se Result/Summary não quebram sem sessão;
* se JSON vazio ou inexistente não causa erro 500;
* se ranking não expõe dados sensíveis;
* se Teacher está documentado como público;
* se TCP bloqueia resposta repetida;
* se xUnit e Robot continuam passando;
* se README está coerente com o código.

Não procurar perfeição profissional.
Não propor arquitetura nova.
Não reescrever o projeto.
Corrigir apenas:

* bugs reais;
* mensagens erradas;
* documentação incoerente;
* paths quebrados;
* dados sensíveis;
* testes quebrados.

Responder com:

1. Estado geral.
2. Buracos encontrados.
3. Correções pequenas recomendadas.
4. O que não precisa mexer.
5. Se pode entregar.

# RobotMCP opcional

RobotMCP é uma ferramenta auxiliar para validar o projeto NetLearn Battle C#.

Ela não faz parte da aplicação principal e não é necessária para executar o site.
O objetivo é gerar contexto simples para revisão no Codex/OpenCode, usando os testes já existentes.

## O que faz

* confirma se existe o projeto C#;
* executa `dotnet build`;
* executa `dotnet test`;
* executa os testes Robot Framework;
* lê `Tests/Robot/results/output.xml`;
* conta testes Robot passados, falhados e ignorados;
* gera um relatório Markdown com resultado e sugestões de cobertura.

O RobotMCP não corrige código automaticamente.

## Como executar

Na raiz do repositório:

```powershell
python tools/robotmcp/robotmcp.py
```

## Saída

O relatório é gerado em:

```text
tools/robotmcp/output/robotmcp_report.md
```

A pasta `output/` é gerada localmente e fica ignorada pelo Git.

## Modo Explorer

O modo Explorer abre a aplicação com Selenium e faz uma navegação segura.
Ele visita páginas públicas, confirma páginas protegidas sem login, cria um
utilizador temporário, faz login, explora o jogo e gera um relatório extra.

Executar:

```powershell
python tools/robotmcp/robotmcp.py --explore
```

O relatório do explorer é gerado em:

```text
tools/robotmcp/output/robotmcp_explorer_report.md
```

O explorer:

* inicia a app em `http://127.0.0.1:5013`;
* usa Selenium;
* clica apenas em ações seguras;
* ignora links/botões com palavras destrutivas como `delete`, `remove`,
  `reset`, `apagar`, `eliminar` e `limpar`;
* não altera a lógica principal;
* não substitui testes manuais nem Robot;
* serve para encontrar buracos visuais e fluxos não cobertos.

Se Selenium não estiver instalado:

```powershell
pip install selenium
```

## Quando usar

Use antes da entrega ou antes de pedir ao Codex/OpenCode uma revisão final.
O relatório ajuda a identificar rapidamente:

* build quebrado;
* testes xUnit quebrados;
* testes Robot quebrados;
* fluxos principais cobertos;
* fluxos que ainda podem receber testes melhores.

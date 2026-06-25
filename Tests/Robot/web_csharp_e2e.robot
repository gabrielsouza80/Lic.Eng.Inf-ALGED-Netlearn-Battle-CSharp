*** Settings ***
Documentation    Fluxo end-to-end completo da versão C# do NetLearn Battle.
# [M38] Fluxo E2E: registo, login, jogo, páginas principais e logout.
Resource         resources/csharp_common.resource
Suite Setup      Iniciar Ambiente CSharp
Suite Teardown   Terminar Ambiente CSharp
Test Setup       Iniciar Registo No Console
Test Teardown    Finalizar Registo No Console

*** Test Cases ***
Fluxo End To End CSharp
    [Tags]    e2e    valido    navegacao
    Mostrar Passo    Abrir página inicial.
    Go To    ${BASE_URL}/
    Page Should Contain Element    xpath=//h1[normalize-space()='NetLearn Battle']

    # Registo
    Mostrar Passo    Registar utilizador único.
    ${created_at}=    Get Current Date    result_format=%Y%m%d_%H%M%S_%f
    ${username}=    Set Variable    e2e_${created_at}
    Set Suite Variable    ${ACTIVE_USER}    ${username}
    Registrar Utilizador    ${ACTIVE_USER}    ${SESSION_PASSWORD}

    # Login
    Fazer Login    ${ACTIVE_USER}    ${SESSION_PASSWORD}
    Page Should Contain    Olá, ${ACTIVE_USER}

    # Jogar nível 1, uma pergunta
    Iniciar Nível    1
    Responder Pergunta Atual    0
    Page Should Contain Element    xpath=//h1[normalize-space()='Correto!' or normalize-space()='Errado!']

    # Histórico
    Abrir History
    Page Should Contain Element    xpath=//table[contains(@class, 'attempts-table')]//tbody/tr
    Mostrar Validação    Histórico mostra tentativa.

    # Estatísticas
    Abrir Stats
    Page Should Contain    Score Atual
    Mostrar Validação    Estatísticas do aluno apresentadas.

    # Ranking
    Abrir Ranking
    Page Should Contain    Ranking Top 5
    Mostrar Validação    Ranking aberto.

    # Teacher
    Abrir Teacher
    Page Should Contain    Área do Professor
    Mostrar Validação    Área do Professor apresenta dados.

    # Logout
    Fazer Logout
    Page Should Contain    Sessão terminada
    Mostrar Validação    Logout concluído com sucesso.

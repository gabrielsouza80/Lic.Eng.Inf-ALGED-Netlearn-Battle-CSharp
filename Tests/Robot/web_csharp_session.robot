*** Settings ***
Documentation    Teste de sessão completa de 5 perguntas na versão C#.
Resource         resources/csharp_common.resource
Suite Setup      Iniciar Ambiente CSharp
Suite Teardown   Terminar Ambiente CSharp
Test Setup       Iniciar Registo No Console
Test Teardown    Finalizar Registo No Console

*** Test Cases ***
Sessão Completa De 5 Perguntas
    [Tags]    sessao    valido    jogo
    ${created_at}=    Get Current Date    result_format=%Y%m%d_%H%M%S_%f
    ${username}=    Set Variable    sessao_${created_at}
    Set Suite Variable    ${ACTIVE_USER}    ${username}

    Registrar Utilizador    ${ACTIVE_USER}    ${SESSION_PASSWORD}
    Fazer Login    ${ACTIVE_USER}    ${SESSION_PASSWORD}

    # Nível 1 — responder 5 perguntas
    Completar Sessão De 5 Perguntas    1

    # Abrir Resumo
    Ir Para Resumo
    Page Should Contain Element    xpath=//h1[normalize-space()='Resumo da Sessão']
    Page Should Contain    Total
    Page Should Contain    Certas
    Page Should Contain    Erradas
    Page Should Contain    Pontos
    Mostrar Validação    Resumo da sessão apresenta todas as estatísticas.

    # Confirmar histórico
    Abrir History
    Page Should Contain    1
    ${row_count}=    Get Element Count    xpath=//table[contains(@class, 'attempts-table')]//tbody/tr
    Should Be Equal As Integers    ${row_count}    5
    Mostrar Validação    Histórico mostra 5 tentativas.

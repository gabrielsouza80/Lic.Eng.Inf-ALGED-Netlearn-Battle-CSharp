*** Settings ***
Documentation    Teste de persistência de dados entre sessões na versão C#.
Resource         resources/csharp_common.resource
Suite Setup      Iniciar Ambiente CSharp
Suite Teardown   Terminar Ambiente CSharp
Test Setup       Iniciar Registo No Console
Test Teardown    Finalizar Registo No Console

*** Variables ***
${PERSISTENT_USER}      csharp_persistente_teste
${PERSISTENT_PASSWORD}  password_teste

*** Test Cases ***
Dados Persistem Entre Sessões De Login
    [Tags]    persistencia    valido
    Mostrar Passo    Registrar ou garantir existência do utilizador persistente.
    Go To    ${BASE_URL}/Register
    Clear Element Text    id=username
    Input Text    id=username    ${PERSISTENT_USER}
    Clear Element Text    id=password
    Input Password    id=password    ${PERSISTENT_PASSWORD}
    Click Button    Criar Conta
    ${register_ok}=    Run Keyword And Return Status    Page Should Contain    Conta criada com sucesso
    # Se já existir, o registo falha mas o login deve funcionar.
    Run Keyword If    not ${register_ok}    Log    Utilizador já existente, a prosseguir para login.

    # Login e jogar
    Fazer Login    ${PERSISTENT_USER}    ${PERSISTENT_PASSWORD}
    Page Should Contain    Olá, ${PERSISTENT_USER}

    Iniciar Nível    1
    Responder Pergunta Atual    0
    Page Should Contain Element    xpath=//h1[normalize-space()='Correto!' or normalize-space()='Errado!']

    # Logout
    Fazer Logout

    # Segundo login
    Fazer Login    ${PERSISTENT_USER}    ${PERSISTENT_PASSWORD}
    Page Should Contain    Olá, ${PERSISTENT_USER}

    # Histórico deve conter a tentativa anterior
    Abrir History
    Page Should Contain Element    xpath=//table[contains(@class, 'attempts-table')]//tbody/tr
    ${row_count}=    Get Element Count    xpath=//table[contains(@class, 'attempts-table')]//tbody/tr
    Should Be True    ${row_count} >= 1    Histórico devia ter pelo menos 1 tentativa.
    Mostrar Validação    Histórico mantém tentativas entre sessões.

    # Dashboard deve mostrar score > 0 ou 0 (caso tenha errado com -5 e score inicial)
    Abrir Dashboard
    ${score_text}=    Get Text    css=.card-value
    Should Not Be Empty    ${score_text}
    Mostrar Validação    Dashboard mantém score entre sessões.

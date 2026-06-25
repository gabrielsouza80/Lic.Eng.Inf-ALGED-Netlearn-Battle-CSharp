*** Settings ***
Documentation    Testes de autenticação e páginas protegidas na versão C#.
# [M38] Valida login errado e páginas protegidas.
Resource         resources/csharp_common.resource
Suite Setup      Iniciar Ambiente CSharp Com Registo Novo
Suite Teardown   Terminar Ambiente CSharp
Test Setup       Iniciar Registo No Console
Test Teardown    Finalizar Registo No Console

*** Test Cases ***
Dashboard Sem Login Redireciona Para Login
    [Tags]    invalido    auth    redirect
    Mostrar Passo    Tentar aceder ao Dashboard sem sessão.
    Go To    ${BASE_URL}/Dashboard
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Login
    Page Should Contain Element    xpath=//h1[normalize-space()='Entrar']
    Mostrar Validação    Dashboard bloqueado e redirecionado para Login.

Play Sem Login Redireciona Para Login
    [Tags]    invalido    auth    redirect
    Mostrar Passo    Tentar aceder ao Play sem sessão.
    Go To    ${BASE_URL}/Play
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Login
    Page Should Contain Element    xpath=//h1[normalize-space()='Entrar']
    Mostrar Validação    Play bloqueado.

History Sem Login Redireciona Para Login
    [Tags]    invalido    auth    redirect
    Mostrar Passo    Tentar aceder ao Histórico sem sessão.
    Go To    ${BASE_URL}/History
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Login
    Page Should Contain Element    xpath=//h1[normalize-space()='Entrar']
    Mostrar Validação    History bloqueado.

Stats Sem Login Redireciona Para Login
    [Tags]    invalido    auth    redirect
    Mostrar Passo    Tentar aceder às Estatísticas sem sessão.
    Go To    ${BASE_URL}/Stats
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Login
    Page Should Contain Element    xpath=//h1[normalize-space()='Entrar']
    Mostrar Validação    Stats bloqueado.

Login Com Password Errada Mostra Erro
    [Tags]    invalido    auth
    Mostrar Passo    Tentar login com password errada.
    Go To    ${BASE_URL}/Login
    Clear Element Text    id=username
    Input Text    id=username    ${ACTIVE_USER}
    Clear Element Text    id=password
    Input Password    id=password    password_errada
    Click Button    Entrar
    Wait Until Keyword Succeeds    5x    500 milliseconds    Page Should Contain    Utilizador ou palavra-passe incorretos.
    Mostrar Validação    Erro de login apresentado.

Ranking É Público
    [Tags]    invalido    publico
    Mostrar Passo    Abrir Ranking sem sessão.
    Go To    ${BASE_URL}/Ranking
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Ranking
    Page Should Contain Element    xpath=//h1[normalize-space()='Ranking Top 5']
    Mostrar Validação    Ranking acessível sem autenticação.

Teacher É Público
    [Tags]    invalido    publico
    Mostrar Passo    Abrir Teacher sem sessão.
    Go To    ${BASE_URL}/Teacher
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Teacher
    Page Should Contain    Área do Professor
    Page Should Contain    A área do professor é pública
    Mostrar Validação    Teacher acessível sem autenticação com nota académica.

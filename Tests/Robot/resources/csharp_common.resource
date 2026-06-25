*** Settings ***
Documentation    Recursos partilhados pelos testes Robot da versão C#.
Library          SeleniumLibrary
Library          Process
Library          OperatingSystem
Library          DateTime
Library          Collections
Library          String

*** Variables ***
${BASE_URL}              http://127.0.0.1:5012
${SESSION_PASSWORD}      csharp_robot_123
${VISUAL_SPEED}          100 milliseconds
${CSPROJ_DIR}            ${EMPTY}
${ACTIVE_USER}           ${EMPTY}
${CSPROJ_PROCESS}        ${EMPTY}

*** Keywords ***
Iniciar Registo No Console
    Log To Console    \n========== [C# TESTE] ${TEST NAME} ==========

Finalizar Registo No Console
    Log To Console    \n[RESULTADO] ${TEST NAME}: ${TEST STATUS}

Mostrar Passo
    [Arguments]    ${message}
    Log To Console    \n[PASSO] ${message}
    Log    [PASSO] ${message}

Mostrar Validação
    [Arguments]    ${message}
    Log To Console    \n[VALIDAR] ${message}
    Log    [VALIDAR] ${message}

Preparar Caminhos Do Projeto CSharp
    ${project_dir}=    Evaluate    os.path.abspath(os.path.join(r'''${CURDIR}''', '..', '..', '..'))    os
    Set Suite Variable    ${CSPROJ_DIR}    ${project_dir}
    ${robot_dir}=    Evaluate    os.path.abspath(os.path.join(r'''${CURDIR}''', '..'))    os
    ${results_dir}=    Set Variable    ${robot_dir}${/}results
    Set Suite Variable    ${ROBOT_RESULTS_DIR}    ${results_dir}
    Create Directory    ${ROBOT_RESULTS_DIR}

Iniciar Servidor CSharp
    Preparar Caminhos Do Projeto CSharp
    ${output_log}=    Set Variable    ${ROBOT_RESULTS_DIR}${/}csharp-output.log
    ${error_log}=    Set Variable    ${ROBOT_RESULTS_DIR}${/}csharp-error.log
    Run Keyword And Ignore Error    Remove File    ${output_log}
    Run Keyword And Ignore Error    Remove File    ${error_log}
    Directory Should Exist    ${CSPROJ_DIR}
    ${windows_dotnet}=    Set Variable    C:/Program Files/dotnet/dotnet.exe
    ${has_windows_dotnet}=    Run Keyword And Return Status    File Should Exist    ${windows_dotnet}
    IF    ${has_windows_dotnet}
        ${dotnet}=    Set Variable    ${windows_dotnet}
    ELSE
        ${dotnet}=    Set Variable    dotnet
    END
    ${process}=    Start Process    ${dotnet}    run    --urls    http://127.0.0.1:5012
    ...    cwd=${CSPROJ_DIR}    alias=csharp    stdout=${output_log}    stderr=${error_log}
    Set Suite Variable    ${CSPROJ_PROCESS}    ${process}

Terminar Servidor CSharp
    Run Keyword And Ignore Error    Terminate Process    ${CSPROJ_PROCESS}    kill=True
    Run Keyword And Ignore Error    Wait For Process    ${CSPROJ_PROCESS}    timeout=5 seconds

Abrir Navegador
    ${driver_log}=    Set Variable    ${ROBOT_RESULTS_DIR}${/}chromedriver.log
    ${chrome_service}=    Evaluate    selenium.webdriver.chrome.service.Service(log_output=r'''${driver_log}''')    modules=selenium.webdriver.chrome.service
    Open Browser    about:blank    chrome    options=add_argument('--log-level=3');add_experimental_option('excludeSwitches',['enable-logging'])    service=${chrome_service}
    Maximize Browser Window
    Set Selenium Speed    ${VISUAL_SPEED}
    Wait Until Keyword Succeeds    10x    2 seconds    Confirmar Aplicação Ativa

Fechar Navegador
    Close All Browsers

Iniciar Ambiente CSharp
    Iniciar Servidor CSharp
    Abrir Navegador

Iniciar Ambiente CSharp Com Sessão Nova
    Iniciar Servidor CSharp
    Abrir Navegador
    ${created_at}=    Get Current Date    result_format=%Y%m%d_%H%M%S_%f
    ${username}=    Set Variable    csharp_aluno_${created_at}
    Set Suite Variable    ${ACTIVE_USER}    ${username}
    Registrar Utilizador    ${ACTIVE_USER}    ${SESSION_PASSWORD}
    Fazer Login    ${ACTIVE_USER}    ${SESSION_PASSWORD}

Iniciar Ambiente CSharp Com Registo Novo
    Iniciar Servidor CSharp
    Abrir Navegador
    ${created_at}=    Get Current Date    result_format=%Y%m%d_%H%M%S_%f
    ${username}=    Set Variable    csharp_invalido_${created_at}
    Set Suite Variable    ${ACTIVE_USER}    ${username}
    Registrar Utilizador    ${ACTIVE_USER}    ${SESSION_PASSWORD}

Terminar Ambiente CSharp
    Fechar Navegador
    Terminar Servidor CSharp

Confirmar Aplicação Ativa
    Go To    ${BASE_URL}/
    Title Should Be    NetLearn Battle — Início

Registrar Utilizador
    [Arguments]    ${username}    ${password}
    Mostrar Passo    Registar utilizador ${username}.
    Go To    ${BASE_URL}/Register
    Clear Element Text    id=username
    Input Text    id=username    ${username}
    Clear Element Text    id=password
    Input Password    id=password    ${password}
    Click Button    Criar Conta
    Wait Until Keyword Succeeds    5x    500 milliseconds    Page Should Contain    Conta criada com sucesso
    Mostrar Validação    Registo aceite.

Fazer Login
    [Arguments]    ${username}    ${password}
    Mostrar Passo    Fazer login como ${username}.
    Go To    ${BASE_URL}/Login
    Clear Element Text    id=username
    Input Text    id=username    ${username}
    Clear Element Text    id=password
    Input Password    id=password    ${password}
    Click Button    Entrar
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Dashboard
    Page Should Contain    Olá, ${username}
    Mostrar Validação    Login aceite.

Fazer Logout
    Mostrar Passo    Fazer logout.
    Go To    ${BASE_URL}/Logout
    Wait Until Keyword Succeeds    5x    500 milliseconds    Page Should Contain    Sessão terminada
    Mostrar Validação    Logout efetuado.

Iniciar Nível
    [Arguments]    ${level}
    Mostrar Passo    Iniciar nível ${level}.
    Go To    ${BASE_URL}/Play
    Click Button    xpath=//button[@name='level' and @value='${level}']
    Wait Until Keyword Succeeds    5x    500 milliseconds    Page Should Contain Element    xpath=//button[@name='selectedIndex']
    Mostrar Validação    Nível ${level} iniciado.

Responder Pergunta Atual
    [Arguments]    ${option_index}=0
    Mostrar Passo    Responder à pergunta com opção ${option_index}.
    Click Button    xpath=(//button[@name='selectedIndex'])[${{ int($option_index) + 1 }}]
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Result
    Mostrar Validação    Resposta submetida, resultado carregado.

Avançar Para Proxima Pergunta
    Mostrar Passo    Avançar para a próxima pergunta.
    Click Link    Próxima Pergunta
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Play
    Mostrar Validação    Próxima pergunta carregada.

Ir Para Resumo
    Mostrar Passo    Ir para o resumo da sessão.
    Click Link    Ver Resumo
    Wait Until Keyword Succeeds    5x    500 milliseconds    Location Should Contain    /Summary
    Mostrar Validação    Resumo carregado.

Completar Sessão De 5 Perguntas
    [Arguments]    ${level}
    Iniciar Nível    ${level}
    FOR    ${i}    IN RANGE    4
        Responder Pergunta Atual    0
        Avançar Para Proxima Pergunta
    END
    Responder Pergunta Atual    0
    Page Should Contain Link    Ver Resumo
    Mostrar Validação    Sessão de 5 perguntas concluída.

Abrir Dashboard
    Mostrar Passo    Abrir Dashboard.
    Go To    ${BASE_URL}/Dashboard
    Wait Until Keyword Succeeds    5x    500 milliseconds    Page Should Contain Element    xpath=//h1[normalize-space()='Dashboard']
    Mostrar Validação    Dashboard aberto.

Abrir History
    Mostrar Passo    Abrir Histórico.
    Go To    ${BASE_URL}/History
    Wait Until Keyword Succeeds    5x    500 milliseconds    Page Should Contain Element    xpath=//h1[normalize-space()='Histórico de Tentativas']
    Mostrar Validação    Histórico aberto.

Abrir Stats
    Mostrar Passo    Abrir Estatísticas.
    Go To    ${BASE_URL}/Stats
    Wait Until Keyword Succeeds    5x    500 milliseconds    Page Should Contain Element    xpath=//h1[normalize-space()='Estatísticas do Aluno']
    Mostrar Validação    Estatísticas abertas.

Abrir Ranking
    Mostrar Passo    Abrir Ranking.
    Go To    ${BASE_URL}/Ranking
    Wait Until Keyword Succeeds    5x    500 milliseconds    Page Should Contain Element    xpath=//h1[normalize-space()='Ranking Top 5']
    Mostrar Validação    Ranking aberto.

Abrir Teacher
    Mostrar Passo    Abrir Área do Professor.
    Go To    ${BASE_URL}/Teacher
    Wait Until Keyword Succeeds    5x    500 milliseconds    Page Should Contain Element    xpath=//h1[normalize-space()='Área do Professor']
    Mostrar Validação    Área do Professor aberta.

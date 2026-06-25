import argparse
import json
import re
import shutil
import subprocess
import sys
import time
from datetime import datetime
from pathlib import Path
from urllib.error import URLError
from urllib.parse import urljoin
from urllib.request import urlopen
from xml.etree import ElementTree


PUBLIC_ROUTES = ["/", "/Register", "/Login", "/Ranking", "/Teacher"]
PROTECTED_ROUTES = ["/Dashboard", "/Play", "/History", "/Stats", "/Result", "/Summary"]
AUTHENTICATED_ROUTES = ["/Dashboard", "/Play", "/History", "/Stats", "/Ranking", "/Teacher"]
COVERAGE_TERMS = [
    "Register", "Login", "Dashboard", "Play", "Result", "Summary",
    "History", "Stats", "Ranking", "Teacher", "Logout",
    "level 1", "level 2", "level 3", "level 4", "level 5",
]


def find_repo_root():
    # [M39] Localiza a raiz sem depender da pasta atual do terminal.
    current = Path(__file__).resolve()
    for parent in [current.parent, *current.parents]:
        if (parent / "NetLearnBattle.CSharp.csproj").exists():
            return parent
    raise FileNotFoundError("Nao foi encontrada a raiz com NetLearnBattle.CSharp.csproj.")


def load_config(tool_dir):
    config_path = tool_dir / "config.json"
    with config_path.open("r", encoding="utf-8") as file:
        return json.load(file)


def find_command(command_name):
    found = shutil.which(command_name)
    if found:
        return found

    if command_name == "dotnet":
        windows_dotnet = Path(r"C:\Program Files\dotnet\dotnet.exe")
        if windows_dotnet.exists():
            return str(windows_dotnet)

    return command_name


def run_command(command, repo_root):
    # [M39] Executa comandos externos e guarda output para relatório.
    completed = subprocess.run(
        command,
        cwd=repo_root,
        text=True,
        capture_output=True,
        shell=False,
    )
    output = completed.stdout.strip()
    error = completed.stderr.strip()
    combined = "\n".join(part for part in [output, error] if part)
    return {
        "command": " ".join(str(item) for item in command),
        "code": completed.returncode,
        "ok": completed.returncode == 0,
        "output": combined,
    }


def parse_xunit_result(text):
    pattern = r"Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)"
    match = re.search(pattern, text)
    if not match:
        return {"total": None, "passed": None, "failed": None, "skipped": None}

    failed, passed, skipped, total = [int(value) for value in match.groups()]
    return {
        "total": total,
        "passed": passed,
        "failed": failed,
        "skipped": skipped,
    }


def parse_robot_output(output_xml):
    # [M39] Lê output.xml do Robot para contar testes.
    if not output_xml.exists():
        return {
            "exists": False,
            "total": 0,
            "passed": 0,
            "failed": 0,
            "skipped": 0,
            "suites": [],
            "failures": ["Nao foi encontrado Tests/Robot/results/output.xml."],
        }

    tree = ElementTree.parse(output_xml)
    root = tree.getroot()

    total = 0
    passed = 0
    failed = 0
    skipped = 0
    failures = []

    for test in root.iter("test"):
        total += 1
        status_element = test.find("status")
        status = ""
        message = ""
        if status_element is not None:
            status = status_element.attrib.get("status", "")
            message = (status_element.text or "").strip()

        if status == "PASS":
            passed += 1
        elif status == "SKIP":
            skipped += 1
        else:
            failed += 1
            test_name = test.attrib.get("name", "teste sem nome")
            failures.append(f"{test_name}: {message}")

    suites = []
    for suite in root.iter("suite"):
        suite_name = suite.attrib.get("name", "")
        if suite_name and suite_name not in suites:
            suites.append(suite_name)

    return {
        "exists": True,
        "total": total,
        "passed": passed,
        "failed": failed,
        "skipped": skipped,
        "suites": suites,
        "failures": failures,
    }


def list_robot_flows(robot_dir):
    flows = []
    robot_files = sorted(robot_dir.glob("*.robot"))
    for robot_file in robot_files:
        name = robot_file.name.lower()
        if "e2e" in name:
            flows.append("E2E")
        elif "session" in name:
            flows.append("sessao de 5 perguntas")
        elif "invalid" in name:
            flows.append("cenarios invalidos")
        elif "persistence" in name:
            flows.append("persistencia")
        else:
            flows.append(robot_file.stem)
    return flows


def markdown_status(ok):
    return "PASSOU" if ok else "FALHOU"


def short_output(text, max_chars=3500):
    if not text:
        return "_Sem output._"
    if len(text) <= max_chars:
        return text
    return text[-max_chars:]


def run_base_validation(config, repo_root):
    # [M39] Validação normal: build, xUnit e Robot.
    robot_output = repo_root / config["robot_output"]
    dotnet = find_command("dotnet")
    robot = find_command("robot")

    build_result = run_command([dotnet, "build"], repo_root)
    test_result = run_command([dotnet, "test"], repo_root)

    old_robot_output = robot_output / "output.xml"
    if old_robot_output.exists():
        old_robot_output.unlink()

    robot_command = [robot, "--outputdir", config["robot_output"], config["robot_path"]]
    robot_run = run_command(robot_command, repo_root)

    xunit_stats = parse_xunit_result(test_result["output"])
    robot_result = parse_robot_output(robot_output / "output.xml")
    robot_flows = list_robot_flows(repo_root / config["robot_path"])

    return {
        "build": build_result,
        "test": test_result,
        "xunit_stats": xunit_stats,
        "robot_run": robot_run,
        "robot_result": robot_result,
        "robot_flows": robot_flows,
    }


def build_report(config, repo_root, validation):
    # [M39] Gera relatório Markdown da validação normal.
    build_result = validation["build"]
    test_result = validation["test"]
    xunit_stats = validation["xunit_stats"]
    robot_run = validation["robot_run"]
    robot_result = validation["robot_result"]
    robot_flows = validation["robot_flows"]

    expected_xunit = config.get("expected_xunit_tests")
    expected_robot = config.get("expected_robot_tests")

    xunit_ok = (
        test_result["ok"]
        and xunit_stats["failed"] == 0
        and (expected_xunit is None or xunit_stats["total"] == expected_xunit)
    )
    robot_ok = (
        robot_run["ok"]
        and robot_result["exists"]
        and robot_result["failed"] == 0
        and (expected_robot is None or robot_result["total"] == expected_robot)
    )
    ready = build_result["ok"] and xunit_ok and robot_ok

    lines = [
        "# RobotMCP Report - NetLearn Battle C#",
        "",
        f"Gerado em: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
        f"Raiz analisada: `{repo_root}`",
        "",
        "## 1. Build",
        "",
        f"* comando executado: `{build_result['command']}`",
        f"* resultado: **{markdown_status(build_result['ok'])}**",
    ]

    if not build_result["ok"]:
        lines += ["", "```text", short_output(build_result["output"]), "```"]

    lines += [
        "",
        "## 2. Testes xUnit",
        "",
        f"* comando executado: `{test_result['command']}`",
        f"* resultado: **{markdown_status(xunit_ok)}**",
        f"* total: {xunit_stats['total']}",
        f"* passed: {xunit_stats['passed']}",
        f"* failed: {xunit_stats['failed']}",
        f"* skipped: {xunit_stats['skipped']}",
    ]

    if not xunit_ok:
        lines += ["", "```text", short_output(test_result["output"]), "```"]

    lines += [
        "",
        "## 3. Testes Robot",
        "",
        f"* comando executado: `{robot_run['command']}`",
        f"* resultado: **{markdown_status(robot_ok)}**",
        f"* total: {robot_result['total']}",
        f"* passed: {robot_result['passed']}",
        f"* failed: {robot_result['failed']}",
        f"* skipped: {robot_result['skipped']}",
        "",
        "Suites executadas:",
    ]

    for suite in robot_result["suites"]:
        lines.append(f"* {suite}")

    if robot_result["failures"]:
        lines += ["", "Falhas encontradas:"]
        for failure in robot_result["failures"]:
            lines.append(f"* {failure}")

    if not robot_run["ok"]:
        lines += ["", "Output do Robot:", "", "```text", short_output(robot_run["output"]), "```"]

    lines += [
        "",
        "## 4. Fluxos cobertos",
        "",
    ]

    for flow in robot_flows:
        lines.append(f"* {flow}")

    lines += [
        "",
        "## 5. Fluxos que podem ser melhorados",
        "",
        "* testar todos os níveis 1 a 5 via Robot;",
        "* validar página Teacher com dados reais;",
        "* validar ranking com vários utilizadores;",
        "* validar que CorrectIndex não aparece no HTML;",
        "* validar tentativa de acesso a Result/Summary sem sessão;",
        "* validar responsividade básica, se aplicável;",
        "* validar TCP separadamente por xUnit, não necessariamente por Robot.",
        "",
        "## 6. Recomendação",
        "",
    ]

    if ready:
        lines.append("* pronto para entrega académica, mantendo as limitações documentadas.")
    else:
        lines.append("* corrigir os pontos falhados antes da entrega.")

    return "\n".join(lines) + "\n"


def wait_for_web(url, timeout_seconds):
    deadline = time.time() + timeout_seconds
    while time.time() < deadline:
        try:
            with urlopen(url, timeout=2) as response:
                if response.status < 500:
                    return True
        except URLError:
            time.sleep(0.5)
    return False


def start_explorer_server(config, repo_root):
    # [M39] Inicia a app numa porta isolada para exploração segura.
    dotnet = find_command("dotnet")
    url = config["explorer_url"]
    output_dir = repo_root / "tools" / "robotmcp" / "output"
    output_dir.mkdir(parents=True, exist_ok=True)
    stdout_path = output_dir / "explorer-web-output.log"
    stderr_path = output_dir / "explorer-web-error.log"
    stdout_file = stdout_path.open("w", encoding="utf-8")
    stderr_file = stderr_path.open("w", encoding="utf-8")

    process = subprocess.Popen(
        [dotnet, "run", "--urls", url],
        cwd=repo_root,
        stdout=stdout_file,
        stderr=stderr_file,
        text=True,
    )

    return process, stdout_file, stderr_file


def stop_process(process, stdout_file, stderr_file):
    try:
        if process and process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=8)
            except subprocess.TimeoutExpired:
                process.kill()
    finally:
        stdout_file.close()
        stderr_file.close()


def selenium_modules():
    try:
        from selenium import webdriver
        from selenium.common.exceptions import TimeoutException, WebDriverException
        from selenium.webdriver.common.by import By
        from selenium.webdriver.support import expected_conditions as EC
        from selenium.webdriver.support.ui import WebDriverWait
        return webdriver, By, WebDriverWait, EC, TimeoutException, WebDriverException
    except ImportError as exc:
        raise RuntimeError("Selenium não está instalado. Instale com: pip install selenium") from exc


def is_destructive(element, keywords):
    # [M39] Evita clicar em ações destrutivas.
    text_parts = [
        element.text or "",
        element.get_attribute("href") or "",
        element.get_attribute("id") or "",
        element.get_attribute("class") or "",
        element.get_attribute("aria-label") or "",
        element.get_attribute("name") or "",
        element.get_attribute("value") or "",
    ]
    combined = " ".join(text_parts).lower()
    return any(keyword.lower() in combined for keyword in keywords)


def visible_label(element):
    label = (element.text or "").strip()
    if label:
        return label
    for attr in ["aria-label", "value", "name", "id", "href"]:
        value = element.get_attribute(attr)
        if value:
            return value.strip()
    return ""


def inspect_current_page(driver, route, problems, ignored_actions, button_notes):
    # [M39] Procura sinais simples de erro ou exposição de dados.
    title = (driver.title or "").strip()
    source = driver.page_source or ""
    body_text = ""
    try:
        body_text = driver.find_element("tag name", "body").text.strip()
    except Exception:
        pass

    lower_source = source.lower()
    lower_text = body_text.lower()

    if not title:
        problems.append(("Baixo", route, "Título vazio."))
    if not body_text:
        problems.append(("Médio", route, "Página sem texto visível."))

    for marker in ["exception", "stack trace"]:
        if marker in lower_source or marker in lower_text:
            problems.append(("Alto", route, f"Texto suspeito encontrado: {marker}."))

    if re.search(r"(erro|error|internal)\s*500|500\s*(internal|erro|error)", lower_text):
        problems.append(("Alto", route, "Possível erro HTTP 500 visível na página."))

    if re.search(r"\berror\b", lower_text) and "login" not in lower_text:
        problems.append(("Médio", route, "Texto 'Error' encontrado na página."))

    for sensitive in ["correctindex", "correct_index", "passwordhash", "salt", "hash"]:
        if sensitive in lower_source:
            problems.append(("Alto", route, f"Campo sensível visível no HTML: {sensitive}."))

    for element in driver.find_elements("css selector", "a, button"):
        label = visible_label(element)
        if not label:
            button_notes.append(f"{route}: elemento clicável sem texto/aria-label.")
        if is_destructive(element, DESTRUCTIVE_KEYWORDS):
            ignored_actions.append(f"{route}: ação destrutiva ignorada: {label or '[sem label]'}")

    for form in driver.find_elements("tag name", "form"):
        submit_buttons = form.find_elements("css selector", "button[type='submit'], input[type='submit'], button:not([type])")
        if not submit_buttons:
            problems.append(("Baixo", route, "Formulário sem botão de submit visível."))


DESTRUCTIVE_KEYWORDS = []


def visit_route(driver, base_url, route, timeout, public_result, problems, ignored_actions, button_notes):
    url = urljoin(base_url, route)
    driver.get(url)
    WebDriverWait(driver, timeout).until(lambda d: d.execute_script("return document.readyState") == "complete")
    inspect_current_page(driver, route, problems, ignored_actions, button_notes)
    current = driver.current_url
    public_result.append({
        "route": route,
        "title": driver.title,
        "url": current,
        "problem": "OK" if not any(p[1] == route for p in problems) else "ver problemas",
    })


def fill_input(driver, element_id, value):
    element = driver.find_element("id", element_id)
    element.clear()
    element.send_keys(value)


def click_by_text(driver, texts):
    candidates = driver.find_elements("css selector", "a, button")
    for element in candidates:
        label = visible_label(element).lower()
        if any(text.lower() in label for text in texts):
            element.click()
            return True
    return False


def analyze_robot_coverage(robot_dir):
    text = ""
    for robot_file in robot_dir.glob("*.robot"):
        text += "\n" + robot_file.read_text(encoding="utf-8", errors="ignore")

    lower_text = text.lower()
    result = []
    for term in COVERAGE_TERMS:
        found = term.lower() in lower_text
        result.append((term, found))
    return result


def run_explorer(config, repo_root):
    # [M39] Explorer opcional: navega no site sem alterar código.
    global DESTRUCTIVE_KEYWORDS
    DESTRUCTIVE_KEYWORDS = config.get("destructive_keywords", [])

    webdriver, By, WebDriverWait, EC, TimeoutException, WebDriverException = selenium_modules()
    globals()["WebDriverWait"] = WebDriverWait

    base_url = config["explorer_url"]
    timeout = int(config.get("explorer_timeout_seconds", 10))
    process = None
    stdout_file = None
    stderr_file = None
    driver = None

    result = {
        "url": base_url,
        "visited": [],
        "protected": [],
        "auth_flow": [],
        "clicked": [],
        "ignored": [],
        "button_notes": [],
        "problems": [],
        "coverage": analyze_robot_coverage(repo_root / config["robot_path"]),
    }

    try:
        process, stdout_file, stderr_file = start_explorer_server(config, repo_root)
        if not wait_for_web(base_url, timeout):
            result["problems"].append(("Alto", "server", "Aplicação não iniciou dentro do timeout."))
            return result

        options = webdriver.ChromeOptions()
        options.add_argument("--log-level=3")
        options.add_experimental_option("excludeSwitches", ["enable-logging"])
        driver = webdriver.Chrome(options=options)
        driver.set_page_load_timeout(timeout)

        for route in PUBLIC_ROUTES:
            visit_route(driver, base_url, route, timeout, result["visited"], result["problems"], result["ignored"], result["button_notes"])

        for route in PROTECTED_ROUTES:
            driver.get(urljoin(base_url, route))
            WebDriverWait(driver, timeout).until(lambda d: d.execute_script("return document.readyState") == "complete")
            inspect_current_page(driver, route, result["problems"], result["ignored"], result["button_notes"])
            redirected_to_login = "/Login" in driver.current_url
            result["protected"].append({
                "route": route,
                "expected": "redirecionar para Login ou mensagem controlada",
                "observed": driver.current_url,
                "ok": redirected_to_login or route in ["/Result", "/Summary"],
            })
            if not redirected_to_login and route not in ["/Result", "/Summary"]:
                result["problems"].append(("Alto", route, "Página protegida acessível sem login."))

        username = f"explorer_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
        password = "explorer_123"

        driver.get(urljoin(base_url, "/Register"))
        fill_input(driver, "username", username)
        fill_input(driver, "password", password)
        click_by_text(driver, ["Criar Conta"])
        result["clicked"].append("Register: Criar Conta")
        time.sleep(0.5)
        result["auth_flow"].append(f"registo: {driver.current_url}")

        driver.get(urljoin(base_url, "/Login"))
        fill_input(driver, "username", username)
        fill_input(driver, "password", password)
        click_by_text(driver, ["Entrar"])
        result["clicked"].append("Login: Entrar")
        time.sleep(0.8)
        result["auth_flow"].append(f"login: {driver.current_url}")
        if "/Dashboard" not in driver.current_url:
            result["problems"].append(("Alto", "/Login", "Login do utilizador temporário não chegou ao Dashboard."))

        for route in AUTHENTICATED_ROUTES:
            visit_route(driver, base_url, route, timeout, result["visited"], result["problems"], result["ignored"], result["button_notes"])

        explore_game(driver, base_url, timeout, result)

        driver.get(urljoin(base_url, "/History"))
        inspect_current_page(driver, "/History", result["problems"], result["ignored"], result["button_notes"])
        result["auth_flow"].append("history: visitado")

        driver.get(urljoin(base_url, "/Stats"))
        inspect_current_page(driver, "/Stats", result["problems"], result["ignored"], result["button_notes"])
        result["auth_flow"].append("stats: visitado")

        driver.get(urljoin(base_url, "/Logout"))
        time.sleep(0.5)
        inspect_current_page(driver, "/Logout", result["problems"], result["ignored"], result["button_notes"])
        result["clicked"].append("Logout")
        result["auth_flow"].append(f"logout: {driver.current_url}")

    except RuntimeError:
        raise
    except Exception as exc:
        result["problems"].append(("Alto", "explorer", f"Falha durante exploração: {exc}"))
    finally:
        if driver:
            driver.quit()
        if process:
            stop_process(process, stdout_file, stderr_file)

    return result


def explore_game(driver, base_url, timeout, result):
    # [M39] Explora uma sessão segura do nível 1.
    driver.get(urljoin(base_url, "/Play"))
    WebDriverWait(driver, timeout).until(lambda d: d.execute_script("return document.readyState") == "complete")
    inspect_current_page(driver, "/Play", result["problems"], result["ignored"], result["button_notes"])

    try:
        level_button = driver.find_element("css selector", "button[name='level'][value='1']")
        level_button.click()
        result["clicked"].append("Play: nível 1")
        time.sleep(0.5)
    except Exception:
        result["problems"].append(("Alto", "/Play", "Não foi possível iniciar nível 1."))
        return

    interactions = 0
    while interactions < 6:
        interactions += 1
        if "CorrectIndex" in driver.page_source or "correct_index" in driver.page_source:
            result["problems"].append(("Alto", "/Play", "CorrectIndex apareceu no HTML durante o jogo."))

        buttons = driver.find_elements("css selector", "button[name='selectedIndex']")
        if buttons:
            buttons[0].click()
            result["clicked"].append("Play: resposta opção 0")
            time.sleep(0.5)
            page_text = driver.find_element("tag name", "body").text
            if "Correto" not in page_text and "Errado" not in page_text:
                result["problems"].append(("Médio", "/Result", "Resposta do jogo não mostrou feedback esperado."))

        if click_by_text(driver, ["Próxima Pergunta"]):
            result["clicked"].append("Result: Próxima Pergunta")
            time.sleep(0.4)
            continue

        if click_by_text(driver, ["Ver Resumo"]):
            result["clicked"].append("Result: Ver Resumo")
            time.sleep(0.5)
            break

        break

    driver.get(urljoin(base_url, "/Summary"))
    inspect_current_page(driver, "/Summary", result["problems"], result["ignored"], result["button_notes"])
    result["auth_flow"].append("jogo nível 1: explorado")


def build_explorer_report(config, repo_root, validation, explorer_result):
    # [M39] Relatório extra com páginas visitadas e buracos prováveis.
    build_ok = validation["build"]["ok"]
    xunit_stats = validation["xunit_stats"]
    robot_result = validation["robot_result"]

    problems = explorer_result["problems"]
    high = [p for p in problems if p[0] == "Alto"]
    medium = [p for p in problems if p[0] == "Médio"]
    low = [p for p in problems if p[0] == "Baixo"]

    lines = [
        "# RobotMCP Explorer Report",
        "",
        "## 1. Resumo",
        "",
        f"* data/hora: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
        f"* URL: {explorer_result['url']}",
        f"* páginas visitadas: {len(explorer_result['visited'])}",
        f"* cliques executados: {len(explorer_result['clicked'])}",
        f"* ações ignoradas: {len(explorer_result['ignored'])}",
        f"* problemas encontrados: {len(problems)}",
        "",
        "## 2. Build/xUnit/Robot",
        "",
        f"* Build: {markdown_status(build_ok)}",
        f"* xUnit: total={xunit_stats['total']}, passed={xunit_stats['passed']}, failed={xunit_stats['failed']}, skipped={xunit_stats['skipped']}",
        f"* Robot: total={robot_result['total']}, passed={robot_result['passed']}, failed={robot_result['failed']}, skipped={robot_result['skipped']}",
        "",
        "## 3. Páginas públicas",
        "",
        "| rota | URL observada | título | problema |",
        "|------|---------------|--------|----------|",
    ]

    for item in [v for v in explorer_result["visited"] if v["route"] in PUBLIC_ROUTES]:
        lines.append(f"| `{item['route']}` | `{item['url']}` | {item['title']} | {item['problem']} |")

    lines += [
        "",
        "## 4. Páginas protegidas sem login",
        "",
        "| rota | esperado | observado | resultado |",
        "|------|----------|-----------|-----------|",
    ]

    for item in explorer_result["protected"]:
        status = "OK" if item["ok"] else "verificar"
        lines.append(f"| `{item['route']}` | {item['expected']} | `{item['observed']}` | {status} |")

    lines += [
        "",
        "## 5. Fluxo autenticado",
        "",
    ]

    for item in explorer_result["auth_flow"]:
        lines.append(f"* {item}")

    lines += [
        "",
        "## 6. Cliques e botões",
        "",
        "Cliques executados:",
    ]

    for item in explorer_result["clicked"]:
        lines.append(f"* {item}")

    lines.append("")
    lines.append("Ações ignoradas por segurança:")
    if explorer_result["ignored"]:
        for item in explorer_result["ignored"]:
            lines.append(f"* {item}")
    else:
        lines.append("* nenhuma")

    lines.append("")
    lines.append("Botões/links sem texto ou aria-label:")
    if explorer_result["button_notes"]:
        for item in explorer_result["button_notes"]:
            lines.append(f"* {item}")
    else:
        lines.append("* nenhum")

    lines += [
        "",
        "## 7. Possíveis buracos encontrados",
        "",
        "### Alto",
    ]
    lines += [f"* `{route}` — {msg}" for _, route, msg in high] or ["* nenhum"]
    lines += ["", "### Médio"]
    lines += [f"* `{route}` — {msg}" for _, route, msg in medium] or ["* nenhum"]
    lines += ["", "### Baixo"]
    lines += [f"* `{route}` — {msg}" for _, route, msg in low] or ["* nenhum"]

    lines += [
        "",
        "## 8. Cobertura Robot provável",
        "",
    ]

    for term, found in explorer_result["coverage"]:
        status = "encontrada nos testes" if found else "não encontrada explicitamente / verificar manualmente"
        lines.append(f"* {term}: {status}")

    lines += [
        "",
        "## 9. Recomendações",
        "",
        "* adicionar teste Robot para todos os níveis 1 a 5;",
        "* adicionar verificação Robot de que CorrectIndex não aparece no HTML;",
        "* adicionar teste Robot para acesso direto a Result/Summary sem sessão;",
        "* manter TCP validado por xUnit, não pelo browser.",
        "",
    ]

    if high:
        lines.append("* Corrigir problemas de prioridade alta antes da entrega.")
    else:
        lines.append("* Nenhum buraco crítico encontrado pelo explorer.")

    return "\n".join(lines) + "\n"


def write_report(tool_dir, name, content):
    output_dir = tool_dir / "output"
    output_dir.mkdir(parents=True, exist_ok=True)
    report_path = output_dir / name
    report_path.write_text(content, encoding="utf-8")
    return report_path


def validate_project_layout(config, repo_root):
    main_project = repo_root / config["main_project"]
    robot_dir = repo_root / config["robot_path"]
    if not main_project.exists():
        raise FileNotFoundError(f"Projeto principal nao encontrado: {main_project}")
    if not robot_dir.exists():
        raise FileNotFoundError(f"Pasta Robot nao encontrada: {robot_dir}")


def main():
    # [M39] Sem --explore mantém o comportamento normal do RobotMCP.
    parser = argparse.ArgumentParser(description="RobotMCP para o NetLearn Battle C#.")
    # [M39] --explorer é alias para evitar erro quando o nome é escrito por extenso.
    parser.add_argument("--explore", "--explorer", action="store_true", help="Executa exploração segura com Selenium.")
    args = parser.parse_args()

    tool_dir = Path(__file__).resolve().parent
    repo_root = find_repo_root()
    config = load_config(tool_dir)
    validate_project_layout(config, repo_root)

    validation = run_base_validation(config, repo_root)
    report = build_report(config, repo_root, validation)
    report_path = write_report(tool_dir, "robotmcp_report.md", report)
    print(f"RobotMCP report criado em: {report_path}")

    if args.explore:
        try:
            explorer_result = run_explorer(config, repo_root)
            explorer_report = build_explorer_report(config, repo_root, validation, explorer_result)
            explorer_path = write_report(tool_dir, "robotmcp_explorer_report.md", explorer_report)
            print(f"RobotMCP explorer report criado em: {explorer_path}")
        except RuntimeError as exc:
            explorer_report = "\n".join([
                "# RobotMCP Explorer Report",
                "",
                "## 1. Resumo",
                "",
                f"* data/hora: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
                f"* erro: {exc}",
                "",
                "## 9. Recomendações",
                "",
                "* instalar Selenium com `pip install selenium` para usar o modo explorer.",
                "",
            ])
            explorer_path = write_report(tool_dir, "robotmcp_explorer_report.md", explorer_report)
            print(f"RobotMCP explorer report criado em: {explorer_path}")
            return 1

    if not validation["build"]["ok"] or validation["test"]["code"] != 0 or validation["robot_run"]["code"] != 0:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())

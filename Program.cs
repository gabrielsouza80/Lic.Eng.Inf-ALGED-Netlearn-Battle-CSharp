using NetLearnBattle.CSharp.Network;
using NetLearnBattle.CSharp.Services;

var mode = "web";
var tcpHost = "127.0.0.1";
var tcpPort = 5001;

// [M03] Lê argumentos para escolher web, tcp-server ou tcp-client.
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--" && i + 1 < args.Length) { mode = args[i + 1]; i++; }
    else if (args[i] == "--host" && i + 1 < args.Length) { tcpHost = args[i + 1]; i++; }
    else if (args[i] == "--port" && i + 1 < args.Length) { tcpPort = int.Parse(args[i + 1]); i++; }
    else if (i == 0 && args[i] is "tcp-server" or "tcp-client") mode = args[i];
}

if (mode == "tcp-client")
{
    // [M35] Cliente TCP demonstrativo usado no terminal.
    var client = new TcpClientDemo(tcpHost, tcpPort);
    await client.RunAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// [M03] Ativa Razor Pages e sessão web.
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// [M03] Regista os serviços usados pelas páginas e pelo TCP.
builder.Services.AddSingleton<JsonService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<ScoreService>();
builder.Services.AddSingleton<GameSessionStore>();
builder.Services.AddSingleton<IpService>();
builder.Services.AddSingleton<AclService>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<StatsService>();

if (mode == "tcp-server")
{
    // [M34] Modo servidor TCP: reutiliza os mesmos serviços da aplicação.
    var app = builder.Build();
    var server = new TcpServer(
        app.Services.GetRequiredService<AuthService>(),
        app.Services.GetRequiredService<ScoreService>(),
        app.Services.GetRequiredService<StatsService>(),
        app.Services.GetRequiredService<IpService>(),
        app.Services.GetRequiredService<AclService>(),
        app.Services.GetRequiredService<GameService>(),
        app.Services.GetRequiredService<JsonService>(),
        tcpPort,
        tcpHost);
    await server.StartAsync();
    return;
}

if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    // [M03] Modo web padrão: site em localhost:5002.
    builder.WebHost.UseUrls("http://localhost:5002");
}

var app2 = builder.Build();

if (!app2.Environment.IsDevelopment())
{
    app2.UseExceptionHandler("/Error");
    app2.UseHsts();
}

app2.UseStaticFiles();
app2.UseRouting();
// [M19] A sessão guarda utilizador autenticado e sessão de jogo.
app2.UseSession();
app2.MapRazorPages();

// [M03] Inicia a aplicação web.
app2.Run();

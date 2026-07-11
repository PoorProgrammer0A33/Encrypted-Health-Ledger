using EHL.Api.Services;
using EHL.Ledger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string connectionString = builder.Configuration.GetConnectionString("OracleLedger")
    ?? throw new InvalidOperationException("OracleLedger connection string not found in configuration.");

builder.Services.AddSingleton(new LedgerService(connectionString));
builder.Services.AddSingleton<CryptoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
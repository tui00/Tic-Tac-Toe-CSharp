var builder = WebApplication.CreateBuilder(args);

// Добавь контроллеры
builder.Services.AddControllers();

var app = builder.Build();

// Включаем маршрутизацию
app.UseRouting();

// Включаем контроллеры
app.MapControllers();

app.Run();

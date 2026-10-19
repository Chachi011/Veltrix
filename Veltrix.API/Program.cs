using Microsoft.EntityFrameworkCore;
using Veltrix.Infrastructure.Persistence;
using Veltrix.Application.Interfaces;
using Veltrix.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IGameService, GameService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
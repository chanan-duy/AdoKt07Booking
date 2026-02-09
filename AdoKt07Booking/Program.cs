using AdoKt07Booking.Components;
using AdoKt07Booking.Data;
using AdoKt07Booking.Services;
using Microsoft.EntityFrameworkCore;

namespace AdoKt07Booking;

public class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddRazorComponents()
			.AddInteractiveServerComponents();

		builder.Services.AddDbContext<AppDbContext>(options =>
		{
			options.UseSqlite(builder.Configuration.GetConnectionString("AppDbContext") ??
			                  throw new InvalidOperationException("Connection string 'AppDbContext' not found."));
		});

		builder.Services.AddScoped<IHotelService, HotelService>();
		builder.Services.AddScoped<IRestaurantService, RestaurantService>();

		var app = builder.Build();

		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Error");
			app.UseHsts();
		}

		using var scope = app.Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await dbContext.Database.MigrateAsync();
		await AppDbSeeder.SeedAsync(dbContext);

		app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
		app.UseHttpsRedirection();

		app.UseAntiforgery();

		app.MapStaticAssets();
		app.MapRazorComponents<App>()
			.AddInteractiveServerRenderMode();

		await app.RunAsync();
	}
}

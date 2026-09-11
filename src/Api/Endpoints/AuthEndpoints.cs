using Api.Contracts.Auth;
using Application.Abstractions;
using Domain.Entities;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            ReceiptIqDbContext dbContext,
            IJwtTokenGenerator tokenGenerator) =>
        {
            if (await userManager.FindByEmailAsync(request.Email) is not null)
            {
                return Results.Conflict(new { message = "Email is already registered." });
            }

            var identityUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email
            };

            var createResult = await userManager.CreateAsync(identityUser, request.Password);
            if (!createResult.Succeeded)
            {
                return Results.ValidationProblem(
                    createResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
            }

            dbContext.AppUsers.Add(new User
            {
                Id = identityUser.Id,
                Email = request.Email,
                DisplayName = request.DisplayName
            });
            await dbContext.SaveChangesAsync();

            var token = tokenGenerator.GenerateToken(identityUser.Id, request.Email);
            return Results.Ok(new AuthResponse(token.Value, token.ExpiresAtUtc));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            IJwtTokenGenerator tokenGenerator) =>
        {
            var identityUser = await userManager.FindByEmailAsync(request.Email);
            if (identityUser is null || !await userManager.CheckPasswordAsync(identityUser, request.Password))
            {
                return Results.Unauthorized();
            }

            var token = tokenGenerator.GenerateToken(identityUser.Id, request.Email);
            return Results.Ok(new AuthResponse(token.Value, token.ExpiresAtUtc));
        });
    }
}

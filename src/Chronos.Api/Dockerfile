# Build context is src/ (see vercel.json "services.api"), so only the four production projects are
# sent to the builder -- tests and the frontend stay out of the image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against project files only, so source edits don't invalidate the restore layer.
COPY Chronos.Domain/Chronos.Domain.csproj Chronos.Domain/
COPY Chronos.Application/Chronos.Application.csproj Chronos.Application/
COPY Chronos.Infrastructure/Chronos.Infrastructure.csproj Chronos.Infrastructure/
COPY Chronos.Api/Chronos.Api.csproj Chronos.Api/
RUN dotnet restore Chronos.Api/Chronos.Api.csproj

COPY . .
RUN dotnet publish Chronos.Api/Chronos.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Deployed instances always run against PostgreSQL (Supabase). ConnectionStrings__PostgreSql and
# Jwt__SigningKey must come from the platform's secret store -- Program.cs refuses to start
# without a signing key outside Development.
ENV ASPNETCORE_ENVIRONMENT=Production \
    DatabaseProvider=PostgreSql

USER $APP_UID
EXPOSE 8080
# Vercel's container runtime (and most PaaS) tell the process which port to bind via $PORT.
CMD ["sh", "-c", "exec dotnet Chronos.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]

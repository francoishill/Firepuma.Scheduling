FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS restore
WORKDIR /src

COPY ["Firepuma.Scheduling.Domain/Firepuma.Scheduling.Domain.csproj", "Firepuma.Scheduling.Domain/"]
COPY ["Firepuma.Scheduling.Infrastructure/Firepuma.Scheduling.Infrastructure.csproj", "Firepuma.Scheduling.Infrastructure/"]
COPY ["Firepuma.Scheduling.Tests/Firepuma.Scheduling.Tests.csproj", "Firepuma.Scheduling.Tests/"]

COPY ["Firepuma.Scheduling.Worker/Firepuma.Scheduling.Worker.csproj", "Firepuma.Scheduling.Worker/"]
RUN dotnet restore "Firepuma.Scheduling.Worker/Firepuma.Scheduling.Worker.csproj"
COPY . .


FROM restore AS test
RUN dotnet restore "Firepuma.Scheduling.Tests/Firepuma.Scheduling.Tests.csproj"
RUN dotnet test --no-restore --logger:trx --results-directory /testsresults


FROM test AS publish
ARG target_configuration=Release
ARG version=dev-invalid-version
WORKDIR /src/Firepuma.Scheduling.Worker
RUN dotnet publish  "Firepuma.Scheduling.Worker.csproj" -c ${target_configuration} --no-restore /p:Version=${version} -o /app


FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS final
LABEL maintainer="Francois Hill <francoishill11@gmail.com>"
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "Firepuma.Scheduling.Worker.dll"]
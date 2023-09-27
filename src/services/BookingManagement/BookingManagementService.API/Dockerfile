#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:8.0.100-preview.7-jammy AS base
WORKDIR /app
EXPOSE 80
#EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-preview.7-jammy AS build
WORKDIR /src
#COPY ["BookingManagementService.API.csproj", "./src"]


#COPY "CinemaTicketBookingService.sln" "CinemaTicketBookingService.sln"

COPY "services/BookingManagement/BookingManagementService.API/BookingManagementService.API.csproj" "BookingManagementService.API/BookingManagementService.API.csproj"
COPY "services/BookingManagement/BookingManagementService.Application/BookingManagementService.Application.csproj" "BookingManagementService.Application/BookingManagementService.Application.csproj"
COPY "services/BookingManagement/BookingManagementService.Domain/BookingManagementService.Domain.csproj" "BookingManagementService.Domain/BookingManagementService.Domain.csproj"
COPY "services/BookingManagement/BookingManagementService.Infrastructure/BookingManagementService.Infrastructure.csproj" "BookingManagementService.Infrastructure/BookingManagementService.Infrastructure.csproj"

WORKDIR BookingManagementService.API
RUN dotnet restore "BookingManagementService.API.csproj"

COPY . .
WORKDIR "services/BookingManagement/BookingManagementService.API"
RUN dotnet publish  -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT [ "dotnet", "BookingManagementService.API.dll"]
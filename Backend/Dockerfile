FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
#copy cdproj all
COPY CoworkingSpace.API/CoworkingSpace.API.csproj CoworkingSpace.API/
COPY CoworkingSpace.BLL/CoworkingSpace.BLL.csproj CoworkingSpace.BLL/
COPY CoworkingSpace.DAL/CoworkingSpace.DAL.csproj CoworkingSpace.DAL/

RUN dotnet restore CoworkingSpace.API/CoworkingSpace.API.csproj

#copy all files
COPY . .
RUN dotnet publish CoworkingSpace.API/CoworkingSpace.API.csproj -c Release -o /app/out

#opreational stage 
FROM  mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["dotnet","CoworkingSpace.API.dll"]



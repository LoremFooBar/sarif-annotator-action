FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

ENV ProjectName=LoremFooBar.SarifAnnotatorAction

WORKDIR /source

COPY Directory.Build.props .
COPY src/$ProjectName/$ProjectName.csproj src/$ProjectName/$ProjectName.csproj
COPY src/DiffPatchFixed/DiffPatchFixed.csproj src/DiffPatchFixed/DiffPatchFixed.csproj

RUN dotnet restore src/$ProjectName/$ProjectName.csproj

COPY src/ src/

RUN dotnet publish -c Release -o /app src/$ProjectName/$ProjectName.csproj

# Label as GitHub action
LABEL com.github.actions.name="Sarif Annotator"
# Limit to 160 characters
LABEL com.github.actions.description="Create annotations from SARIF file"
# See branding:
# https://docs.github.com/actions/creating-actions/metadata-syntax-for-github-actions#branding
LABEL com.github.actions.icon="code"
LABEL com.github.actions.color="purple"

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime

WORKDIR /app

COPY --from=build /app .

ENTRYPOINT ["dotnet", "/app/LoremFooBar.SarifAnnotatorAction.dll"]

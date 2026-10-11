name: Build Hotyn Helper

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build:
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Show repo files
        shell: pwsh
        run: |
          Write-Host "==============================="
          Write-Host "ФАЙЛЫ В КОРНЕ РЕПОЗИТОРИЯ:"
          Write-Host "==============================="
          Get-ChildItem -Force | Format-Table Name, Length -AutoSize
          Write-Host ""
          Write-Host "==============================="
          Write-Host "ПРОВЕРКА НУЖНЫХ ФАЙЛОВ:"
          Write-Host "==============================="
          $files = @("HotynHelper.csproj","Program.cs","MainForm.cs")
          foreach ($f in $files) {
            if (Test-Path $f) {
              Write-Host "OK  $f  найден" -ForegroundColor Green
            } else {
              Write-Host "MISSING  $f  ОТСУТСТВУЕТ" -ForegroundColor Red
            }
          }
          Write-Host ""
          Write-Host "СОДЕРЖИМОЕ ПАПОК:"
          Get-ChildItem -Recurse -Directory | Select-Object FullName

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Inject API key
        shell: pwsh
        run: |
          $key = "${{ secrets.OPENROUTER_KEY }}"
          if ([string]::IsNullOrWhiteSpace($key)) {
            Write-Host "::warning::Секрет OPENROUTER_KEY не задан."
            $key = "NO_KEY_CONFIGURED"
          }
          (Get-Content MainForm.cs) -replace 'OPENROUTER_KEY_PLACEHOLDER', $key | Set-Content MainForm.cs
          Write-Host "Ключ подставлен."

      - name: Restore
        run: dotnet restore HotynHelper.csproj

      - name: Publish
        run: dotnet publish HotynHelper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

      - name: Upload
        uses: actions/upload-artifact@v4
        with:
          name: HotynHelper-exe
          path: publish/HotynHelper.exe

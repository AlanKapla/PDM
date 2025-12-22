# Automatyczna konwersja fetch → axios w plikach React

$pagesPath = "c:\Users\kapla\source\repos\PDM\01-Applications\ProjectDataManagementUI\src\pages"
$files = Get-ChildItem -Path $pagesPath -Filter "*.tsx" -Recurse

Write-Host "🔄 Migracja fetch → axios response patterns" -ForegroundColor Cyan
Write-Host "Znaleziono plików: $($files.Count)" -ForegroundColor Yellow
Write-Host ""

foreach ($file in $files) {
    Write-Host "Przetwarzanie: $($file.Name)" -ForegroundColor Green
    
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    # Pattern 1: Usuń if (response.ok) { const data = await response.json(); } 
    # Zamień na: const data = response.data;
    $content = $content -replace '(?s)if\s*\(\s*response\.ok\s*\)\s*\{\s*const\s+(\w+)\s*=\s*await\s+response\.json\(\)\s*;', 'const $1 = response.data;'
    
    # Pattern 2: Usuń if (response.ok) setData(await response.json());
    # Zamień na: setData(response.data);
    $content = $content -replace 'if\s*\(\s*response\.ok\s*\)\s+(\w+)\(await\s+response\.json\(\)\);', '$1(response.data);'
    
    # Pattern 3: Usuń if (!response.ok) { throw/return }
    # Axios automatycznie rzuca błędy
    $content = $content -replace '(?s)if\s*\(\s*!response\.ok\s*\)\s*\{[^}]*throw[^}]*\}', '// Axios throws errors automatically'
    $content = $content -replace '(?s)if\s*\(\s*!response\.ok\s*\)\s*\{[^}]*return;?\s*\}', '// Axios throws errors automatically'
    
    # Pattern 4: Zamień await handleApiError(response) na handleApiError(error)
    # To wymaga użycia w catch block
    $content = $content -replace 'await\s+handleApiError\(response\)', 'handleApiError(error)'
    
    # Pattern 5: Usuń niepotrzebne await response.json() w set*()
    $content = $content -replace 'set(\w+)\(await\s+response\.json\(\)\);', 'set$1(response.data);'
    
    # Pattern 6: Usuń sprawdzanie response.ok przed zwrotem danych
    $content = $content -replace 'if\s*\(\s*response\.ok\s*\)\s*\{', '// Response data:'
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  ✅ Zaktualizowano" -ForegroundColor Green
    } else {
        Write-Host "  ⏭ Bez zmian" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "✅ Migracja zakończona!" -ForegroundColor Cyan
Write-Host "⚠️  Sprawdź błędy TypeScript i popraw manualnie obsługę błędów w catch blocks" -ForegroundColor Yellow

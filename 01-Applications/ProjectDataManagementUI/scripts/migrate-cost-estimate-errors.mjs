import fs from "fs";

const file = "src/pages/CostEstimateEditPage.tsx";
let content = fs.readFileSync(file, "utf8");

content = content.replace(
  "const { showSuccess, showError, showApiSuccess } = useToastNotification();",
  "const { showSuccess, showError, showApiSuccess, showApiError } = useToastNotification();",
);

const apiErrorPatterns = [
  [/showError\(\s*'Błąd zapisu',\s*'Nie udało się zapisać zmiany pola'\s*\);/g, "showApiError(error);"],
  [/showError\('Błąd synchronizacji', 'Nie udało się zsynchronizować harmonogramu'\);/g, "showApiError(error);"],
  [/showError\(\s*'Błąd',\s*err instanceof Error \? err\.message : '[^']+'\s*\);/g, "showApiError(err);"],
  [/showError\('Błąd zapisu', 'Nie udało się zapisać wyboru opcji'\);/g, "showApiError(error);"],
  [/showError\('Błąd', 'Nie udało się zmienić widoczności kolumny'\);/g, "showApiError(err);"],
  [/showError\('Błąd', 'Nie udało się dodać kolumny'\);/g, "showApiError(err);"],
  [/showError\('Błąd usuwania', 'Nie udało się usunąć pliku'\);/g, "showApiError(err);"],
  [/showError\('Błąd uploadu', 'Nie udało się przesłać plików'\);/g, "showApiError(err);"],
  [
    /showError\(\s*'Błąd zapisu',\s*err instanceof Error \? err\.message : 'Nie udało się zapisać kosztorysu'\s*\);/g,
    "showApiError(err);",
  ],
  [
    /showError\(\s*'Błąd przeliczania',\s*'Nie udało się przeliczyć kosztorysu\. Spróbuj ponownie\.'\s*\);/g,
    "showApiError(err);",
  ],
];

for (const [re, repl] of apiErrorPatterns) {
  content = content.replace(re, repl);
}

content = content.replace(/showError,/g, "showApiError,");
content = content.replace(/, showError\]/g, ", showApiError]");
content = content.replace(/, showError\)/g, ", showApiError)");

// Fix sync catch - was catch { without error variable
content = content.replace(
  /} catch \{\s*\n\s*showApiError\(error\);/g,
  "} catch (error) {\n      showApiError(error);",
);

fs.writeFileSync(file, content);
console.log("CostEstimateEditPage migrated");

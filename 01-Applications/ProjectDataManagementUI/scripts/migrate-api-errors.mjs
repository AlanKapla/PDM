import fs from "fs";
import path from "path";

function walk(dir, files = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory() && entry.name !== "node_modules") walk(full, files);
    else if (entry.isFile() && /\.(tsx?|jsx?)$/.test(entry.name)) files.push(full);
  }
  return files;
}

const src = path.join(process.cwd(), "src");
const files = walk(src);
let changed = 0;

const patterns = [
  [/const \{ title, description \} = handleApiError\((\w+)\);\s*\n\s*showError\(title, description\);/g, "showApiError($1);"],
  [/const \{ title: errTitle, description \} = handleApiError\((\w+)\);\s*\n\s*showError\(errTitle, description\);/g, "showApiError($1);"],
  [/const \{ title: t, description \} = handleApiError\((\w+)\);\s*\n\s*showError\(t, description\);/g, "showApiError($1);"],
];

for (const file of files) {
  if (file.includes("handleApiError.ts") || file.includes("handleApiError.test.ts")) continue;
  let content = fs.readFileSync(file, "utf8");
  const original = content;
  for (const [re, repl] of patterns) {
    content = content.replace(re, repl);
  }
  if (content !== original) {
    if (content.includes("showApiError(") && content.includes("useToastNotification")) {
      content = content.replace(/const \{([^}]+)\} = useToastNotification\(\);/g, (m, inner) => {
        if (inner.includes("showApiError")) return m;
        return `const {${inner.trim()}, showApiError } = useToastNotification();`;
      });
    }
    if (!content.includes("handleApiError(")) {
      content = content.replace(/import \{ handleApiError \} from ["'][^"']+handleApiError["'];\n/g, "");
    }
    fs.writeFileSync(file, content);
    changed++;
    console.log("updated:", path.relative(process.cwd(), file));
  }
}
console.log("Total:", changed);

import type { ProjectFilePackageWeb } from "../types/project.types";

export interface FlatCatalogOption {
  id: string;
  label: string;
  depth: number;
}

export function flattenCatalogsForSelect(
  catalogs: ProjectFilePackageWeb[],
  depth = 0
): FlatCatalogOption[] {
  const result: FlatCatalogOption[] = [];
  for (const cat of catalogs) {
    const prefix = depth === 0 ? "" : "  ".repeat(depth) + "└─ ";
    result.push({ id: cat.id, label: prefix + cat.name, depth });
    if (cat.subCatalogs?.length) {
      result.push(...flattenCatalogsForSelect(cat.subCatalogs, depth + 1));
    }
  }
  return result;
}

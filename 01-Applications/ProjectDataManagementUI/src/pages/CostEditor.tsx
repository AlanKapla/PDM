import {
  ModuleRegistry,
  AllCommunityModule,
} from "ag-grid-community";
ModuleRegistry.registerModules([AllCommunityModule]);

// =============================================================
// IMPORTY
// =============================================================
import { useMemo, useState, useRef } from "react";
import {
  Box,
  Heading,
  VStack,
  HStack,
  Checkbox,
  Divider,
  Button,
  Text,
  Select,
} from "@chakra-ui/react";
import MainLayout from "../layout/MainLayout";

import { AgGridReact } from "ag-grid-react";
import type {
  ColDef,
  CellValueChangedEvent,
  ICellRendererParams,
} from "ag-grid-community";

import * as XLSX from "xlsx";

import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-alpine.css";

// =============================================================
// TYPY
// =============================================================
type CostCategory = "materials" | "labor" | "equipment";
type TemplateId =
  | "full"
  | "materialsOnly"
  | "laborOnly"
  | "equipmentOnly";

interface CostRow {
  id: string;
  category: CostCategory;

  name?: string;
  unit?: string;
  quantity?: number;
  unitNetPrice?: number;
  vat?: number;

  workerCategory?: string;
  hourRate?: number;
  hours?: number;

  equipmentType?: string;
  equipmentRate?: number;
  equipmentHours?: number;

  [key: string]: any;
}

// Jednostki dla SELECT
const UNITS = ["szt.", "m", "m²", "m³", "kg", "t", "godz.", "dni"];

// =============================================================
// DEFINICJE POL
// =============================================================
const FIELD_DEFS = {
  materials: [
    { key: "name", label: "Nazwa pozycji" },
    { key: "unit", label: "Jednostka" },
    { key: "quantity", label: "Ilość" },
    { key: "unitNetPrice", label: "Cena netto" },
    { key: "netValue", label: "Wartość netto" },
    { key: "vat", label: "VAT (%)" },
    { key: "grossValue", label: "Wartość brutto" },
  ],
  labor: [
    { key: "workerCategory", label: "Kategoria pracownika" },
    { key: "hourRate", label: "Stawka/h" },
    { key: "hours", label: "Godziny" },
    { key: "laborCost", label: "Koszt robocizny" },
  ],
  equipment: [
    { key: "equipmentType", label: "Typ sprzętu" },
    { key: "equipmentRate", label: "Stawka sprzętu/h" },
    { key: "equipmentHours", label: "Godziny sprzętu" },
    { key: "equipmentCost", label: "Koszt sprzętu" },
  ],
} as const;

const FIELD_LABELS: Record<string, string> = {};
(["materials", "labor", "equipment"] as CostCategory[]).forEach(
  (cat) =>
    FIELD_DEFS[cat].forEach(
      (f) => (FIELD_LABELS[f.key] = f.label)
    )
);

// =============================================================
// SZABLONY
// =============================================================
const TEMPLATES: Record<
  TemplateId,
  Record<CostCategory, string[]>
> = {
  full: {
    materials: [
      "name",
      "unit",
      "quantity",
      "unitNetPrice",
      "netValue",
      "vat",
      "grossValue",
    ],
    labor: ["workerCategory", "hourRate", "hours", "laborCost"],
    equipment: [
      "equipmentType",
      "equipmentRate",
      "equipmentHours",
      "equipmentCost",
    ],
  },
  materialsOnly: {
    materials: [
      "name",
      "unit",
      "quantity",
      "unitNetPrice",
      "netValue",
      "vat",
      "grossValue",
    ],
    labor: [],
    equipment: [],
  },
  laborOnly: {
    materials: [],
    labor: ["workerCategory", "hourRate", "hours", "laborCost"],
    equipment: [],
  },
  equipmentOnly: {
    materials: [],
    labor: [],
    equipment: [
      "equipmentType",
      "equipmentRate",
      "equipmentHours",
      "equipmentCost",
    ],
  },
};

// =============================================================
// HELPERS
// =============================================================
const numberParser = (value: any): number | undefined => {
  if (value === "" || value === null || value === undefined)
    return undefined;
  const n = Number(value);
  return Number.isNaN(n) ? undefined : n;
};

// =============================================================
// KOMPONENT
// =============================================================
export default function CostEditor() {
  const gridRef = useRef<AgGridReact>(null);

  const [gridKey, setGridKey] = useState(0);
  const [selectedTemplate, setSelectedTemplate] =
    useState<TemplateId>("full");

  const [selectedColumns, setSelectedColumns] = useState<
    Record<CostCategory, string[]>
  >(TEMPLATES.full);

  const [rowData, setRowData] = useState<CostRow[]>([
    {
      id: crypto.randomUUID(),
      category: "materials",
      name: "Pozycja 1",
      vat: 23,
    },
  ]);

  // Widoczne pola (checkboxy)
  const visibleFields = useMemo(() => {
    const set = new Set<string>();
    (Object.keys(selectedColumns) as CostCategory[]).forEach(
      (cat) => selectedColumns[cat].forEach((k) => set.add(k))
    );
    return set;
  }, [selectedColumns]);

  // =============================================================
  // BUDOWANIE KOLUMN
  // =============================================================
  const columnDefs = useMemo<ColDef<CostRow>[]>(() => {
    const cols: ColDef<CostRow>[] = [];

    // -------------------------
    // Lp.
    // -------------------------
    cols.push({
      headerName: "Lp.",
      pinned: "left",
      width: 60,
      valueGetter: (p) => (p.node?.rowIndex ?? 0) + 1,
    });

    // -------------------------
    // Kategoria
    // -------------------------
    cols.push({
      headerName: "Kategoria",
      field: "category",
      pinned: "left",
      width: 140,
      editable: true,
      cellEditor: "agSelectCellEditor",
      cellEditorParams: {
        values: ["materials", "labor", "equipment"],
      },
      valueFormatter: (p) =>
        p.value === "materials"
          ? "Materiały"
          : p.value === "labor"
            ? "Robocizna"
            : p.value === "equipment"
              ? "Sprzęt"
              : "",
    });

    // -------------------------
    // Nazwa
    // -------------------------
    if (visibleFields.has("name")) {
      cols.push({
        headerName: FIELD_LABELS["name"] ?? "Nazwa",
        field: "name",
        colId: "name",
        editable: true,
      });
    }


    // -------------------------
    // POZOSTAŁE POLA
    // -------------------------
    const allKeys = new Set<string>();
    (Object.keys(FIELD_DEFS) as CostCategory[]).forEach((cat) =>
      FIELD_DEFS[cat].forEach((f) => f.key !== "name" && allKeys.add(f.key))
    );

    allKeys.forEach((key) => {
      if (!visibleFields.has(key)) return;

      const col: ColDef<CostRow> = {
        headerName: FIELD_LABELS[key],
        field: key,
        colId: key,
      };


      // Jednostka (select)
      if (key === "unit") {
        col.editable = true;
        col.cellEditor = "agSelectCellEditor";
        col.cellEditorParams = { values: UNITS };
      }

      // Pola obliczane
      else if (key === "netValue") {
        col.editable = false;
        col.valueGetter = (p) =>
          (p.data?.quantity ?? 0) * (p.data?.unitNetPrice ?? 0);
        col.valueFormatter = (p) => (p.value || 0).toFixed(2);
      } else if (key === "grossValue") {
        col.editable = false;
        col.valueGetter = (p) => {
          const net =
            (p.data?.quantity ?? 0) *
            (p.data?.unitNetPrice ?? 0);
          return net * (1 + (p.data?.vat ?? 23) / 100);
        };
        col.valueFormatter = (p) => (p.value || 0).toFixed(2);
      } else if (key === "laborCost") {
        col.editable = false;
        col.valueGetter = (p) =>
          (p.data?.hourRate ?? 0) * (p.data?.hours ?? 0);
        col.valueFormatter = (p) => (p.value || 0).toFixed(2);
      } else if (key === "equipmentCost") {
        col.editable = false;
        col.valueGetter = (p) =>
          (p.data?.equipmentRate ?? 0) *
          (p.data?.equipmentHours ?? 0);
        col.valueFormatter = (p) => (p.value || 0).toFixed(2);
      }

      // Pola liczbowe
      else if (
        key === "quantity" ||
        key === "unitNetPrice" ||
        key === "vat" ||
        key === "hourRate" ||
        key === "hours" ||
        key === "equipmentRate" ||
        key === "equipmentHours"
      ) {
        col.editable = true;
        col.valueParser = (p) => numberParser(p.newValue);
      }

      // normal editable
      else {
        col.editable = true;
      }

      cols.push(col);
    });

    // ==== POPRAWIONA LOGIKA SAFE COLUMN ====
    // Liczymy tylko kolumny UŻYTKOWNIKA (nie Lp., nie Kategoria, nie fallback, nie Delete)
    const userVisibleColumnCount = cols.filter((c) => {
      const f = c.field;
      const h = c.headerName;

      return (
        f !== "__fallback" &&
        h !== "Lp." &&
        h !== "Kategoria" &&
        h !== "" && // nie liczymy kolumny Usuń
        !c.hide
      );
    }).length;

    // Jeśli użytkownik odznaczy WSZYSTKIE pola → dodaj SAFE COLUMN
    if (userVisibleColumnCount === 0) {
      cols.push({
        headerName: "Brak pól",
        field: "__safe",
        editable: false,
        valueGetter: () => "",
        width: 150,
      });
    }


    // =============================================================
    // FALLBACK COLUMN (ukryta)
    // =============================================================
    cols.push({
      headerName: "_fallback",
      field: "__fallback",
      hide: true,
    });

    // =============================================================
    // Usuń
    // =============================================================
    function DeleteButtonRenderer(p: ICellRendererParams<CostRow>) {
      if (!p.data) return null;

      return (
        <button
          onClick={() => {
            setRowData((prev) => prev.filter((row) => row.id !== p.data!.id));
          }}
          style={{
            cursor: "pointer",
            padding: "4px 8px",
            background: "#ff4d4f",
            color: "white",
            border: "none",
            borderRadius: "4px",
          }}
        >
          Usuń
        </button>
      );
    }


    cols.push({
      headerName: "",
      width: 80,
      cellRenderer: DeleteButtonRenderer,
    });

    return cols;
  }, [visibleFields]);

  // =============================================================
  // ZMIANA WARTOŚCI W KOMPÓRCE
  // =============================================================
  const onCellValueChanged = (e: CellValueChangedEvent<CostRow>) => {
    const updated = e.data;

    setRowData((prev) =>
      prev.map((r) => (r.id === updated.id ? { ...updated } : r))
    );
  };

  // =============================================================
  // DODAWANIE WIERSZA
  // =============================================================
  const addRow = () => {
    const nextCategory: CostCategory =
      selectedColumns.materials.length > 0
        ? "materials"
        : selectedColumns.labor.length > 0
          ? "labor"
          : "equipment";

    setRowData((prev) => [
      ...prev,
      {
        id: crypto.randomUUID(),
        category: nextCategory,
        name: "",
        vat: 23,
      },
    ]);
  };


  // =============================================================
  // SUMY KOŃCOWE
  // =============================================================
  const totals = useMemo(() => {
    let netTotal = 0;
    let grossTotal = 0;
    let laborTotal = 0;
    let equipmentTotal = 0;

    rowData.forEach((r) => {
      const q = r.quantity ?? 0;
      const up = r.unitNetPrice ?? 0;
      const vat = r.vat ?? 23;

      const net = q * up;
      const gross = net * (1 + vat / 100);

      netTotal += net;
      grossTotal += gross;

      laborTotal += (r.hourRate ?? 0) * (r.hours ?? 0);
      equipmentTotal += (r.equipmentRate ?? 0) * (r.equipmentHours ?? 0);
    });

    return { netTotal, grossTotal, laborTotal, equipmentTotal };
  }, [rowData]);

  // =============================================================
  // WYBÓR SZABLONU
  // =============================================================
  const applyTemplate = (templateId: TemplateId) => {
    setSelectedTemplate(templateId);
    const template = TEMPLATES[templateId];

    setSelectedColumns(template);

    // HARD REFRESH GRID
    setGridKey((k) => k + 1);

    setTimeout(() => {
      const api = gridRef.current?.api;
      if (api) {
        api.setGridOption("columnDefs", [...columnDefs]);
        api.refreshClientSideRowModel("everything");
        api.redrawRows();
      }
    }, 80);
  };

  // =============================================================
  // EXPORT EXCEL (ładniejszy, z nazwami kolumn)
  // =============================================================
  const exportExcel = () => {
    const api = gridRef.current?.api;
    if (!api) return;

    const data = rowData.map((row) => {
      const result: any = {};

      columnDefs.forEach((col) => {
        if (!col.field || col.field === "__fallback") return;
        result[col.headerName ?? col.field] = row[col.field] ?? "";
      });

      return result;
    });

    const worksheet = XLSX.utils.json_to_sheet(data);
    const workbook = XLSX.utils.book_new();

    XLSX.utils.book_append_sheet(workbook, worksheet, "Kosztorys");
    XLSX.writeFile(workbook, `kosztorys.xlsx`);
  };

  // =============================================================
  // ZMIANA WIDOCZNOŚCI POLA
  // =============================================================
  const toggleField = (category: CostCategory, key: string) => {
    setSelectedColumns((prev) => {
      const arr = prev[category];
      const exists = arr.includes(key);
      const next = exists ? arr.filter((k) => k !== key) : [...arr, key];

      return {
        ...prev,
        [category]: next,
      };
    });

    // Hard refresh (bez tego AG Grid crashuje)
    setGridKey((k) => k + 1);

    setTimeout(() => {
      const api = gridRef.current?.api;
      if (api) {
        api.setGridOption("columnDefs", [...columnDefs]);
        api.refreshClientSideRowModel("everything");
        api.redrawRows();
      }
    }, 80);
  };

  // =============================================================
  // RENDER
  // =============================================================
  return (
    <MainLayout>
      <Box p={6}>
        <Heading size="md" mb={4}>
          Kosztorys
        </Heading>

        {/* SZABLONY */}
        <HStack mb={4} spacing={4}>
          <Select
            width="250px"
            value={selectedTemplate}
            onChange={(e) => applyTemplate(e.target.value as TemplateId)}
          >
            <option value="full">Pełny kosztorys</option>
            <option value="materialsOnly">Tylko materiały</option>
            <option value="laborOnly">Tylko robocizna</option>
            <option value="equipmentOnly">Tylko sprzęt</option>
          </Select>

          <Button colorScheme="blue" onClick={addRow}>
            Dodaj wiersz
          </Button>

          <Button colorScheme="green" onClick={exportExcel}>
            Eksport do Excel
          </Button>
        </HStack>

        {/* PANEL POL */}
        <HStack align="flex-start" spacing={10} mb={4}>
          <VStack align="flex-start">
            <Text fontWeight="bold">Materiały</Text>
            {FIELD_DEFS.materials.map((f) => (
              <Checkbox
                key={f.key}
                isChecked={selectedColumns.materials.includes(f.key)}
                onChange={() => toggleField("materials", f.key)}
              >
                {f.label}
              </Checkbox>
            ))}
          </VStack>

          <VStack align="flex-start">
            <Text fontWeight="bold">Robocizna</Text>
            {FIELD_DEFS.labor.map((f) => (
              <Checkbox
                key={f.key}
                isChecked={selectedColumns.labor.includes(f.key)}
                onChange={() => toggleField("labor", f.key)}
              >
                {f.label}
              </Checkbox>
            ))}
          </VStack>

          <VStack align="flex-start">
            <Text fontWeight="bold">Sprzęt</Text>
            {FIELD_DEFS.equipment.map((f) => (
              <Checkbox
                key={f.key}
                isChecked={selectedColumns.equipment.includes(f.key)}
                onChange={() => toggleField("equipment", f.key)}
              >
                {f.label}
              </Checkbox>
            ))}
          </VStack>
        </HStack>

        <Divider mb={4} />

        {/* TABELA */}
        <Box className="ag-theme-alpine" style={{ height: 500, width: "100%" }}>
          <AgGridReact<CostRow>
            theme="legacy"
            key={gridKey}
            ref={gridRef}
            rowData={rowData}
            columnDefs={columnDefs}
            getRowId={(p) => p.data.id}
            onCellValueChanged={onCellValueChanged}
            suppressMovableColumns={false}
            enableCellTextSelection
            stopEditingWhenCellsLoseFocus
          />
        </Box>

        {/* SUMY */}
        <Box mt={6}>
          <Text>Materiały netto: {totals.netTotal.toFixed(2)} zł</Text>
          <Text>Materiały brutto: {totals.grossTotal.toFixed(2)} zł</Text>
          <Text>Robocizna: {totals.laborTotal.toFixed(2)} zł</Text>
          <Text>Sprzęt: {totals.equipmentTotal.toFixed(2)} zł</Text>
        </Box>
      </Box>
    </MainLayout>
  );
}


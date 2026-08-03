import { describe, expect, it } from "vitest";
import { detectSameDayWorkConflicts } from "./detectSameDayWorkConflicts";
import type { UserAssignedWorkWeb } from "../types/workSchedule.types";

function work(
  name: string,
  periods: Array<{ start: string; end: string; closed?: boolean }>,
  isClosed = false
): UserAssignedWorkWeb {
  return {
    workId: name,
    workName: name,
    workOrder: 0,
    colorRgb: "#000",
    isClosed,
    periods: periods.map((p, i) => ({
      id: `${name}-${i}`,
      startDate: p.start,
      endDate: p.end,
      isClosed: p.closed ?? false,
    })),
    comments: [],
  };
}

describe("detectSameDayWorkConflicts", () => {
  it("returns empty when works do not overlap on any day", () => {
    const result = detectSameDayWorkConflicts([
      work("A", [{ start: "2026-07-01", end: "2026-07-02" }]),
      work("B", [{ start: "2026-07-03", end: "2026-07-04" }]),
    ]);
    expect(result).toEqual([]);
  });

  it("returns one entry per work for single-day overlap", () => {
    const result = detectSameDayWorkConflicts([
      work("Fundamenty", [{ start: "2026-07-01", end: "2026-07-05" }]),
      work("Zbrojenie", [{ start: "2026-07-05", end: "2026-07-08" }]),
    ]);
    expect(result).toHaveLength(2);
    expect(result).toEqual([
      {
        workName: "Fundamenty",
        rangeStartKey: "2026-07-05",
        rangeEndKey: "2026-07-05",
        rangeLabel: "05.07.2026",
        lineLabel: "Fundamenty: 05.07.2026",
      },
      {
        workName: "Zbrojenie",
        rangeStartKey: "2026-07-05",
        rangeEndKey: "2026-07-05",
        rangeLabel: "05.07.2026",
        lineLabel: "Zbrojenie: 05.07.2026",
      },
    ]);
  });

  it("returns one entry per work for multi-day overlap", () => {
    const result = detectSameDayWorkConflicts([
      work("A", [{ start: "2026-07-01", end: "2026-07-10" }]),
      work("B", [{ start: "2026-07-03", end: "2026-07-07" }]),
    ]);
    expect(result).toHaveLength(2);
    expect(result[0]).toMatchObject({
      workName: "A",
      rangeStartKey: "2026-07-03",
      rangeEndKey: "2026-07-07",
    });
    expect(result[1]).toMatchObject({
      workName: "B",
      rangeStartKey: "2026-07-03",
      rangeEndKey: "2026-07-07",
    });
  });

  it("ignores closed works and closed periods", () => {
    const result = detectSameDayWorkConflicts([
      work("A", [{ start: "2026-07-01", end: "2026-07-03" }], true),
      work("B", [{ start: "2026-07-01", end: "2026-07-03", closed: true }]),
      work("C", [{ start: "2026-07-01", end: "2026-07-03" }]),
    ]);
    expect(result).toEqual([]);
  });

  it("returns separate ranges per work when conflict days are not continuous", () => {
    const result = detectSameDayWorkConflicts([
      work("A", [
        { start: "2026-07-01", end: "2026-07-02" },
        { start: "2026-07-05", end: "2026-07-06" },
      ]),
      work("B", [
        { start: "2026-07-01", end: "2026-07-02" },
        { start: "2026-07-05", end: "2026-07-06" },
      ]),
    ]);
    expect(result).toHaveLength(4);
    expect(result).toEqual([
      {
        workName: "A",
        rangeStartKey: "2026-07-01",
        rangeEndKey: "2026-07-02",
        rangeLabel: "01.07.2026 – 02.07.2026",
        lineLabel: "A: 01.07.2026 – 02.07.2026",
      },
      {
        workName: "B",
        rangeStartKey: "2026-07-01",
        rangeEndKey: "2026-07-02",
        rangeLabel: "01.07.2026 – 02.07.2026",
        lineLabel: "B: 01.07.2026 – 02.07.2026",
      },
      {
        workName: "A",
        rangeStartKey: "2026-07-05",
        rangeEndKey: "2026-07-06",
        rangeLabel: "05.07.2026 – 06.07.2026",
        lineLabel: "A: 05.07.2026 – 06.07.2026",
      },
      {
        workName: "B",
        rangeStartKey: "2026-07-05",
        rangeEndKey: "2026-07-06",
        rangeLabel: "05.07.2026 – 06.07.2026",
        lineLabel: "B: 05.07.2026 – 06.07.2026",
      },
    ]);
  });

  it("returns shorter range for work with narrower conflict window among three works", () => {
    const result = detectSameDayWorkConflicts([
      work("A", [{ start: "2026-07-01", end: "2026-07-10" }]),
      work("B", [{ start: "2026-07-03", end: "2026-07-08" }]),
      work("C", [{ start: "2026-07-05", end: "2026-07-06" }]),
    ]);
    expect(result).toHaveLength(3);
    expect(result.find((item) => item.workName === "A")).toMatchObject({
      rangeStartKey: "2026-07-03",
      rangeEndKey: "2026-07-08",
    });
    expect(result.find((item) => item.workName === "B")).toMatchObject({
      rangeStartKey: "2026-07-03",
      rangeEndKey: "2026-07-08",
    });
    expect(result.find((item) => item.workName === "C")).toMatchObject({
      rangeStartKey: "2026-07-05",
      rangeEndKey: "2026-07-06",
    });
  });
});

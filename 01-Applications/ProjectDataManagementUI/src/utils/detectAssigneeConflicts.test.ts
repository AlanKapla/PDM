import { describe, expect, it } from "vitest";
import {
  detectAssigneeConflicts,
  formatAssigneeConflictTooltip,
} from "./detectAssigneeConflicts";
import type { WorkScheduleAssigneeBusyPeriodWeb } from "../types/workSchedule.types";

const busy = (
  workId: string,
  startDate: string,
  endDate: string
): WorkScheduleAssigneeBusyPeriodWeb => ({
  workId,
  workName: `Work ${workId}`,
  workScheduleId: "ws-1",
  workScheduleName: "HS",
  projectId: "p-1",
  projectName: "Projekt",
  startDate,
  endDate,
});

describe("detectAssigneeConflicts", () => {
  it("returns conflict when busy period overlaps current work", () => {
    const result = detectAssigneeConflicts({
      workId: "current",
      workPeriods: [{ startDate: "2026-07-01", endDate: "2026-07-10", isClosed: false }],
      candidates: [
        {
          userId: "u1",
          assigneeName: "Jan Kowalski",
          assignments: [busy("other", "2026-07-05", "2026-07-15")],
        },
      ],
    });

    expect(result).toHaveLength(1);
    expect(result[0].assigneeName).toBe("Jan Kowalski");
    expect(result[0].conflictingWorkId).toBe("other");
    expect(result[0].overlapStart.slice(0, 10)).toBe("2026-07-05");
    expect(result[0].overlapEnd.slice(0, 10)).toBe("2026-07-10");
  });

  it("ignores assignments to the same work", () => {
    const result = detectAssigneeConflicts({
      workId: "current",
      workPeriods: [{ startDate: "2026-07-01", endDate: "2026-07-10", isClosed: false }],
      candidates: [
        {
          userId: "u1",
          assigneeName: "Jan",
          assignments: [busy("current", "2026-07-01", "2026-07-10")],
        },
      ],
    });

    expect(result).toEqual([]);
  });

  it("returns empty when periods do not overlap", () => {
    const result = detectAssigneeConflicts({
      workId: "current",
      workPeriods: [{ startDate: "2026-07-01", endDate: "2026-07-03", isClosed: false }],
      candidates: [
        {
          contractorId: "c1",
          assigneeName: "Firma",
          assignments: [busy("other", "2026-07-10", "2026-07-12")],
        },
      ],
    });

    expect(result).toEqual([]);
  });

  it("ignores fully closed periods of current work", () => {
    const result = detectAssigneeConflicts({
      workId: "current",
      workPeriods: [
        { startDate: "2026-07-01", endDate: "2026-07-10", isClosed: true },
        { startDate: "2026-07-15", endDate: "2026-07-20", isClosed: true },
      ],
      candidates: [
        {
          userId: "u1",
          assigneeName: "Jan",
          assignments: [busy("other", "2026-07-05", "2026-07-18")],
        },
      ],
    });

    expect(result).toEqual([]);
  });

  it("checks only open periods when work has mixed closed and open ranges", () => {
    const result = detectAssigneeConflicts({
      workId: "current",
      workPeriods: [
        { startDate: "2026-07-01", endDate: "2026-07-10", isClosed: true },
        { startDate: "2026-07-15", endDate: "2026-07-20", isClosed: false },
      ],
      candidates: [
        {
          userId: "u1",
          assigneeName: "Jan",
          assignments: [busy("other", "2026-07-05", "2026-07-18")],
        },
      ],
    });

    expect(result).toHaveLength(1);
    expect(result[0].overlapStart.slice(0, 10)).toBe("2026-07-15");
    expect(result[0].overlapEnd.slice(0, 10)).toBe("2026-07-18");
  });
});

describe("formatAssigneeConflictTooltip", () => {
  it("formats conflict lines like assigned works alert", () => {
    const text = formatAssigneeConflictTooltip([
      {
        userId: "u1",
        contractorId: null,
        assigneeName: "Jan",
        conflictingWorkId: "w2",
        conflictingWorkName: "Fundamenty",
        conflictingWorkScheduleId: "ws",
        conflictingWorkScheduleName: "HS",
        conflictingProjectId: "p",
        conflictingProjectName: "Budowa",
        overlapStart: "2026-07-05",
        overlapEnd: "2026-07-10",
      },
    ]);
    expect(text).toContain("Fundamenty: 05.07.2026 – 10.07.2026");
    expect(text).not.toContain("Budowa");
    expect(text).toContain("Już przypisany");
  });
});

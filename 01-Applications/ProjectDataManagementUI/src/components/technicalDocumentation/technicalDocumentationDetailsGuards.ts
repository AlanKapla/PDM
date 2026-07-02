import type { ProjectTechnicalDocumentationDetailsWeb } from '../../types/technicalDocumentation.types';

export function isNewFormatTechnicalDocumentationDetails(
  details: ProjectTechnicalDocumentationDetailsWeb
): boolean {
  return details.projectModel !== undefined;
}

export function isLegacyTechnicalDocumentationDetails(
  details: ProjectTechnicalDocumentationDetailsWeb
): boolean {
  return details.projectModel === undefined
    && (details.project !== undefined || (details.rooms?.length ?? 0) > 0);
}

import React from 'react';
import { Badge, Link as ChakraLink } from '@chakra-ui/react';
import { Link } from 'react-router-dom';
import { usePendingAICostImportCountByType } from '../../hooks/usePendingAICostImports';
import {
  getAICostReviewPath,
  getCostDocumentTypeForContext,
  type AICostReviewContext,
} from '../../utils/aiCostReviewPaths';

export interface AICostPendingBadgeProps {
  tenantId: string | undefined;
  projectId: string | undefined;
  context: AICostReviewContext;
}

export function AICostPendingBadge({
  tenantId,
  projectId,
  context,
}: AICostPendingBadgeProps): React.ReactElement | null {
  const costDocumentType = getCostDocumentTypeForContext(context);
  const data = usePendingAICostImportCountByType(tenantId, projectId, costDocumentType);

  const reviewCount = data.pendingCount + data.errorCount + data.duplicateCount;
  if (reviewCount === 0 || !projectId) {
    return null;
  }

  const reviewPath = getAICostReviewPath(projectId, context);

  return (
    <ChakraLink as={Link} to={reviewPath} _hover={{ textDecoration: 'none' }}>
      <Badge
        colorScheme="purple"
        variant="solid"
        px={3}
        py={1}
        borderRadius="full"
        fontSize="xs"
        cursor="pointer"
      >
        {reviewCount} do weryfikacji AI
      </Badge>
    </ChakraLink>
  );
}

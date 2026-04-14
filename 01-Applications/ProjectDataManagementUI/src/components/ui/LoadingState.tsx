import { memo } from "react";
import { VStack, Skeleton, SkeletonText, Box } from "@chakra-ui/react";

interface LoadingStateProps {
  /** Liczba wierszy skeletona, domyślnie 5 */
  rows?: number;
  /** Wysokość pojedynczego wiersza w px, domyślnie 40 */
  rowHeight?: number;
  /** Czy wyświetlić skeleton tekstowy (kilka linii), domyślnie false */
  textMode?: boolean;
}

/**
 * Komponent stanu ładowania dla tabel i list.
 * Wyświetla animowane Skeleton o tej samej wysokości co docelowa zawartość.
 */
const LoadingState = memo(function LoadingState({
  rows = 5,
  rowHeight = 40,
  textMode = false,
}: LoadingStateProps) {
  if (textMode) {
    return (
      <Box px={4} py={2}>
        <SkeletonText mt={2} noOfLines={rows} spacing={3} skeletonHeight={4} />
      </Box>
    );
  }

  return (
    <VStack spacing={2} align="stretch" px={0} py={2}>
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} height={`${rowHeight}px`} borderRadius="md" />
      ))}
    </VStack>
  );
});

export default LoadingState;

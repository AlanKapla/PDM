import React from "react";
import { Box, IconButton, useColorModeValue } from "@chakra-ui/react";
import { ChevronLeft, ChevronRight } from "lucide-react";

export interface GanttTimelineNavigationProps {
  onNavigatePrev: () => void;
  onNavigateNext: () => void;
}

export function GanttTimelineNavigation({
  onNavigatePrev,
  onNavigateNext,
}: GanttTimelineNavigationProps): React.ReactElement {
  const buttonBg = useColorModeValue("whiteAlpha.900", "gray.800");
  const buttonBorder = useColorModeValue("gray.200", "gray.600");
  const buttonShadow = useColorModeValue("md", "dark-lg");

  return (
    <>
      <Box
        position="absolute"
        left={2}
        top="50%"
        transform="translateY(-50%)"
        zIndex={25}
        pointerEvents="none"
      >
        <IconButton
          aria-label="Poprzedni tydzień"
          icon={<ChevronLeft size={18} aria-hidden="true" />}
          size="sm"
          variant="outline"
          bg={buttonBg}
          borderColor={buttonBorder}
          boxShadow={buttonShadow}
          pointerEvents="auto"
          onClick={onNavigatePrev}
        />
      </Box>

      <Box
        position="absolute"
        right={2}
        top="50%"
        transform="translateY(-50%)"
        zIndex={25}
        pointerEvents="none"
      >
        <IconButton
          aria-label="Następny tydzień"
          icon={<ChevronRight size={18} aria-hidden="true" />}
          size="sm"
          variant="outline"
          bg={buttonBg}
          borderColor={buttonBorder}
          boxShadow={buttonShadow}
          pointerEvents="auto"
          onClick={onNavigateNext}
        />
      </Box>
    </>
  );
}

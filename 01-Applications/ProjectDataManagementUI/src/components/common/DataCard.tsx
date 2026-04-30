import { memo } from "react";
import { Box, useColorModeValue } from "@chakra-ui/react";
import type { BoxProps } from "@chakra-ui/react";

interface DataCardProps extends BoxProps {
  children: React.ReactNode;
  hoverable?: boolean;
}

const DataCard = memo(function DataCard({ 
  children, 
  hoverable = false,
  ...props 
}: DataCardProps) {
  const bg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");

  return (
    <Box
      bg={bg}
      border="1px"
      borderColor={borderColor}
      borderRadius="md"
      p={4}
      transition="all 0.2s"
      _hover={hoverable ? { bg: hoverBg } : undefined}
      {...props}
    >
      {children}
    </Box>
  );
});

export default DataCard;

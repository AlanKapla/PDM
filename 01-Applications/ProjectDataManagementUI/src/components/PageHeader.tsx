import { HStack, Heading, Text, Box, useBreakpointValue } from "@chakra-ui/react";
import React from "react";

export interface PageHeaderProps {
  title: string;
  breadcrumb?: string[];
  icon?: React.ComponentType<{ size?: number }>;
}

export function PageHeader({ title, breadcrumb, icon: Icon }: PageHeaderProps) {
  const iconSize = useBreakpointValue({ base: 20, md: 28 }) || 20;
  return (
    <Box mb={{ base: 4, md: 6 }} px={{ base: 4, md: 6 }}>
      {/* BREADCRUMB */}
      {breadcrumb && (
        <Text fontSize={{ base: "xs", md: "sm" }} color="gray.500" mb={1}>
          {breadcrumb.join(" / ")}
        </Text>
      )}

      {/* HEADER */}
      <HStack spacing={{ base: 2, md: 3 }} align="center">
        {Icon && <Icon size={iconSize} />}
        <Heading size={{ base: "md", md: "lg" }}>{title}</Heading>
      </HStack>
    </Box>
  );
}

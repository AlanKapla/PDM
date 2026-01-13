import { HStack, Heading, Text, Box } from "@chakra-ui/react";
import type { LucideIcon } from "lucide-react";

export interface PageHeaderProps {
  title: string;
  breadcrumb?: string[];
  icon?: LucideIcon; // ← DODAJ TO
}

export function PageHeader({ title, breadcrumb, icon: Icon }: PageHeaderProps) {
  return (
    <Box mb={6}>
      {/* BREADCRUMB */}
      {breadcrumb && (
        <Text fontSize="xs" color="gray.500" mb={1}>
          {breadcrumb.join(" / ")}
        </Text>
      )}

      {/* HEADER */}
      <HStack spacing={3} align="center">
        {Icon && <Icon size={28} />}
        <Heading size="lg">{title}</Heading>
      </HStack>
    </Box>
  );
}

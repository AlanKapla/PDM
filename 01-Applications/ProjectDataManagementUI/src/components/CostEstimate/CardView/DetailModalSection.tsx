import React from 'react';
import { Box, Text, VStack } from '@chakra-ui/react';

interface DetailModalSectionProps {
  title: string;
  children: React.ReactNode;
}

export const DetailModalSection: React.FC<DetailModalSectionProps> = ({
  title,
  children,
}) => {
  return (
    <Box
      border="1px solid"
      borderColor="neutral.200"
      borderRadius="12px"
      bg="neutral.25"
      px={5}
      py={4}
    >
      <Text
        fontSize="md"
        fontWeight="semibold"
        color="neutral.700"
        mb={4}
        pb={2}
        borderBottom="1px solid"
        borderColor="neutral.200"
      >
        {title}
      </Text>
      <VStack spacing={4} align="stretch">
        {children}
      </VStack>
    </Box>
  );
};

import {
  Box,
  Text,
  useColorModeValue,
} from "@chakra-ui/react";

export default function Header() {
  const bg = useColorModeValue("white", "gray.800");
  const border = useColorModeValue("gray.200", "gray.700");
  const textColor = useColorModeValue("gray.700", "gray.200");

  return (
    <Box
      bg={bg}
      borderBottom="1px solid"
      borderColor={border}
      px={{ base: 4, md: 6 }}
      py={3}
      position="sticky"
      top={0}
      zIndex={10}
    >
      <Box maxW="1400px" mx="auto">
        <Text fontSize="lg" fontWeight="bold" color={textColor}>
          Project Data Management
        </Text>
      </Box>
    </Box>
  );
}

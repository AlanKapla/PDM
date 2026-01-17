import { useContext } from "react";
import {
  Box,
  HStack,
  Avatar,
  Text,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  MenuDivider,
  useColorModeValue,
} from "@chakra-ui/react";
import { User as UserIcon, RefreshCw } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";

export default function HeaderUserMenu() {
  const { user, logout } = useContext(AuthContext);
  const navigate = useNavigate();
  const bg = useColorModeValue("white", "gray.800");
  const border = useColorModeValue("gray.200", "gray.700");
  const initials = user ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase() : "U";

  return (
    <Box
      position="fixed"
      top={4}
      right={6}
      zIndex={1000}
      bg={bg}
      borderRadius="xl"
      boxShadow="lg"
      border="1px solid"
      borderColor={border}
      px={3}
      py={2}
      minW="220px"
      maxW="300px"
      _hover={{ boxShadow: "xl" }}
      transition="all 0.2s"
    >
      <Menu placement="bottom-end" strategy="fixed">
        <MenuButton as={Box} w="100%" cursor="pointer">
          <HStack spacing={3}>
            <Avatar size="sm" bg="blue.600" color="white" src="" ignoreFallback>
              {initials}
            </Avatar>
            <Box minW={0} flex={1}>
              <Text fontSize="sm" fontWeight="bold" isTruncated>{user?.firstName} {user?.lastName}</Text>
              <Text fontSize="xs" color="gray.500" isTruncated>{user?.email}</Text>
            </Box>
          </HStack>
        </MenuButton>
        <MenuList zIndex={1001}>
          <MenuItem icon={<UserIcon size={16} />} onClick={() => navigate("/profile")}>Ustawienia profilu</MenuItem>
          <MenuDivider />
          <MenuItem icon={<RefreshCw size={16} />} onClick={() => navigate("/tenants/managed")}>Zmień aktywnego tenanta</MenuItem>
          <MenuDivider />
          <MenuItem color="red.500" onClick={() => logout()}>Wyloguj się</MenuItem>
        </MenuList>
      </Menu>
    </Box>
  );
}

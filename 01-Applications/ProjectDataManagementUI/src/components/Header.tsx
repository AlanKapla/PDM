import { useEffect, useState, useContext } from "react";
import {
  Box,
  Text,
  HStack,
  Avatar,
  Menu,
  MenuButton,
  MenuList,
  MenuItem,
  MenuDivider,
  useColorModeValue,
  Icon,
  VStack,
} from "@chakra-ui/react";
import { useNavigate, useLocation } from "react-router-dom";
import { Database, User as UserIcon, RefreshCw, Building2 } from "lucide-react";
import { AuthContext } from "../context/AuthContext";
import { tenantApi } from "../api/tenantApi";
import NotificationBell from "./NotificationBell";

export default function Header() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout, isAuthenticated } = useContext(AuthContext);
  const [activeTenantName, setActiveTenantName] = useState<string | null>(null);
  
  const bg = useColorModeValue("white", "gray.800");
  const border = useColorModeValue("gray.200", "gray.700");
  const textColor = useColorModeValue("gray.700", "gray.200");
  const mutedColor = useColorModeValue("gray.600", "gray.400");
  const initials = user ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase() : "U";

  // Pobierz nazwę aktywnego tenanta
  useEffect(() => {
    if (!isAuthenticated) return;

    const fetchActiveTenant = async () => {
      try {
        const [activeTenantResponse, tenantsResponse] = await Promise.all([
          tenantApi.getActiveTenant(),
          tenantApi.getUserTenants(),
        ]);

        const activeTenantData = activeTenantResponse.data;
        const tenants = tenantsResponse.data;
        
        if (activeTenantData?.activeTenantId) {
          const activeTenant = tenants.find((t: any) => t.id === activeTenantData.activeTenantId);
          setActiveTenantName(activeTenant?.name || null);
        } else {
          setActiveTenantName(null);
        }
      } catch (err) {
        console.error("Błąd pobierania aktywnego tenanta:", err);
        setActiveTenantName(null);
      }
    };

    fetchActiveTenant();
  }, [isAuthenticated, location.pathname]);

  return (
    <Box
      bg={bg}
      borderBottom="1px solid"
      borderColor={border}
      px={{ base: 4, md: 6 }}
      py={3}
      position="fixed"
      top={0}
      left={0}
      right={0}
      zIndex={1000}
    >
      <HStack maxW="100%" mx="auto" justify="space-between">
        {/* Nazwa aplikacji */}
        <HStack 
          spacing={2}
          cursor="pointer"
          _hover={{ opacity: 0.8 }}
          onClick={() => navigate("/dashboard")}
        >
          <Icon as={Database} boxSize={5} color="blue.600" />
          <Text 
            fontSize="lg" 
            fontWeight="bold" 
            color={textColor}
          >
            Project Data Management
          </Text>
        </HStack>

        {/* Menu użytkownika */}
        {isAuthenticated && user ? (
          <HStack spacing={2}>
            <VStack 
              align="flex-end" 
              spacing={0}
              display={{ base: "none", md: "flex" }}
            >
              <Text 
                fontSize="sm" 
                fontWeight="medium" 
                color={textColor}
              >
                {user.firstName} {user.lastName}
              </Text>
              {activeTenantName && (
                <HStack spacing={1} fontSize="xs" color={mutedColor}>
                  <Icon as={Building2} boxSize={3} />
                  <Text>{activeTenantName}</Text>
                </HStack>
              )}
            </VStack>
            
            <NotificationBell />
            
            <Menu placement="bottom-end" strategy="fixed">
              <MenuButton cursor="pointer">
                <Avatar 
                  size="sm" 
                  bg="blue.600" 
                  color="white" 
                  src=""
                  ignoreFallback
                  css={{
                    "& svg": { display: "none" }
                  }}
                >
                  {initials}
                </Avatar>
              </MenuButton>
            <MenuList zIndex={1001}>
              <MenuItem icon={<UserIcon size={16} />} onClick={() => navigate("/profile")}>
                Ustawienia profilu
              </MenuItem>
              <MenuDivider />
              <MenuItem icon={<RefreshCw size={16} />} onClick={() => navigate("/tenants/collaborating")}>
                Zmień aktywnego tenanta
              </MenuItem>
              <MenuDivider />
              <MenuItem 
                color="red.500" 
                onClick={async () => { 
                  await logout(); 
                  navigate("/"); 
                }}
              >
                Wyloguj się
              </MenuItem>
            </MenuList>
            </Menu>
          </HStack>
        ) : (
          <Text fontSize="sm" color="gray.500">Nie zalogowano</Text>
        )}
      </HStack>
    </Box>
  );
}

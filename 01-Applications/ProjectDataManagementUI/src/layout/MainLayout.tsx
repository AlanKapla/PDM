import { useEffect } from "react";
import { Box, useDisclosure, Drawer, DrawerOverlay, DrawerContent, DrawerCloseButton, DrawerBody } from "@chakra-ui/react";
import Sidebar, { SidebarContent } from "../components/Sidebar";
import Header from "../components/Header";
import Breadcrumbs from "../components/Breadcrumbs";
import { BottomNavBar } from "../components/ui";
import { useNavigate } from "react-router-dom";
import { useMyTenants } from "../hooks/queries";
import { useToastNotification } from "../hooks/useToastNotification";
import { subscriptionEventEmitter } from "../services/subscriptionEventEmitter";
import { RoleCodes } from "../constants/roleCodes";

interface MainLayoutProps {
  children: React.ReactNode;
}

export default function MainLayout({ children }: MainLayoutProps) {
  const { isOpen, onOpen, onClose } = useDisclosure();
  const navigate = useNavigate();
  const { data: tenants = [] } = useMyTenants();
  const { showWarning, showError } = useToastNotification();

  useEffect(() => {
    const unsubscribe = subscriptionEventEmitter.onBlocked(({ tenantId }) => {
      const tenant = tenants.find(t => t.id === tenantId);
      const userIsAdmin = tenant?.roleCode === RoleCodes.TENANT_ADMIN;
      if (userIsAdmin) {
        navigate(`/tenants/${tenantId}/subscription`);
        showWarning('Subskrypcja nieaktywna', 'Opłać subskrypcję aby uzyskać pełny dostęp do tenanta.');
      } else {
        showError('Brak dostępu', 'Subskrypcja tenanta jest nieaktywna. Skontaktuj się z administratorem.');
      }
    });
    return unsubscribe;
  }, [tenants, navigate, showWarning, showError]);

  return (
    <Box>
      <Header onMenuOpen={onOpen} />
      <Sidebar />

      {/* Mobile Sidebar Drawer */}
      <Drawer isOpen={isOpen} placement="left" onClose={onClose} size="xs">
        <DrawerOverlay />
        <DrawerContent>
          <DrawerCloseButton />
          <DrawerBody p={5}>
            <SidebarContent />
          </DrawerBody>
        </DrawerContent>
      </Drawer>

      {/* Treść strony — padding-bottom na mobile dla dolnego paska nav */}
      <Box
        ml={{ base: 0, md: "250px" }}
        pt={{ base: "60px", md: "60px" }}
        pb={{ base: "64px", md: 0 }}
        minH="100vh"
      >
        <Breadcrumbs />
        {children}
      </Box>

      {/* Dolny pasek nawigacji — tylko mobile */}
      <BottomNavBar />
    </Box>
  );
}

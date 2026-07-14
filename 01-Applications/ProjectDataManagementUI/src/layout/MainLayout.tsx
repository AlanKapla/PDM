import { Box, useDisclosure, Drawer, DrawerOverlay, DrawerContent, DrawerCloseButton, DrawerBody } from "@chakra-ui/react";
import Sidebar, { SidebarContent } from "../components/Sidebar";
import Header from "../components/Header";
import Breadcrumbs from "../components/Breadcrumbs";
import { BottomNavBar } from "../components/ui";
import { useAuth } from "../context/AuthContext";
import { hasActiveTenant } from "../utils/tenantUtils";

interface MainLayoutProps {
  children: React.ReactNode;
}

export default function MainLayout({ children }: MainLayoutProps) {
  const { isOpen, onOpen, onClose } = useDisclosure();
  const { user } = useAuth();
  const showNavigation = hasActiveTenant(user?.activeTenantId);

  return (
    <Box>
      {/* Skip link — WCAG 2.4.1 */}
      <a href="#main-content" className="skip-link">
        Przejdź do treści głównej
      </a>

      <Header onMenuOpen={onOpen} />
      {showNavigation && <Sidebar />}

      {/* Mobile Sidebar Drawer */}
      {showNavigation && (
        <Drawer isOpen={isOpen} placement="left" onClose={onClose} size="xs">
          <DrawerOverlay />
          <DrawerContent>
            <DrawerCloseButton />
            <DrawerBody p={5}>
              <SidebarContent />
            </DrawerBody>
          </DrawerContent>
        </Drawer>
      )}

      {/* Treść strony — padding-bottom na mobile dla dolnego paska nav */}
      <Box
        as="main"
        id="main-content"
        ml={{ base: 0, md: showNavigation ? "250px" : 0 }}
        pt={{ base: "60px", md: "60px" }}
        pb={{ base: showNavigation ? "64px" : 0, md: 0 }}
        minH="100vh"
      >
        {showNavigation && <Breadcrumbs />}
        {children}
      </Box>

      {/* Dolny pasek nawigacji — tylko mobile */}
      {showNavigation && <BottomNavBar />}
    </Box>
  );
}

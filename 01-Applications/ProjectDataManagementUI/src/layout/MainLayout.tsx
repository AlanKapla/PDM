import { Box, useDisclosure, Drawer, DrawerOverlay, DrawerContent, DrawerCloseButton, DrawerBody } from "@chakra-ui/react";
import Sidebar, { SidebarContent } from "../components/Sidebar";
import Header from "../components/Header";
import Breadcrumbs from "../components/Breadcrumbs";
import { BottomNavBar } from "../components/ui";

interface MainLayoutProps {
  children: React.ReactNode;
}

export default function MainLayout({ children }: MainLayoutProps) {
  const { isOpen, onOpen, onClose } = useDisclosure();

  return (
    <Box>
      {/* Skip link — WCAG 2.4.1 */}
      <a href="#main-content" className="skip-link">
        Przejdź do treści głównej
      </a>

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
        as="main"
        id="main-content"
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

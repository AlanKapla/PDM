import { Box } from "@chakra-ui/react";
import Sidebar from "../components/Sidebar";
import Header from "../components/Header";
import Breadcrumbs from "../components/Breadcrumbs";

export default function MainLayout({ children }: { children: React.ReactNode }) {
  return (
    <Box>
      <Header />
      <Sidebar />
      <Box ml={{ base: 0, md: "250px" }} pt="60px">
        <Breadcrumbs />
        {children}
      </Box>
    </Box>
  );
}

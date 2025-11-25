import { Box } from "@chakra-ui/react";
import Sidebar from "../components/Sidebar";
import Header from "../components/Header";

export default function MainLayout({ children }: { children: React.ReactNode }) {
  return (
  <Box>
    <Sidebar />
    <Box ml={{ base: 0, md: "250px" }}>
      <Header />
      <Box pt={{ base: 0, md: 0 }}>
        {children}
      </Box>
    </Box>
  </Box>
);
}

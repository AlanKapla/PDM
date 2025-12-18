import { Box } from "@chakra-ui/react";
import Sidebar from "../components/Sidebar";
import Header from "../components/Header";

export default function MainLayout({ children }: { children: React.ReactNode }) {
  return (
    <Box>
      <Header />
      <Sidebar />
      <Box ml={{ base: 0, md: "250px" }} pt="60px">
        {children}
      </Box>
    </Box>
  );
}

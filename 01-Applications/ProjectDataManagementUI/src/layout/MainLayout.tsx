import { Box } from "@chakra-ui/react";
import Sidebar from "../components/Sidebar";

export default function MainLayout({ children }: { children: React.ReactNode }) {
  return (
  <Box>
    <Sidebar />
    <Box ml={{ base: 0, md: "250px" }}>
      {children}
    </Box>
  </Box>
);
}

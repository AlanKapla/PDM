import { Box } from "@chakra-ui/react";
import Sidebar from "../components/Sidebar";

export default function MainLayout({ children }: { children: React.ReactNode }) {
  return (
  <Box>
    <Sidebar />
    <Box ml="250px">
      {children}
    </Box>
  </Box>
);
}

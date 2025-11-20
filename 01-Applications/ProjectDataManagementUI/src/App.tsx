import { BrowserRouter } from "react-router-dom";
import AppRouter from "./routes/AppRouter";
import { ChakraProvider } from "@chakra-ui/react";
import { AuthProvider } from "./context/AuthContext";

function App() {
  return (
    <ChakraProvider>
      <AuthProvider>
        <BrowserRouter>
          <AppRouter />
        </BrowserRouter>
      </AuthProvider>
    </ChakraProvider>
  );
}

export default App;

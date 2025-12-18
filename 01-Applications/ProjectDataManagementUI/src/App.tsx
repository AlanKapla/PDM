import { BrowserRouter } from "react-router-dom";
import AppRouter from "./routes/AppRouter";
import { AuthProvider } from "./context/AuthContext";
import CookieBanner from "./components/CookieBanner";

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <AppRouter />
        <CookieBanner />
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;

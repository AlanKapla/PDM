import { BrowserRouter } from "react-router-dom";
import AppRouter from "./routes/AppRouter";
import { AuthProvider } from "./context/AuthContext";
import { ProjectCacheProvider } from "./context/ProjectCacheContext";
import CookieBanner from "./components/CookieBanner";

function App() {
  return (
    <AuthProvider>
      <ProjectCacheProvider>
        <BrowserRouter>
          <AppRouter />
          <CookieBanner />
        </BrowserRouter>
      </ProjectCacheProvider>
    </AuthProvider>
  );
}

export default App;

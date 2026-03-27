import { BrowserRouter } from "react-router-dom";
import AppRouter from "./routes/AppRouter";
import { AuthProvider } from "./context/AuthContext";
import { ProjectCacheProvider } from "./context/ProjectCacheContext";
import { ChatUnreadProvider } from "./context/ChatUnreadContext";
import CookieBanner from "./components/CookieBanner";

function App() {
  return (
    <AuthProvider>
      <ProjectCacheProvider>
        <ChatUnreadProvider>
          <BrowserRouter>
            <AppRouter />
            <CookieBanner />
          </BrowserRouter>
        </ChatUnreadProvider>
      </ProjectCacheProvider>
    </AuthProvider>
  );
}

export default App;
